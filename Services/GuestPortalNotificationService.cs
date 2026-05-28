using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Services;

public class GuestPortalNotificationService(ILogger<GuestPortalNotificationService> logger)
{
    private readonly ILogger<GuestPortalNotificationService> _logger = logger;

    public Task SendPortalAccessAsync(GuestPortalAccess access)
    {
        _logger.LogInformation(
            "Guest portal access notification prepared for reservation {ReservationId}; token value suppressed.",
            access.ReservationId);
        return Task.CompletedTask;
    }

    public Task SendPreCheckInReceivedAsync(GuestPreCheckIn preCheckIn)
    {
        _logger.LogInformation("Pre-check-in received notification queued for reservation {ReservationId}.", preCheckIn.ReservationId);
        return Task.CompletedTask;
    }

    public Task SendServiceRequestReceivedAsync(GuestServiceRequest request)
    {
        _logger.LogInformation("Service request notification queued for request {RequestId}.", request.Id);
        return Task.CompletedTask;
    }

    public Task SendExpressCheckoutRequestedAsync(ExpressCheckoutRequest request)
    {
        _logger.LogInformation("Express checkout request notification queued for request {RequestId}.", request.Id);
        return Task.CompletedTask;
    }

    public Task SendFeedbackReceivedAsync(GuestFeedback feedback)
    {
        _logger.LogInformation("Guest feedback notification queued for feedback {FeedbackId}.", feedback.Id);
        return Task.CompletedTask;
    }
}
