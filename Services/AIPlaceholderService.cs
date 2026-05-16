using Vantage.PMS.Models.ManagementAI;

namespace Vantage.PMS.Services;

public class AIPlaceholderService
{
    public Task<string> GenerateDailySummaryTextAsync(ManagementDailySummary summary)
    {
        var revenueMix = summary.TotalRevenue > 0
            ? $"Rooms {Percent(summary.RoomRevenue, summary.TotalRevenue):0.#}%, F&B {Percent(summary.FBRevenue, summary.TotalRevenue):0.#}%, Banquet {Percent(summary.BanquetRevenue, summary.TotalRevenue):0.#}%"
            : "No revenue has been posted for this business date.";

        var text =
            $"Business date {summary.BusinessDate:MMM d, yyyy}: occupancy is {summary.OccupancyPercentage:0.#}% " +
            $"with {summary.OccupiedRooms} occupied room(s), {summary.AvailableRooms} available room(s), " +
            $"{summary.DirtyRooms} dirty room(s), and {summary.OutOfOrderRooms} out of order room(s). " +
            $"Expected front office movement includes {summary.ArrivalsToday} arrival(s), {summary.DeparturesToday} departure(s), " +
            $"and {summary.InHouseGuests} in-house guest(s). Total revenue is {summary.TotalRevenue:C} and payments are {summary.TotalPayments:C}. " +
            $"Revenue mix: {revenueMix}. Outstanding guest balances are {summary.OutstandingGuestBalances:C}; AR balance is {summary.ARBalance:C}. " +
            $"Operational attention points: {summary.OpenServiceRequests} open guest request(s), {summary.PendingHousekeepingTasks} pending housekeeping task(s), " +
            $"{summary.LowStockItems} low-stock item(s), {summary.PendingPurchaseRequests} pending purchase request(s), and {summary.PendingApprovals} pending approval(s).";

        return Task.FromResult(text);
    }

    public Task<IList<string>> GenerateOperationalRecommendationsAsync(ManagementDailySummary summary)
    {
        IList<string> recommendations = new List<string>();

        if (summary.ArrivalsToday > 20)
        {
            recommendations.Add("Stage registration cards and assign additional front desk coverage before peak arrival time.");
        }

        if (summary.DeparturesToday > 20 || summary.DirtyRooms > summary.TotalRooms * 0.2m)
        {
            recommendations.Add("Assign additional housekeeping attendants to due-out and dirty rooms before 2:00 PM.");
        }

        if (summary.OutOfOrderRooms > summary.TotalRooms * 0.05m)
        {
            recommendations.Add("Review out of order rooms with maintenance and prioritize rooms that can return to inventory today.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Monitor arrivals, departures, and housekeeping turnover from the live module dashboards.");
        }

        return Task.FromResult(recommendations);
    }

    public Task<IList<string>> GenerateFinancialRecommendationsAsync(ManagementDailySummary summary)
    {
        IList<string> recommendations = new List<string>();

        if (summary.OutstandingGuestBalances > 50000)
        {
            recommendations.Add("Review high-balance guest folios before allowing checkout.");
        }

        if (summary.ARBalance > 100000)
        {
            recommendations.Add("Follow up AR accounts with aging balances and prioritize invoices over 60 days.");
        }

        if (summary.PendingApprovals > 0)
        {
            recommendations.Add("Clear pending finance approvals before night audit to reduce control exceptions.");
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Reconcile cashier activity and guest balances before the close of business.");
        }

        return Task.FromResult(recommendations);
    }

    public Task<IList<string>> GenerateGuestExperienceRecommendationsAsync(ManagementDailySummary summary)
    {
        IList<string> recommendations = new List<string>();

        if (summary.OpenServiceRequests > 0)
        {
            recommendations.Add("Assign open guest service requests and follow up high-priority items with department supervisors.");
        }

        if (summary.InHouseGuests > 0 && summary.OpenServiceRequests == 0)
        {
            recommendations.Add("Run proactive guest courtesy checks for in-house VIPs and long-stay guests.");
        }

        return Task.FromResult(recommendations);
    }

    public Task<IList<string>> GenerateRevenueRecommendationsAsync(ManagementDailySummary summary)
    {
        IList<string> recommendations = new List<string>();

        if (summary.OccupancyPercentage < 40)
        {
            recommendations.Add("Review rate restrictions and promotions for low-occupancy dates.");
        }

        if (summary.OccupancyPercentage > 90)
        {
            recommendations.Add("Review remaining inventory, room rates, and upgrade opportunities for high-demand dates.");
        }

        if (summary.FBRevenue == 0 && summary.OccupancyPercentage > 50)
        {
            recommendations.Add("Push restaurant, bar, and room service offers to in-house guests.");
        }

        return Task.FromResult(recommendations);
    }

    public Task<IList<string>> GenerateInventoryRecommendationsAsync(ManagementDailySummary summary)
    {
        IList<string> recommendations = new List<string>();

        if (summary.LowStockItems > 0)
        {
            recommendations.Add("Reorder low-stock items before they affect F&B, housekeeping, or guest amenities.");
        }

        if (summary.PendingPurchaseRequests > 0)
        {
            recommendations.Add("Review pending purchase requests and convert approved operational needs to purchase orders.");
        }

        return Task.FromResult(recommendations);
    }

    private static decimal Percent(decimal value, decimal total)
    {
        return total <= 0 ? 0 : value / total * 100;
    }
}
