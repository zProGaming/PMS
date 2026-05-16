using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Models.Executive;

namespace Vantage.PMS.Data;

public static class ExecutiveSeedData
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await SeedExecutiveKpisAsync(context);
        await SeedBenchmarkSettingsAsync(context);
    }

    private static async Task SeedExecutiveKpisAsync(ApplicationDbContext context)
    {
        var defaults = new[]
        {
            Kpi("OCC", "Occupancy Percentage", ExecutiveKPICategory.Occupancy, "Occupied Rooms / Total Active Rooms x 100", 75, 60, 45, true, 10),
            Kpi("ADR", "ADR", ExecutiveKPICategory.Revenue, "Room Revenue / Rooms Sold", 3800, 3200, 2600, true, 20),
            Kpi("REVPAR", "RevPAR", ExecutiveKPICategory.Revenue, "Room Revenue / Total Available Rooms", 2600, 2100, 1600, true, 30),
            Kpi("TOTAL_REV", "Total Revenue", ExecutiveKPICategory.Revenue, "Room Revenue + F&B Revenue + Banquet Revenue + Other Revenue", 250000, 180000, 120000, true, 40),
            Kpi("ROOM_REV", "Room Revenue", ExecutiveKPICategory.Revenue, "Posted room revenue for the period", 150000, 100000, 70000, true, 50),
            Kpi("FB_REV", "F&B Revenue", ExecutiveKPICategory.FoodBeverage, "Closed POS revenue for the period", 45000, 30000, 15000, true, 60),
            Kpi("BANQUET_REV", "Banquet Revenue", ExecutiveKPICategory.Banquet, "Banquet package and charge revenue for the period", 50000, 25000, 10000, true, 70),
            Kpi("GOP", "Gross Operating Profit", ExecutiveKPICategory.Profitability, "Operating revenue less cost of sales and operating expenses", 80000, 40000, 15000, true, 80),
            Kpi("NET_INCOME", "Net Income", ExecutiveKPICategory.Profitability, "Revenue less cost of sales, expenses, and other costs", 60000, 25000, 0, true, 90),
            Kpi("LABOR_PCT", "Labor Cost Percentage", ExecutiveKPICategory.Labor, "Labor Cost / Total Revenue x 100", 28, 35, 45, false, 100),
            Kpi("AR_BAL", "AR Balance", ExecutiveKPICategory.AccountsReceivable, "Open city ledger invoice balance", 250000, 400000, 600000, false, 110),
            Kpi("AP_BAL", "AP Balance", ExecutiveKPICategory.AccountsPayable, "Open supplier invoice balance", 200000, 350000, 500000, false, 120),
            Kpi("AVG_GUEST_RATING", "Average Guest Rating", ExecutiveKPICategory.GuestExperience, "Average guest feedback rating", 4.5m, 4.0m, 3.5m, true, 130),
            Kpi("OPEN_REQUESTS", "Open Service Requests", ExecutiveKPICategory.GuestExperience, "Guest service requests not completed or cancelled", 0, 5, 10, false, 140),
            Kpi("DIRTY_ROOM_PCT", "Dirty Rooms Percentage", ExecutiveKPICategory.Housekeeping, "Dirty Rooms / Total Rooms x 100", 10, 20, 30, false, 150),
            Kpi("OOO_ROOM_PCT", "Out of Order Rooms Percentage", ExecutiveKPICategory.Maintenance, "Out of Order Rooms / Total Rooms x 100", 2, 5, 10, false, 160),
            Kpi("FOOD_COST_PCT", "Food Cost Percentage", ExecutiveKPICategory.FoodBeverage, "Food Cost / Food Revenue x 100 when mapped", 32, 38, 45, false, 170),
            Kpi("BEV_COST_PCT", "Beverage Cost Percentage", ExecutiveKPICategory.FoodBeverage, "Beverage Cost / Beverage Revenue x 100 when mapped", 24, 30, 38, false, 180),
            Kpi("BANQUET_MARGIN", "Banquet Profit Margin", ExecutiveKPICategory.Banquet, "Banquet Profit / Banquet Revenue x 100", 35, 25, 15, true, 190),
            Kpi("REV_PER_LABOR_HOUR", "Revenue Per Labor Hour", ExecutiveKPICategory.Labor, "Total Revenue / Labor Hours", 1200, 900, 600, true, 200),
            Kpi("LOW_STOCK", "Inventory Low Stock Count", ExecutiveKPICategory.Inventory, "Active inventory items at or below reorder level", 0, 5, 10, false, 210),
            Kpi("BOOKING_CONVERSION", "Booking Conversion Rate", ExecutiveKPICategory.RevenueManagement, "Converted Booking Requests / Total Booking Requests x 100", 55, 35, 20, true, 220),
            Kpi("CANCEL_RATE", "Cancellation Rate", ExecutiveKPICategory.RevenueManagement, "Cancelled Reservations / Total Reservations x 100", 8, 15, 25, false, 230),
            Kpi("NOSHOW_RATE", "No-Show Rate", ExecutiveKPICategory.RevenueManagement, "No-show Reservations / Total Reservations x 100", 3, 8, 15, false, 240)
        };

        foreach (var kpi in defaults)
        {
            if (!await context.ExecutiveKPIs.AnyAsync(existing => existing.KPICode == kpi.KPICode))
            {
                context.ExecutiveKPIs.Add(kpi);
            }
        }
    }

    private static async Task SeedBenchmarkSettingsAsync(ApplicationDbContext context)
    {
        var effectiveFrom = new DateTime(DateTime.Today.Year, 1, 1);
        var activeKpis = await context.ExecutiveKPIs
            .Where(kpi => kpi.IsActive && kpi.TargetValue != null)
            .ToListAsync();

        var benchmarkSources = activeKpis.Count == 0
            ? DefaultBenchmarkSources()
            : activeKpis.Select(kpi => new BenchmarkSeed(kpi.KPIName, kpi.TargetValue ?? 0, kpi.WarningThreshold, kpi.CriticalThreshold)).ToArray();

        foreach (var kpi in benchmarkSources)
        {
            if (!await context.KPIBenchmarkSettings.AnyAsync(setting => setting.KPIName == kpi.KpiName && setting.IsActive))
            {
                context.KPIBenchmarkSettings.Add(new KPIBenchmarkSetting
                {
                    BenchmarkName = $"{kpi.KpiName} Default Target",
                    KPIName = kpi.KpiName,
                    TargetValue = kpi.TargetValue,
                    WarningThreshold = kpi.WarningThreshold,
                    CriticalThreshold = kpi.CriticalThreshold,
                    EffectiveFrom = effectiveFrom,
                    IsActive = true,
                    Notes = "Seeded default executive KPI benchmark. Review and adjust for the property before production use."
                });
            }
        }
    }

    private static BenchmarkSeed[] DefaultBenchmarkSources() =>
    [
        new("Occupancy Percentage", 75, 60, 45),
        new("ADR", 3800, 3200, 2600),
        new("RevPAR", 2600, 2100, 1600),
        new("Total Revenue", 250000, 180000, 120000),
        new("Labor Cost Percentage", 28, 35, 45),
        new("AR Balance", 250000, 400000, 600000),
        new("AP Balance", 200000, 350000, 500000),
        new("Average Guest Rating", 4.5m, 4.0m, 3.5m),
        new("Open Service Requests", 0, 5, 10),
        new("Inventory Low Stock Count", 0, 5, 10)
    ];

    private static ExecutiveKPI Kpi(
        string code,
        string name,
        ExecutiveKPICategory category,
        string formula,
        decimal? target,
        decimal? warning,
        decimal? critical,
        bool higherBetter,
        int sortOrder)
    {
        return new ExecutiveKPI
        {
            KPICode = code,
            KPIName = name,
            Category = category,
            Description = $"{name} executive KPI.",
            FormulaDescription = formula,
            TargetValue = target,
            WarningThreshold = warning,
            CriticalThreshold = critical,
            IsHigherBetter = higherBetter,
            IsActive = true,
            SortOrder = sortOrder
        };
    }

    private sealed record BenchmarkSeed(string KpiName, decimal TargetValue, decimal? WarningThreshold, decimal? CriticalThreshold);
}
