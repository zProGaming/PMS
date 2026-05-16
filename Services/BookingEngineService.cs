using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Services;

public class BookingEngineService(
    ApplicationDbContext context,
    RevenueManagementService revenueManagement,
    BookingNotificationService bookingNotification)
{
    private readonly ApplicationDbContext _context = context;
    private readonly RevenueManagementService _revenueManagement = revenueManagement;
    private readonly BookingNotificationService _bookingNotification = bookingNotification;

    public async Task<BookingEngineSetting> GetSettingsAsync()
    {
        var setting = await _context.BookingEngineSettings
            .Include(settings => settings.DefaultRatePlan)
            .AsNoTracking()
            .OrderBy(settings => settings.Id)
            .FirstOrDefaultAsync();

        return setting ?? new BookingEngineSetting
        {
            HotelName = "Vantage PMS",
            BookingEngineTitle = "Book Your Stay",
            WelcomeMessage = "Search available rooms and send us your booking request.",
            IsBookingEngineEnabled = true,
            AllowPromoCodes = true,
            AllowSpecialRequests = true
        };
    }

    public async Task<int?> ResolveRatePlanIdAsync(BookingEngineSetting setting)
    {
        if (setting.DefaultRatePlanId is not null)
        {
            var exists = await _context.RatePlans
                .AsNoTracking()
                .AnyAsync(ratePlan => ratePlan.Id == setting.DefaultRatePlanId && ratePlan.IsActive);

            if (exists)
            {
                return setting.DefaultRatePlanId;
            }
        }

        return await _context.RatePlans
            .AsNoTracking()
            .Where(ratePlan => ratePlan.IsActive)
            .OrderBy(ratePlan => ratePlan.Code)
            .Select(ratePlan => (int?)ratePlan.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IList<BookingAvailabilityOption>> SearchAvailabilityAsync(BookingSearchCriteria criteria)
    {
        var setting = await GetSettingsAsync();
        var ratePlanId = await ResolveRatePlanIdAsync(setting);
        var promotion = await FindValidPromotionAsync(criteria.PromoCode, setting.AllowPromoCodes, ratePlanId, null);
        var nights = CountNights(criteria.CheckInDate, criteria.CheckOutDate);

        var roomTypes = await _context.RoomTypes
            .Include(roomType => roomType.Rooms)
            .AsNoTracking()
            .Where(roomType => roomType.IsActive)
            .OrderBy(roomType => roomType.Code)
            .ToListAsync();

        var roomContent = await _context.BookingEngineRoomContents
            .AsNoTracking()
            .Where(content => content.IsVisible)
            .ToDictionaryAsync(content => content.RoomTypeId);

        var options = new List<BookingAvailabilityOption>();
        foreach (var roomType in roomTypes)
        {
            if (roomType.MaxOccupancy > 0 && criteria.AdultCount + criteria.ChildCount > roomType.MaxOccupancy)
            {
                continue;
            }

            var availabilityErrors = await ValidateAvailabilityAsync(roomType.Id, ratePlanId, criteria.CheckInDate, criteria.CheckOutDate);
            if (availabilityErrors.Count > 0)
            {
                continue;
            }

            var roomRate = await _revenueManagement.GetSuggestedRateAsync(ratePlanId, roomType.Id, criteria.CheckInDate, criteria.CheckOutDate);
            if (roomRate <= 0)
            {
                continue;
            }

            roomContent.TryGetValue(roomType.Id, out var content);
            var roomTotal = roomRate * nights;
            var roomPromotion = await FindValidPromotionAsync(criteria.PromoCode, setting.AllowPromoCodes, ratePlanId, roomType.Id) ?? promotion;
            var discount = CalculateDiscount(roomPromotion, roomTotal);
            var totalAfterDiscount = Math.Max(0, roomTotal - discount);
            var depositAmount = setting.RequireDeposit
                ? totalAfterDiscount * setting.DepositPercentage / 100
                : 0;

            options.Add(new BookingAvailabilityOption(
                roomType.Id,
                ratePlanId,
                content?.DisplayName ?? roomType.Name,
                content?.ShortDescription ?? roomType.Description,
                content?.LongDescription,
                content?.ImageUrl,
                roomType.MaxOccupancy,
                roomRate,
                roomTotal,
                discount,
                totalAfterDiscount,
                setting.RequireDeposit,
                depositAmount,
                roomPromotion?.Code));
        }

        return options
            .OrderBy(option => roomContent.TryGetValue(option.RoomTypeId, out var content) ? content.SortOrder : 0)
            .ThenBy(option => option.TotalRoomAmount)
            .ToList();
    }

    public async Task<BookingQuoteResult> BuildQuoteAsync(BookingQuoteInput input)
    {
        var setting = await GetSettingsAsync();
        var ratePlanId = input.RatePlanId ?? await ResolveRatePlanIdAsync(setting);
        var errors = new List<string>();

        if (!setting.IsBookingEngineEnabled)
        {
            errors.Add("Online booking is currently unavailable.");
        }

        if (input.CheckOutDate.Date <= input.CheckInDate.Date)
        {
            errors.Add("Check-out date must be after check-in date.");
        }

        var roomType = await _context.RoomTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(roomType => roomType.Id == input.RoomTypeId && roomType.IsActive);

        if (roomType is null)
        {
            errors.Add("The selected room type is not available.");
            return new BookingQuoteResult(null, errors);
        }

        if (roomType.MaxOccupancy > 0 && input.AdultCount + input.ChildCount > roomType.MaxOccupancy)
        {
            errors.Add($"The selected room type allows up to {roomType.MaxOccupancy} guest(s).");
        }

        errors.AddRange(await ValidateAvailabilityAsync(input.RoomTypeId, ratePlanId, input.CheckInDate, input.CheckOutDate));

        var nights = CountNights(input.CheckInDate, input.CheckOutDate);
        var roomRate = await _revenueManagement.GetSuggestedRateAsync(ratePlanId, input.RoomTypeId, input.CheckInDate, input.CheckOutDate);
        if (roomRate <= 0)
        {
            errors.Add("No rate is configured for the selected room type and stay dates.");
        }

        var roomContent = await _context.BookingEngineRoomContents
            .AsNoTracking()
            .FirstOrDefaultAsync(content => content.RoomTypeId == input.RoomTypeId && content.IsVisible);

        var roomTotal = Math.Max(0, roomRate * nights);
        var promotion = await FindValidPromotionAsync(input.PromoCode, setting.AllowPromoCodes, ratePlanId, input.RoomTypeId);
        var promoError = await ValidatePromoCodeAsync(input.PromoCode, setting.AllowPromoCodes, ratePlanId, input.RoomTypeId);
        if (!string.IsNullOrWhiteSpace(promoError))
        {
            errors.Add(promoError);
        }

        var discount = CalculateDiscount(promotion, roomTotal);
        var addOnLines = await BuildAddOnLinesAsync(input.SelectedAddOns, nights, input.AdultCount + input.ChildCount);
        var addOnTotal = addOnLines.Sum(addOn => addOn.Amount);
        var totalAfterDiscount = Math.Max(0, roomTotal - discount);
        var finalTotal = totalAfterDiscount + addOnTotal;
        var depositAmount = setting.RequireDeposit
            ? finalTotal * setting.DepositPercentage / 100
            : 0;

        var quote = new BookingQuote(
            setting,
            roomType.Id,
            ratePlanId,
            promotion?.Id,
            roomContent?.DisplayName ?? roomType.Name,
            roomType.Code,
            roomContent?.ShortDescription ?? roomType.Description,
            roomContent?.LongDescription,
            roomContent?.ImageUrl,
            roomType.MaxOccupancy,
            input.CheckInDate.Date,
            input.CheckOutDate.Date,
            nights,
            input.AdultCount,
            input.ChildCount,
            roomRate,
            roomTotal,
            discount,
            totalAfterDiscount,
            addOnLines,
            addOnTotal,
            setting.RequireDeposit,
            depositAmount,
            finalTotal,
            promotion?.Code);

        return new BookingQuoteResult(quote, errors);
    }

    public async Task<string?> ValidatePromoCodeAsync(string? promoCode, bool allowPromoCodes, int? ratePlanId, int? roomTypeId)
    {
        if (string.IsNullOrWhiteSpace(promoCode) || !allowPromoCodes)
        {
            return null;
        }

        var normalizedCode = promoCode.Trim().ToUpperInvariant();
        var today = DateTime.Today;

        var promotion = await _context.PromotionCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(code => code.Code == normalizedCode);

        if (promotion is null || !promotion.IsActive)
        {
            return "The promo code is not valid.";
        }

        if (today < promotion.ValidFrom.Date || today > promotion.ValidTo.Date)
        {
            return "The promo code is outside its valid date range.";
        }

        if (promotion.UsageLimit.HasValue && promotion.TimesUsed >= promotion.UsageLimit.Value)
        {
            return "The promo code has reached its usage limit.";
        }

        if (promotion.AppliesToRatePlanId.HasValue && promotion.AppliesToRatePlanId != ratePlanId)
        {
            return "The promo code does not apply to the selected rate plan.";
        }

        if (promotion.AppliesToRoomTypeId.HasValue && promotion.AppliesToRoomTypeId != roomTypeId)
        {
            return "The promo code does not apply to the selected room type.";
        }

        return null;
    }

    public async Task<BookingRequest> CreateBookingRequestAsync(BookingQuote quote, BookingGuestInput guestInput)
    {
        var bookingRequest = new BookingRequest
        {
            BookingReference = await GenerateBookingReferenceAsync(),
            GuestFirstName = guestInput.FirstName.Trim(),
            GuestLastName = guestInput.LastName.Trim(),
            GuestEmail = guestInput.Email.Trim(),
            GuestPhone = guestInput.Phone?.Trim(),
            GuestAddress = guestInput.Address?.Trim(),
            CheckInDate = quote.CheckInDate,
            CheckOutDate = quote.CheckOutDate,
            AdultCount = quote.AdultCount,
            ChildCount = quote.ChildCount,
            RoomTypeId = quote.RoomTypeId,
            RatePlanId = quote.RatePlanId,
            PromotionCodeId = quote.PromotionCodeId,
            RoomRate = quote.RoomRate,
            DiscountAmount = quote.DiscountAmount,
            TotalRoomAmount = quote.TotalRoomAmount,
            DepositRequired = quote.DepositRequired,
            DepositAmount = quote.DepositAmount,
            SpecialRequests = guestInput.SpecialRequests?.Trim(),
            BookingStatus = BookingRequestStatus.Pending,
            CreatedAt = DateTime.Now
        };

        foreach (var addOn in quote.AddOns)
        {
            bookingRequest.AddOns.Add(new BookingRequestAddOn
            {
                BookingAddOnId = addOn.BookingAddOnId,
                Quantity = addOn.Quantity,
                UnitPrice = addOn.UnitPrice,
                Amount = addOn.Amount
            });
        }

        _context.BookingRequests.Add(bookingRequest);
        await _context.SaveChangesAsync();
        await _bookingNotification.SendBookingRequestReceivedAsync(bookingRequest);

        return bookingRequest;
    }

    public async Task<BookingConversionResult> ConvertToReservationAsync(int bookingRequestId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var bookingRequest = await _context.BookingRequests
            .Include(request => request.RoomType)
            .Include(request => request.PromotionCode)
            .FirstOrDefaultAsync(request => request.Id == bookingRequestId);

        if (bookingRequest is null)
        {
            return new BookingConversionResult(false, "Booking request was not found.", null);
        }

        if (bookingRequest.BookingStatus == BookingRequestStatus.ConvertedToReservation && bookingRequest.ReservationId is not null)
        {
            return new BookingConversionResult(true, "Booking request is already converted.", bookingRequest.ReservationId);
        }

        if (bookingRequest.BookingStatus == BookingRequestStatus.Cancelled)
        {
            return new BookingConversionResult(false, "Cancelled booking requests cannot be converted.", null);
        }

        var availabilityErrors = await ValidateAvailabilityAsync(
            bookingRequest.RoomTypeId,
            bookingRequest.RatePlanId,
            bookingRequest.CheckInDate,
            bookingRequest.CheckOutDate);

        if (availabilityErrors.Count > 0)
        {
            return new BookingConversionResult(false, string.Join(" ", availabilityErrors), null);
        }

        var roomType = bookingRequest.RoomType ?? await _context.RoomTypes.FindAsync(bookingRequest.RoomTypeId);
        if (roomType is null)
        {
            return new BookingConversionResult(false, "Room type was not found.", null);
        }

        var guest = await FindOrCreateGuestAsync(bookingRequest);
        var reservation = new Reservation
        {
            PropertyId = roomType.PropertyId,
            GuestId = guest.Id,
            RoomTypeId = bookingRequest.RoomTypeId,
            RatePlanId = bookingRequest.RatePlanId,
            ConfirmationNumber = CreateReservationConfirmationNumber(),
            ArrivalDate = bookingRequest.CheckInDate.Date,
            DepartureDate = bookingRequest.CheckOutDate.Date,
            RateAmount = bookingRequest.RoomRate,
            Adults = bookingRequest.AdultCount,
            Children = bookingRequest.ChildCount,
            Status = ReservationStatus.Reserved,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        bookingRequest.ReservationId = reservation.Id;
        bookingRequest.BookingStatus = BookingRequestStatus.ConvertedToReservation;
        bookingRequest.ConfirmedAt ??= DateTime.Now;

        if (bookingRequest.PromotionCode is not null)
        {
            bookingRequest.PromotionCode.TimesUsed += 1;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new BookingConversionResult(true, "Booking request converted to reservation.", reservation.Id);
    }

    public async Task<IList<string>> ValidateAvailabilityAsync(int roomTypeId, int? ratePlanId, DateTime checkInDate, DateTime checkOutDate)
    {
        return await _revenueManagement.ValidateReservationControlsAsync(
            null,
            ratePlanId,
            roomTypeId,
            checkInDate.Date,
            checkOutDate.Date);
    }

    private async Task<PromotionCode?> FindValidPromotionAsync(string? promoCode, bool allowPromoCodes, int? ratePlanId, int? roomTypeId)
    {
        var error = await ValidatePromoCodeAsync(promoCode, allowPromoCodes, ratePlanId, roomTypeId);
        if (!string.IsNullOrWhiteSpace(error) || string.IsNullOrWhiteSpace(promoCode) || !allowPromoCodes)
        {
            return null;
        }

        var normalizedCode = promoCode.Trim().ToUpperInvariant();
        return await _context.PromotionCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(code => code.Code == normalizedCode);
    }

    private async Task<IList<BookingAddOnLine>> BuildAddOnLinesAsync(IEnumerable<BookingAddOnSelection> selections, int nights, int guests)
    {
        var selected = selections
            .Where(selection => selection.Quantity > 0)
            .ToDictionary(selection => selection.BookingAddOnId, selection => selection.Quantity);

        if (selected.Count == 0)
        {
            return new List<BookingAddOnLine>();
        }

        var addOns = await _context.BookingAddOns
            .AsNoTracking()
            .Where(addOn => addOn.IsActive && selected.Keys.Contains(addOn.Id))
            .OrderBy(addOn => addOn.Name)
            .ToListAsync();

        return addOns
            .Select(addOn =>
            {
                var quantity = selected[addOn.Id];
                var amount = addOn.Price * quantity;
                if (addOn.IsPerNight)
                {
                    amount *= nights;
                }

                if (addOn.IsPerPerson)
                {
                    amount *= guests;
                }

                return new BookingAddOnLine(
                    addOn.Id,
                    addOn.Name,
                    quantity,
                    addOn.Price,
                    amount,
                    addOn.IsPerNight,
                    addOn.IsPerPerson);
            })
            .ToList();
    }

    private async Task<Guest> FindOrCreateGuestAsync(BookingRequest bookingRequest)
    {
        var email = bookingRequest.GuestEmail.Trim();
        var phone = bookingRequest.GuestPhone?.Trim();

        var guest = await _context.Guests.FirstOrDefaultAsync(existingGuest =>
            (!string.IsNullOrWhiteSpace(email) && existingGuest.Email == email) ||
            (!string.IsNullOrWhiteSpace(phone) && existingGuest.PhoneNumber == phone));

        if (guest is not null)
        {
            return guest;
        }

        guest = new Guest
        {
            FirstName = bookingRequest.GuestFirstName.Trim(),
            LastName = bookingRequest.GuestLastName.Trim(),
            Email = email,
            PhoneNumber = phone,
            AddressLine1 = bookingRequest.GuestAddress,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Guests.Add(guest);
        await _context.SaveChangesAsync();

        return guest;
    }

    private async Task<string> GenerateBookingReferenceAsync()
    {
        string reference;
        do
        {
            reference = $"BE-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
        }
        while (await _context.BookingRequests.AnyAsync(request => request.BookingReference == reference));

        return reference;
    }

    private static string CreateReservationConfirmationNumber()
    {
        return $"RES-BE-{DateTime.UtcNow:yyyyMMddHHmmss}";
    }

    private static int CountNights(DateTime checkInDate, DateTime checkOutDate)
    {
        return Math.Max(0, (checkOutDate.Date - checkInDate.Date).Days);
    }

    private static decimal CalculateDiscount(PromotionCode? promotion, decimal total)
    {
        if (promotion is null || total <= 0)
        {
            return 0;
        }

        var discount = promotion.DiscountType == DiscountType.Percentage
            ? total * promotion.DiscountValue / 100
            : promotion.DiscountValue;

        return Math.Clamp(discount, 0, total);
    }

    public record BookingSearchCriteria(
        DateTime CheckInDate,
        DateTime CheckOutDate,
        int AdultCount,
        int ChildCount,
        string? PromoCode);

    public record BookingAvailabilityOption(
        int RoomTypeId,
        int? RatePlanId,
        string DisplayName,
        string? ShortDescription,
        string? LongDescription,
        string? ImageUrl,
        int MaxOccupancy,
        decimal RoomRate,
        decimal RoomTotalBeforeDiscount,
        decimal DiscountAmount,
        decimal TotalRoomAmount,
        bool DepositRequired,
        decimal DepositAmount,
        string? PromotionCode);

    public record BookingQuoteInput(
        int RoomTypeId,
        int? RatePlanId,
        DateTime CheckInDate,
        DateTime CheckOutDate,
        int AdultCount,
        int ChildCount,
        string? PromoCode,
        IEnumerable<BookingAddOnSelection> SelectedAddOns);

    public record BookingAddOnSelection(
        int BookingAddOnId,
        int Quantity);

    public record BookingAddOnLine(
        int BookingAddOnId,
        string Name,
        int Quantity,
        decimal UnitPrice,
        decimal Amount,
        bool IsPerNight,
        bool IsPerPerson);

    public record BookingQuote(
        BookingEngineSetting Setting,
        int RoomTypeId,
        int? RatePlanId,
        int? PromotionCodeId,
        string RoomTypeName,
        string RoomTypeCode,
        string? ShortDescription,
        string? LongDescription,
        string? ImageUrl,
        int MaxOccupancy,
        DateTime CheckInDate,
        DateTime CheckOutDate,
        int Nights,
        int AdultCount,
        int ChildCount,
        decimal RoomRate,
        decimal RoomTotalBeforeDiscount,
        decimal DiscountAmount,
        decimal TotalRoomAmount,
        IList<BookingAddOnLine> AddOns,
        decimal AddOnTotal,
        bool DepositRequired,
        decimal DepositAmount,
        decimal FinalEstimatedTotal,
        string? PromotionCode);

    public record BookingQuoteResult(
        BookingQuote? Quote,
        IList<string> Errors);

    public record BookingGuestInput(
        string FirstName,
        string LastName,
        string Email,
        string? Phone,
        string? Address,
        string? SpecialRequests);

    public record BookingConversionResult(
        bool Succeeded,
        string Message,
        int? ReservationId);
}
