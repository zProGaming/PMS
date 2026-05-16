using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Pages.Executive;

public class GuestExperienceModel(ApplicationDbContext context) : PageModel
{
    public decimal AverageRating { get; private set; }
    public int LowRatings { get; private set; }
    public int OpenUrgentRequests { get; private set; }
    public int PendingExpressCheckout { get; private set; }
    public int ComplaintCount { get; private set; }
    public decimal CompletionRate { get; private set; }
    public IList<RequestTypeRow> CommonRequests { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var since = DateTime.Today.AddDays(-30);
        AverageRating = await context.GuestFeedbacks.AsNoTracking().Where(feedback => feedback.SubmittedAt >= since).AverageAsync(feedback => (decimal?)feedback.Rating) ?? 0;
        LowRatings = await context.GuestFeedbacks.AsNoTracking().CountAsync(feedback => feedback.SubmittedAt >= since && feedback.Rating <= 2 && !feedback.IsResolved);
        OpenUrgentRequests = await context.GuestServiceRequests.AsNoTracking().CountAsync(request => request.Priority == GuestServiceRequestPriority.Urgent && request.Status != GuestServiceRequestStatus.Completed && request.Status != GuestServiceRequestStatus.Cancelled);
        PendingExpressCheckout = await context.ExpressCheckoutRequests.AsNoTracking().CountAsync(request => request.Status == ExpressCheckoutRequestStatus.Requested || request.Status == ExpressCheckoutRequestStatus.UnderReview);
        ComplaintCount = LowRatings;
        var totalRequests = await context.GuestServiceRequests.AsNoTracking().CountAsync(request => request.CreatedAt >= since);
        var completedRequests = await context.GuestServiceRequests.AsNoTracking().CountAsync(request => request.CreatedAt >= since && request.Status == GuestServiceRequestStatus.Completed);
        CompletionRate = totalRequests == 0 ? 0 : completedRequests * 100m / totalRequests;
        CommonRequests = await context.GuestServiceRequests
            .AsNoTracking()
            .Where(request => request.CreatedAt >= since)
            .GroupBy(request => request.RequestType)
            .Select(group => new RequestTypeRow(group.Key.ToString(), group.Count()))
            .OrderByDescending(row => row.Count)
            .Take(8)
            .ToListAsync();
    }
}

public record RequestTypeRow(string RequestType, int Count);
