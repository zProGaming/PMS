using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.AccountsReceivable.ARPayments;

public class DetailsModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public ARPayment Payment { get; set; } = new();

    [BindProperty]
    public int ARInvoiceId { get; set; }

    [BindProperty]
    public decimal AllocatedAmount { get; set; }

    public SelectList InvoiceOptions { get; set; } = null!;

    public decimal RemainingAmount => Payment.Amount - Payment.Allocations.Sum(allocation => allocation.AllocatedAmount);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostAllocateAsync(int id)
    {
        var errors = await _financeService.AllocateARPaymentAsync(id, ARInvoiceId, AllocatedAmount, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            TempData["ErrorMessage"] = string.Join(" ", errors);
        }

        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var payment = await _context.ARPayments
            .AsNoTracking()
            .Include(item => item.ARAccount)
            .Include(item => item.Allocations)
                .ThenInclude(allocation => allocation.ARInvoice)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (payment is null)
        {
            return false;
        }

        Payment = payment;
        var invoices = await _context.ARInvoices
            .AsNoTracking()
            .Where(invoice => invoice.ARAccountId == payment.ARAccountId && invoice.Balance > 0)
            .OrderBy(invoice => invoice.DueDate)
            .Select(invoice => new { invoice.Id, Name = invoice.InvoiceNumber + " - Balance " + invoice.Balance })
            .ToListAsync();
        InvoiceOptions = new SelectList(invoices, "Id", "Name");
        return true;
    }
}
