using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Services;

public class GuestPortalService(
    ApplicationDbContext context,
    GuestPortalNotificationService notificationService,
    IConfiguration configuration,
    IMemoryCache memoryCache,
    ILogger<GuestPortalService> logger)
{
    private readonly ApplicationDbContext _context = context;
    private readonly GuestPortalNotificationService _notificationService = notificationService;
    private readonly IConfiguration _configuration = configuration;
    private readonly IMemoryCache _memoryCache = memoryCache;
    private readonly ILogger<GuestPortalService> _logger = logger;

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

    public async Task<GuestPortalLookupResult> LookupAsync(string reference, string? email, string? phone, string? clientKey = null)
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
        var throttleResult = GetLookupThrottleBlock(clientKey);
        if (throttleResult is not null)
        {
            return throttleResult;
        }

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
                return RecordLookupFailure(clientKey, "No matching reservation was found for the provided guest verification details.");
            }

            var access = await CreateOrReuseAccessAsync(
                reservation.Id,
                null,
                reservation.GuestId,
                reservation.Guest?.Email ?? normalizedEmail ?? string.Empty,
                reservation.Guest?.PhoneNumber ?? normalizedPhone);

            ClearLookupThrottle(clientKey);
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
                return RecordLookupFailure(clientKey, "No matching booking request was found for the provided guest verification details.");
            }

            var access = await CreateOrReuseAccessAsync(
                reservationId,
                bookingRequest.Id,
                guestId,
                matchEmail,
                matchPhone);

            ClearLookupThrottle(clientKey);
            return new GuestPortalLookupResult(true, "Booking request found.", access);
        }

        return RecordLookupFailure(clientKey, "No reservation or booking request matched the provided details.");
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
        var expiresAt = DateTime.Now.AddDays(GetAccessTokenLifetimeDays());

        if (existingAccess is not null)
        {
            existingAccess.LastAccessedAt = DateTime.Now;
            if (existingAccess.ExpiresAt is null || existingAccess.ExpiresAt > expiresAt)
            {
                existingAccess.ExpiresAt = expiresAt;
            }

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
            ExpiresAt = expiresAt,
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

    private int GetAccessTokenLifetimeDays()
    {
        var configuredDays = _configuration.GetValue<int?>("GuestPortal:AccessTokenDays");
        return configuredDays is >= 1 and <= 14 ? configuredDays.Value : 7;
    }

    private GuestPortalLookupResult? GetLookupThrottleBlock(string? clientKey)
    {
        var key = BuildLookupThrottleKey(clientKey);
        var now = DateTimeOffset.UtcNow;
        var state = _memoryCache.Get<GuestPortalLookupThrottleState>(key);
        if (state?.LockedUntil is null)
        {
            return null;
        }

        if (state.LockedUntil > now)
        {
            return BuildLookupThrottleResult(state.LockedUntil.Value - now);
        }

        _memoryCache.Remove(key);
        return null;
    }

    private GuestPortalLookupResult RecordLookupFailure(string? clientKey, string message)
    {
        var key = BuildLookupThrottleKey(clientKey);
        var now = DateTimeOffset.UtcNow;
        var options = GetLookupThrottleOptions();
        var state = _memoryCache.Get<GuestPortalLookupThrottleState>(key);

        if (state is null || now - state.FirstAttemptAt > options.Window)
        {
            state = new GuestPortalLookupThrottleState
            {
                Attempts = 1,
                FirstAttemptAt = now
            };
        }
        else
        {
            state.Attempts++;
        }

        if (state.Attempts >= options.MaxAttempts)
        {
            state.LockedUntil = now.Add(options.Lockout);
            _logger.LogWarning("Guest portal lookup temporarily throttled after repeated failed attempts.");
        }

        _memoryCache.Set(
            key,
            state,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = options.Window.Add(options.Lockout) });

        return state.LockedUntil is not null && state.LockedUntil > now
            ? BuildLookupThrottleResult(state.LockedUntil.Value - now)
            : new GuestPortalLookupResult(false, message, null);
    }

    private void ClearLookupThrottle(string? clientKey)
    {
        _memoryCache.Remove(BuildLookupThrottleKey(clientKey));
    }

    private GuestPortalLookupThrottleOptions GetLookupThrottleOptions()
    {
        var maxAttempts = Math.Clamp(_configuration.GetValue("GuestPortal:LookupMaxAttempts", 5), 3, 20);
        var windowMinutes = Math.Clamp(_configuration.GetValue("GuestPortal:LookupWindowMinutes", 15), 5, 60);
        var lockoutMinutes = Math.Clamp(_configuration.GetValue("GuestPortal:LookupLockoutMinutes", 15), 5, 60);
        return new GuestPortalLookupThrottleOptions(maxAttempts, TimeSpan.FromMinutes(windowMinutes), TimeSpan.FromMinutes(lockoutMinutes));
    }

    private static string BuildLookupThrottleKey(string? clientKey)
    {
        var normalized = string.IsNullOrWhiteSpace(clientKey) ? "unknown" : clientKey.Trim();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"GuestPortalLookup:{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private static GuestPortalLookupResult BuildLookupThrottleResult(TimeSpan retryAfter)
    {
        var minutes = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalMinutes));
        return new GuestPortalLookupResult(false, $"For guest privacy, lookup is temporarily limited after repeated failed attempts. Please try again in {minutes} minute(s).", null);
    }

    public record GuestPortalLookupResult(bool Succeeded, string Message, GuestPortalAccess? Access);

    public record ExpressCheckoutCompletionResult(bool Succeeded, string Message);

    private sealed class GuestPortalLookupThrottleState
    {
        public int Attempts { get; set; }

        public DateTimeOffset FirstAttemptAt { get; set; }

        public DateTimeOffset? LockedUntil { get; set; }
    }

    private sealed record GuestPortalLookupThrottleOptions(int MaxAttempts, TimeSpan Window, TimeSpan Lockout);
}
