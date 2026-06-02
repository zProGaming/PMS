using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;

namespace Vantage.PMS.Pages.AccountsReceivable.ARPayments;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<ARPayment> Payments { get; set; } = new List<ARPayment>();

    [BindProperty]
    public ARPayment Payment { get; set; } = new() { PaymentDate = DateTime.Today };

    public SelectList ARAccountOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Payment.ReceivedBy = User.Identity?.Name ?? string.Empty;
        await LoadAsync();
    }

    public async Task<IActionResult> OnGetNativeAsync()
    {
        Payment.PaymentDate = DateTime.Today;
        Payment.ReceivedBy = User.Identity?.Name ?? string.Empty;
        await LoadAsync();
        return NativePartial();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Payment.Amount <= 0)
        {
            ModelState.AddModelError(nameof(Payment.Amount), "Payment amount must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return NativePartialOrPage();
        }

        _context.ARPayments.Add(Payment);
        await _context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = Payment.Id });
    }

    private IActionResult NativePartialOrPage()
    {
        return IsNativeWorkflowRequest() ? NativePartial() : Page();
    }

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
    }

    private PartialViewResult NativePartial()
    {
        return new PartialViewResult
        {
            ViewName = "_CreatePaymentNative",
            ViewData = new ViewDataDictionary<IndexModel>(ViewData, this)
        };
    }

    private async Task LoadAsync()
    {
        Payments = await _context.ARPayments
            .AsNoTracking()
            .Include(payment => payment.ARAccount)
            .Include(payment => payment.Allocations)
            .OrderByDescending(payment => payment.PaymentDate)
            .Take(200)
            .ToListAsync();

        var accounts = await _context.ARAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountName).ToListAsync();
        ARAccountOptions = new SelectList(accounts, "Id", "AccountName", Payment.ARAccountId);
    }
}
