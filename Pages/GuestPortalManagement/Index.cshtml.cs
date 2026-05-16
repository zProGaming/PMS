using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Pages.GuestPortalManagement;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public int PreCheckInsToday { get; set; }
    public int OpenServiceRequests { get; set; }
    public int UrgentServiceRequests { get; set; }
    public int PendingExpressCheckoutRequests { get; set; }
    public int FeedbackThisMonth { get; set; }
    public decimal AverageFeedbackRatingThisMonth { get; set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        PreCheckInsToday = await _context.GuestPreCheckIns.CountAsync(preCheckIn => preCheckIn.SubmittedAt >= today && preCheckIn.SubmittedAt < tomorrow);
        OpenServiceRequests = await _context.GuestServiceRequests.CountAsync(request =>
            request.Status == GuestServiceRequestStatus.New ||
            request.Status == GuestServiceRequestStatus.Assigned ||
            request.Status == GuestServiceRequestStatus.InProgress);
        UrgentServiceRequests = await _context.GuestServiceRequests.CountAsync(request =>
            request.Priority == GuestServiceRequestPriority.Urgent &&
            request.Status != GuestServiceRequestStatus.Completed &&
            request.Status != GuestServiceRequestStatus.Cancelled);
        PendingExpressCheckoutRequests = await _context.ExpressCheckoutRequests.CountAsync(request =>
            request.Status == ExpressCheckoutRequestStatus.Requested ||
            request.Status == ExpressCheckoutRequestStatus.UnderReview);
        FeedbackThisMonth = await _context.GuestFeedbacks.CountAsync(feedback => feedback.SubmittedAt >= monthStart && feedback.SubmittedAt < nextMonth);

        var ratings = await _context.GuestFeedbacks
            .Where(feedback => feedback.SubmittedAt >= monthStart && feedback.SubmittedAt < nextMonth)
            .Select(feedback => feedback.Rating)
            .ToListAsync();

        AverageFeedbackRatingThisMonth = ratings.Count == 0 ? 0 : (decimal)ratings.Average();
    }
}
