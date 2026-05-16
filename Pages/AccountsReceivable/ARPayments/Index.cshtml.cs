using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Payment.Amount <= 0)
        {
            ModelState.AddModelError(nameof(Payment.Amount), "Payment amount must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        _context.ARPayments.Add(Payment);
        await _context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = Payment.Id });
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
