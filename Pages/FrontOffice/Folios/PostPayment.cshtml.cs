using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.FrontOffice.Folios;

public class PostPaymentModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    [BindProperty]
    public Payment Payment { get; set; } = new();

    public int FolioId { get; set; }

    public string FolioNumber { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int? folioId)
    {
        if (folioId is null)
        {
            return NotFound();
        }

        var folio = await _context.Folios
            .AsNoTracking()
            .FirstOrDefaultAsync(folio => folio.Id == folioId);

        if (folio is null)
        {
            return NotFound();
        }

        FolioId = folio.Id;
        FolioNumber = folio.FolioNumber;
        var businessDate = await GetBusinessDateAsync();
        Payment = new Payment
        {
            FolioId = folio.Id,
            PaymentDate = businessDate.Date.Add(DateTime.Now.TimeOfDay),
            Status = PaymentStatus.Completed
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? folioId)
    {
        if (folioId is null)
        {
            return NotFound();
        }

        var folio = await _context.Folios.FindAsync(folioId);
        if (folio is null)
        {
            return NotFound();
        }

        FolioId = folio.Id;
        FolioNumber = folio.FolioNumber;
        var businessDate = await GetBusinessDateAsync();
        ValidatePayment();
        ValidatePaymentDate(businessDate);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Payment.FolioId = folio.Id;
        var userName = User.Identity?.Name ?? "Cashier";
        var allowWithoutOpenShift = User.IsInRole(PmsRoles.FinanceManager) ||
            User.IsInRole(PmsRoles.GeneralManager) ||
            User.IsInRole(PmsRoles.SystemAdmin);

        var errors = await _financeService.PostFolioPaymentAsync(Payment, userName, allowWithoutOpenShift);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return Page();
        }

        return RedirectToPage("./Details", new { id = folio.Id });
    }

    private void ValidatePayment()
    {
        if (Payment.Amount <= 0)
        {
            ModelState.AddModelError("Payment.Amount", "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(Payment.PaymentMethod))
        {
            ModelState.AddModelError("Payment.PaymentMethod", "Payment method is required.");
        }
    }

    private async Task<DateTime> GetBusinessDateAsync()
    {
        var setting = await _context.BusinessDateSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return setting?.CurrentBusinessDate.Date ?? DateTime.Today;
    }

    private void ValidatePaymentDate(DateTime businessDate)
    {
        if (Payment.PaymentDate.Date < businessDate.Date)
        {
            ModelState.AddModelError("Payment.PaymentDate", "Transactions for this business date are locked.");
        }
    }
}
