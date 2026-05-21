using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.AccountsReceivable.Aging;

public class IndexModel(
    ApplicationDbContext context,
    ARCollectionReportService arCollectionReportService,
    ReportExportService reportExportService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly ARCollectionReportService _arCollectionReportService = arCollectionReportService;
    private readonly ReportExportService _reportExportService = reportExportService;

    [BindProperty(SupportsGet = true)]
    public DateTime AsOfDate { get; set; } = DateTime.Today;

    [BindProperty(SupportsGet = true)]
    public int? ARAccountId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? AgingBucket { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludePaid { get; set; }

    public ARAgingReportResult Report { get; set; } = new(DateTime.Today, Array.Empty<ARAgingInvoiceRow>(), new ARAgingSummary(0, 0, 0, 0, 0));

    public SelectList ARAccountOptions { get; set; } = null!;

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> AgingBucketOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task OnGetAsync()
    {
        await LoadReportAsync();
    }

    public async Task<IActionResult> OnGetCsvAsync()
    {
        await LoadReportAsync();

        var rows = new List<string[]>
        {
            new[] { "AR Account", "Invoice Number", "Invoice Date", "Due Date", "Original Amount", "Payments Applied", "Credit Memos", "Debit Memos", "Adjustments", "Balance", "Aging Bucket", "Days Overdue", "Last Payment Date", "Status" }
        };

        rows.AddRange(Report.Rows.Select(row => new[]
        {
            row.AccountName,
            row.InvoiceNumber,
            _reportExportService.FormatDate(row.InvoiceDate),
            _reportExportService.FormatDate(row.DueDate),
            _reportExportService.FormatCurrency(row.OriginalAmount),
            _reportExportService.FormatCurrency(row.PaymentsApplied),
            _reportExportService.FormatCurrency(row.CreditMemos),
            _reportExportService.FormatCurrency(row.DebitMemos),
            _reportExportService.FormatCurrency(row.Adjustments),
            _reportExportService.FormatCurrency(row.Balance),
            row.AgingBucket,
            row.DaysOverdue.ToString(),
            row.LastPaymentDate is null ? string.Empty : _reportExportService.FormatDate(row.LastPaymentDate.Value),
            row.Status.ToString()
        }));

        var content = _reportExportService.ExportToCsv("AR Aging Report", AsOfDate, AsOfDate, rows);
        var fileName = _reportExportService.BuildSafeFileName("AR Aging Report", AsOfDate, AsOfDate, "csv");
        return File(content, "text/csv; charset=utf-8", fileName);
    }

    private async Task LoadReportAsync()
    {
        AsOfDate = AsOfDate == default ? DateTime.Today : AsOfDate.Date;
        Report = await _arCollectionReportService.GetAgingReportAsync(AsOfDate, ARAccountId, Status, AgingBucket, Search, IncludePaid);
        await LoadOptionsAsync();
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
        var buckets = new[] { "Current", "1-30 days", "31-60 days", "61-90 days", "Over 90 days" };
        AgingBucketOptions = buckets.Select(bucket => new SelectListItem
        {
            Value = bucket,
            Text = bucket,
            Selected = bucket.Equals(AgingBucket, StringComparison.OrdinalIgnoreCase)
        });
    }
}
