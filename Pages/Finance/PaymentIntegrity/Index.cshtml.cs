using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.PaymentIntegrity;

public class IndexModel(PaymentIntegrityService paymentIntegrityService) : PageModel
{
    private readonly PaymentIntegrityService _paymentIntegrityService = paymentIntegrityService;

    [BindProperty(SupportsGet = true)]
    public string? IssueType { get; set; }

    public PaymentIntegritySummary Summary { get; set; } = new(0, 0, 0, 0, 0, 0);

    public IReadOnlyList<PaymentIntegrityIssueRow> Issues { get; set; } = [];

    public async Task OnGetAsync()
    {
        Summary = await _paymentIntegrityService.GetSummaryAsync();
        var rows = await _paymentIntegrityService.GetIssueRowsAsync(500);
        Issues = string.IsNullOrWhiteSpace(IssueType)
            ? rows
            : rows.Where(row => string.Equals(row.IssueType, IssueType, StringComparison.OrdinalIgnoreCase)).ToList();
    }
}
