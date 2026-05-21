using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.AccountsReceivable.Collections;

public class IndexModel(
    ApplicationDbContext context,
    ARCollectionReportService arCollectionReportService,
    ReportExportService reportExportService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly ARCollectionReportService _arCollectionReportService = arCollectionReportService;
    private readonly ReportExportService _reportExportService = reportExportService;

    [BindProperty(SupportsGet = true)]
    public string Period { get; set; } = "daily";

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? ARAccountId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public ARCollectionReportResult Report { get; set; } = EmptyReport(DateTime.Today, DateTime.Today);

    public SelectList ARAccountOptions { get; set; } = null!;

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public string ReportTitle => Period.ToLowerInvariant() switch
    {
        "weekly" => "Weekly AR Collection Report",
        "monthly" => "Monthly AR Collection Report",
        "custom" => "Custom AR Collection Report",
        _ => "Daily AR Collection Report"
    };

    public async Task OnGetAsync()
    {
        await LoadReportAsync();
    }

    public async Task<IActionResult> OnGetCsvAsync()
    {
        await LoadReportAsync();

        var rows = new List<string[]>
        {
            new[] { "Metric", "Amount" },
            new[] { "Opening AR", _reportExportService.FormatCurrency(Report.OpeningAR) },
            new[] { "Billings", _reportExportService.FormatCurrency(Report.Billings) },
            new[] { "Debit Memos", _reportExportService.FormatCurrency(Report.DebitMemos) },
            new[] { "Collections", _reportExportService.FormatCurrency(Report.Collections) },
            new[] { "Credit Memos", _reportExportService.FormatCurrency(Report.CreditMemos) },
            new[] { "Adjustments", _reportExportService.FormatCurrency(Report.Adjustments) },
            new[] { "Ending AR", _reportExportService.FormatCurrency(Report.EndingAR) },
            new[] { "Overdue AR", _reportExportService.FormatCurrency(Report.OverdueAR) },
            new[] { "Collection Rate", Report.CollectionRate is null ? "N/A" : _reportExportService.FormatPercentage(Report.CollectionRate) },
            new[] { "DSO", Report.DaysSalesOutstanding?.ToString("0.00") ?? "N/A" },
            Array.Empty<string>(),
            new[] { "Top Collected Accounts", "Amount", "Last Payment Date" }
        };

        rows.AddRange(Report.TopCollectedAccounts.Select(row => new[]
        {
            row.AccountName,
            _reportExportService.FormatCurrency(row.Amount),
            row.LastPaymentDate is null ? string.Empty : _reportExportService.FormatDate(row.LastPaymentDate.Value)
        }));

        rows.Add(Array.Empty<string>());
        rows.Add(new[] { "Top Overdue Accounts", "Amount", "Last Payment Date" });
        rows.AddRange(Report.TopOverdueAccounts.Select(row => new[]
        {
            row.AccountName,
            _reportExportService.FormatCurrency(row.Amount),
            row.LastPaymentDate is null ? string.Empty : _reportExportService.FormatDate(row.LastPaymentDate.Value)
        }));

        rows.Add(Array.Empty<string>());
        rows.Add(new[] { "Payment Method", "Amount" });
        rows.AddRange(Report.PaymentsByMethod.Select(row => new[]
        {
            row.PaymentMethod,
            _reportExportService.FormatCurrency(row.Amount)
        }));

        var content = _reportExportService.ExportToCsv(ReportTitle, Report.PeriodStart, Report.PeriodEnd, rows);
        var fileName = _reportExportService.BuildSafeFileName(ReportTitle, Report.PeriodStart, Report.PeriodEnd, "csv");
        return File(content, "text/csv; charset=utf-8", fileName);
    }

    private async Task LoadReportAsync()
    {
        var (start, end) = ResolvePeriod();
        DateFrom = start;
        DateTo = end;

        Report = await _arCollectionReportService.GetCollectionReportAsync(start, end, ARAccountId, Status);
        await LoadOptionsAsync();
    }

    private (DateTime Start, DateTime End) ResolvePeriod()
    {
        var today = DateTime.Today;
        var normalized = string.IsNullOrWhiteSpace(Period) ? "daily" : Period.Trim().ToLowerInvariant();
        Period = normalized;

        if (normalized == "custom")
        {
            var start = (DateFrom ?? today).Date;
            var end = (DateTo ?? start).Date;
            return end < start ? (start, start) : (start, end);
        }

        if (normalized == "weekly")
        {
            var offset = ((int)today.DayOfWeek + 6) % 7;
            var start = today.AddDays(-offset).Date;
            return (start, start.AddDays(6));
        }

        if (normalized == "monthly")
        {
            var start = new DateTime(today.Year, today.Month, 1);
            return (start, start.AddMonths(1).AddDays(-1));
        }

        Period = "daily";
        return (today, today);
    }

    private async Task LoadOptionsAsync()
    {
        var accounts = await _context.ARAccounts
            .AsNoTracking()
            .OrderBy(account => account.AccountName)
            .Select(account => new { account.Id, account.AccountName })
            .ToListAsync();

        ARAccountOptions = new SelectList(accounts, "Id", "AccountName", ARAccountId);
        StatusOptions = Enum.GetValues<ARInvoiceStatus>()
            .Select(status => new SelectListItem
            {
                Value = status.ToString(),
                Text = status.ToString(),
                Selected = status.ToString().Equals(Status, StringComparison.OrdinalIgnoreCase)
            });
    }

    private static ARCollectionReportResult EmptyReport(DateTime start, DateTime end)
    {
        return new ARCollectionReportResult(
            start,
            end,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            null,
            Array.Empty<ARCollectionAccountRow>(),
            Array.Empty<ARCollectionAccountRow>(),
            Array.Empty<ARCollectionAccountRow>(),
            Array.Empty<ARPaymentMethodSummary>(),
            Array.Empty<ARCollectionTrendRow>());
    }
}
