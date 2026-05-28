using Vantage.PMS.Models.Booking;

namespace Vantage.PMS.Services;

public class BookingNotificationService(ILogger<BookingNotificationService> logger)
{
    private readonly ILogger<BookingNotificationService> _logger = logger;

    public Task SendBookingRequestReceivedAsync(BookingRequest bookingRequest)
    {
        _logger.LogInformation("Booking request received notification queued for {BookingReference}.", bookingRequest.BookingReference);
        return Task.CompletedTask;
    }

    public Task SendBookingConfirmedAsync(BookingRequest bookingRequest)
    {
        _logger.LogInformation("Booking confirmed notification queued for {BookingReference}.", bookingRequest.BookingReference);
        return Task.CompletedTask;
    }

    public Task SendBookingCancelledAsync(BookingRequest bookingRequest)
    {
        _logger.LogInformation("Booking cancelled notification queued for {BookingReference}.", bookingRequest.BookingReference);
        return Task.CompletedTask;
    }
}
