using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Reports;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.ReportReadiness;

public class IndexModel(ReportCatalogService reportCatalog, IWebHostEnvironment environment) : PageModel
{
    public IReadOnlyList<ReportReadinessGroup> Groups { get; private set; } = [];

    public ReportReadinessSummary Summary { get; private set; } = new(0, 0, 0, 0, 0, 100);

    public void OnGet()
    {
        var rows = reportCatalog.GetCatalog()
            .Select(InspectReport)
            .ToList();

        Groups = rows
            .GroupBy(row => row.Entry.ReportCategory)
            .Select(group => new ReportReadinessGroup(group.Key, ReportCatalogService.FormatCategory(group.Key), group.OrderBy(row => row.Entry.ReportName).ToList()))
            .OrderBy(group => group.CategoryName)
            .ToList();

        var available = rows.Count(row => row.Status == ReportReadinessStatus.Available);
        var review = rows.Count(row => row.Status == ReportReadinessStatus.NeedsReview);
        var planned = rows.Count(row => row.Status == ReportReadinessStatus.Planned);
        var csv = rows.Count(row => row.Entry.SupportsCsvExport);
        var score = rows.Count == 0 ? 100 : Math.Max(0, (int)Math.Round((available * 100m) / rows.Count));
        Summary = new ReportReadinessSummary(rows.Count, available, review, planned, csv, score);
    }

    private ReportReadinessRow InspectReport(ReportCatalogEntry entry)
    {
        if (!entry.IsAvailable)
        {
            return new ReportReadinessRow(entry, ReportReadinessStatus.Planned, "Report is intentionally not exposed as an available route.");
        }

        var pagePath = ToPageFilePath(entry.RoutePath!);
        var absolutePath = global::System.IO.Path.Combine(environment.ContentRootPath, pagePath);
        if (!global::System.IO.File.Exists(absolutePath))
        {
            return new ReportReadinessRow(entry, ReportReadinessStatus.NeedsReview, $"Catalog route has no page file: {pagePath}");
        }

        var content = global::System.IO.File.ReadAllText(absolutePath).ToLowerInvariant();
        var hasDisclaimer = content.Contains("disclaimer") || content.Contains("validated") || content.Contains("review");
        var financeCategory = entry.ReportCategory is ReportCategory.Accounting or ReportCategory.USALI or ReportCategory.PhilippineFinance or ReportCategory.AccountsReceivable or ReportCategory.AccountsPayable or ReportCategory.Banking;
        if (financeCategory && !hasDisclaimer)
        {
            return new ReportReadinessRow(entry, ReportReadinessStatus.NeedsReview, "Finance-sensitive report route exists, but disclaimer/review language was not detected.");
        }

        return new ReportReadinessRow(entry, ReportReadinessStatus.Available, entry.SupportsCsvExport ? "Route exists with CSV capability." : "Route exists with print/browser-PDF capability.");
    }

    private static string ToPageFilePath(string routePath)
    {
        var clean = routePath.Split('?', '#')[0].Trim('/');
        if (string.IsNullOrWhiteSpace(clean))
        {
            clean = "Index";
        }

        return global::System.IO.Path.Combine(["Pages", .. clean.Split('/', StringSplitOptions.RemoveEmptyEntries)]) + ".cshtml";
    }

    public static string StatusClass(ReportReadinessStatus status) => status switch
    {
        ReportReadinessStatus.Available => "vpms-status-pill success",
        ReportReadinessStatus.NeedsReview => "vpms-status-pill warning",
        ReportReadinessStatus.Planned => "vpms-status-pill",
        _ => "vpms-status-pill"
    };
}

public enum ReportReadinessStatus
{
    Available,
    NeedsReview,
    Planned
}

public record ReportReadinessRow(ReportCatalogEntry Entry, ReportReadinessStatus Status, string Evidence);

public record ReportReadinessGroup(ReportCategory Category, string CategoryName, IReadOnlyList<ReportReadinessRow> Rows);

public record ReportReadinessSummary(int Total, int Available, int NeedsReview, int Planned, int CsvEnabled, int Score);
