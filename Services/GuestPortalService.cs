using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Services;

public class GuestPortalService(
    ApplicationDbContext context,
    GuestPortalNotificationService notificationService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly GuestPortalNotificationService _notificationService = notificationService;

    public async Task<GuestPortalSetting> GetSettingsAsync()
    {
        var setting = await _context.GuestPortalSettings
            .AsNoTracking()
            .OrderBy(setting => setting.Id)
            .FirstOrDefaultAsync();

        return setting ?? new GuestPortalSetting
        {
            PortalTitle = "Guest Portal",
            WelcomeMessage = "Access your stay details and send requests to our team.",
            IsGuestPortalEnabled = true,
            AllowPreCheckIn = true,
            AllowServiceRequests = true,
            AllowFolioView = true,
            AllowExpressCheckoutRequest = true,
            AllowFeedback = true,
            RequireReservationLookupVerification = true
        };
    }

    public async Task<GuestPortalLookupResult> LookupAsync(string reference, string? email, string? phone)
    {
        var setting = await GetSettingsAsync();
        if (!setting.IsGuestPortalEnabled)
        {
            return new GuestPortalLookupResult(false, "Guest portal is currently unavailable.", null);
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            return new GuestPortalLookupResult(false, "Reservation number or booking reference is required.", null);
        }

        var normalizedReference = reference.Trim();
        var normalizedEmail = email?.Trim();
        var normalizedPhone = phone?.Trim();

        var reservation = await _context.Reservations
            .Include(reservation => reservation.Guest)
            .Include(reservation => reservation.RoomType)
            .AsNoTracking()
            .FirstOrDefaultAsync(reservation =>
                reservation.ConfirmationNumber == normalizedReference ||
                reservation.Id.ToString() == normalizedReference);

        if (reservation is not null)
        {
            if (!IsVerified(setting, reservation.Guest?.Email, reservation.Guest?.PhoneNumber, normalizedEmail, normalizedPhone))
            {
                return new GuestPortalLookupResult(false, "No matching reservation was found for the provided guest verification details.", null);
            }

            var access = await CreateOrReuseAccessAsync(
                reservation.Id,
                null,
                reservation.GuestId,
                reservation.Guest?.Email ?? normalizedEmail ?? string.Empty,
                reservation.Guest?.PhoneNumber ?? normalizedPhone);

            return new GuestPortalLookupResult(true, "Reservation found.", access);
        }

        var bookingRequest = await _context.BookingRequests
            .Include(request => request.Reservation)
                .ThenInclude(reservation => reservation!.Guest)
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.BookingReference == normalizedReference);

        if (bookingRequest is not null)
        {
            var matchEmail = bookingRequest.GuestEmail;
            var matchPhone = bookingRequest.GuestPhone;
            var guestId = bookingRequest.Reservation?.GuestId;
            var reservationId = bookingRequest.ReservationId;

            if (!IsVerified(setting, matchEmail, matchPhone, normalizedEmail, normalizedPhone))
            {
                return new GuestPortalLookupResult(false, "No matching booking request was found for the provided guest verification details.", null);
            }

            var access = await CreateOrReuseAccessAsync(
                reservationId,
                bookingRequest.Id,
                guestId,
                matchEmail,
                matchPhone);

            return new GuestPortalLookupResult(true, "Booking request found.", access);
        }

        return new GuestPortalLookupResult(false, "No reservation or booking request matched the provided details.", null);
    }

    public async Task<GuestPortalAccess?> GetAccessAsync(string? token, bool asTracking = false)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var query = _context.GuestPortalAccesses
            .Include(access => access.Reservation)
                .ThenInclude(reservation => reservation!.Guest)
            .Include(access => access.Reservation)
                .ThenInclude(reservation => reservation!.RoomType)
            .Include(access => access.Reservation)
                .ThenInclude(reservation => reservation!.Room)
            .Include(access => access.Reservation)
                .ThenInclude(reservation => reservation!.Folios)
                    .ThenInclude(folio => folio.Items)
            .Include(access => access.Reservation)
                .ThenInclude(reservation => reservation!.Folios)
                    .ThenInclude(folio => folio.Payments)
            .Include(access => access.BookingRequest)
                .ThenInclude(request => request!.RoomType)
            .Include(access => access.BookingRequest)
                .ThenInclude(request => request!.Reservation)
            .Include(access => access.Guest)
            .AsSplitQuery()
            .Where(access => access.AccessToken == token && access.IsActive);

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        var portalAccess = await query.FirstOrDefaultAsync();
        if (portalAccess is null)
        {
            return null;
        }

        if (portalAccess.ExpiresAt is not null && portalAccess.ExpiresAt < DateTime.Now)
        {
            return null;
        }

        if (asTracking)
        {
            portalAccess.LastAccessedAt = DateTime.Now;
        }
        else
        {
            await _context.GuestPortalAccesses
                .Where(access => access.Id == portalAccess.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(access => access.LastAccessedAt, DateTime.Now));
        }

        return portalAccess;
    }

    public Folio? GetPrimaryFolio(GuestPortalAccess access)
    {
        return access.Reservation?.Folios.FirstOrDefault();
    }

    public async Task<ExpressCheckoutCompletionResult> CompleteExpressCheckoutAsync(int requestId, string? processedBy)
    {
        var request = await _context.ExpressCheckoutRequests
            .Include(checkoutRequest => checkoutRequest.Reservation)
                .ThenInclude(reservation => reservation!.Room)
            .Include(checkoutRequest => checkoutRequest.Reservation)
                .ThenInclude(reservation => reservation!.Folios)
                    .ThenInclude(folio => folio.Items)
            .Include(checkoutRequest => checkoutRequest.Reservation)
                .ThenInclude(reservation => reservation!.Folios)
                    .ThenInclude(folio => folio.Payments)
            .FirstOrDefaultAsync(checkoutRequest => checkoutRequest.Id == requestId);

        if (request?.Reservation is null)
        {
            return new ExpressCheckoutCompletionResult(false, "Express checkout request was not found.");
        }

        var reservation = request.Reservation;
        if (reservation.Status != ReservationStatus.CheckedIn)
        {
            return new ExpressCheckoutCompletionResult(false, "Only checked-in reservations can be checked out.");
        }

        if (reservation.Room is null)
        {
            return new ExpressCheckoutCompletionResult(false, "A room must be assigned before check-out.");
        }

        var balance = reservation.Folios.FirstOrDefault()?.Balance ?? 0;
        if (balance > 0)
        {
            return new ExpressCheckoutCompletionResult(false, "Guest has outstanding balance. Settlement is required before checkout completion.");
        }

        reservation.Status = ReservationStatus.CheckedOut;
        reservation.ActualCheckOutDate = DateTime.Now;
        reservation.Room.Status = RoomStatus.Dirty;
        request.Status = ExpressCheckoutRequestStatus.Completed;
        request.ProcessedAt = DateTime.Now;
        request.ProcessedBy = processedBy;

        await _context.SaveChangesAsync();
        return new ExpressCheckoutCompletionResult(true, "Express checkout completed.");
    }

    private async Task<GuestPortalAccess> CreateOrReuseAccessAsync(
        int? reservationId,
        int? bookingRequestId,
        int? guestId,
        string guestEmail,
        string? guestPhone)
    {
        var existingAccess = await _context.GuestPortalAccesses.FirstOrDefaultAsync(access =>
            access.IsActive &&
            access.ReservationId == reservationId &&
            access.BookingRequestId == bookingRequestId &&
            access.GuestEmail == guestEmail &&
            (access.ExpiresAt == null || access.ExpiresAt > DateTime.Now));

        if (existingAccess is not null)
        {
            existingAccess.LastAccessedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await _notificationService.SendPortalAccessAsync(existingAccess);
            return existingAccess;
        }

        var access = new GuestPortalAccess
        {
            ReservationId = reservationId,
            BookingRequestId = bookingRequestId,
            GuestId = guestId,
            AccessToken = Guid.NewGuid().ToString("N"),
            GuestEmail = guestEmail,
            GuestPhone = guestPhone,
            ExpiresAt = DateTime.Now.AddDays(30),
            IsActive = true,
            CreatedAt = DateTime.Now,
            LastAccessedAt = DateTime.Now
        };

        _context.GuestPortalAccesses.Add(access);
        await _context.SaveChangesAsync();
        await _notificationService.SendPortalAccessAsync(access);
        return access;
    }

    private static bool IsVerified(
        GuestPortalSetting setting,
        string? matchEmail,
        string? matchPhone,
        string? inputEmail,
        string? inputPhone)
    {
        if (!setting.RequireReservationLookupVerification)
        {
            return true;
        }

        var emailMatches = !string.IsNullOrWhiteSpace(inputEmail) &&
            string.Equals(matchEmail, inputEmail, StringComparison.OrdinalIgnoreCase);
        var phoneMatches = !string.IsNullOrWhiteSpace(inputPhone) &&
            string.Equals(NormalizePhone(matchPhone), NormalizePhone(inputPhone), StringComparison.OrdinalIgnoreCase);

        return emailMatches || phoneMatches;
    }

    private static string? NormalizePhone(string? phone)
    {
        return string.IsNullOrWhiteSpace(phone)
            ? null
            : new string(phone.Where(char.IsDigit).ToArray());
    }

    public record GuestPortalLookupResult(bool Succeeded, string Message, GuestPortalAccess? Access);

    public record ExpressCheckoutCompletionResult(bool Succeeded, string Message);
}
