using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.AccountsPayable.PaymentVouchers;

public class CreateModel(ApplicationDbContext context, AccountsPayableService accountsPayableService) : PageModel
{
    [BindProperty]
    public PaymentVoucher Input { get; set; } = new();

    public SelectList SupplierOptions { get; private set; } = default!;
    public SelectList APInvoiceOptions { get; private set; } = default!;

    public async Task OnGetAsync(int? apInvoiceId)
    {
        Input.VoucherNumber = await accountsPayableService.GenerateNumberAsync("PV");
        Input.VoucherDate = DateTime.Today;
        Input.PaymentMethod = FinancePaymentMethod.BankTransfer;
        if (apInvoiceId is not null)
        {
            var invoice = await context.APInvoices.AsNoTracking().FirstOrDefaultAsync(item => item.Id == apInvoiceId.Value);
            if (invoice is not null)
            {
                Input.APInvoiceId = invoice.Id;
                Input.SupplierId = invoice.SupplierId;
                Input.Amount = invoice.Balance;
                Input.NetPaymentAmount = invoice.Balance;
                Input.Notes = $"Voucher for AP invoice {invoice.InvoiceNumber}.";
            }
        }
        await LoadOptionsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Input.SupplierId <= 0)
        {
            ModelState.AddModelError("Input.SupplierId", "Supplier is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.VoucherNumber))
        {
            ModelState.AddModelError("Input.VoucherNumber", "Voucher number is required.");
        }

        if (Input.Amount <= 0)
        {
            ModelState.AddModelError("Input.Amount", "Voucher amount must be positive.");
        }

        var invoice = Input.APInvoiceId is null ? null : await context.APInvoices.AsNoTracking().FirstOrDefaultAsync(item => item.Id == Input.APInvoiceId.Value);
        if (invoice is not null)
        {
            Input.SupplierId = invoice.SupplierId;
            if (Input.Amount > invoice.Balance)
            {
                ModelState.AddModelError("Input.Amount", "Voucher amount cannot exceed AP invoice balance.");
            }
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        Input.PreparedBy = User.Identity?.Name ?? "System";
        Input.Status = PaymentVoucherStatus.Draft;
        Input.NetPaymentAmount = Input.Amount - Input.WithholdingTaxAmount;
        context.PaymentVouchers.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = Input.Id });
    }

    private async Task LoadOptionsAsync()
    {
        SupplierOptions = new SelectList(await context.Suppliers.AsNoTracking().Where(item => item.IsActive).OrderBy(item => item.SupplierName).ToListAsync(), "Id", "SupplierName");
        APInvoiceOptions = new SelectList(await context.APInvoices.AsNoTracking()
            .Include(item => item.Supplier)
            .Where(item => item.Balance > 0 && item.Status != APInvoiceStatus.Cancelled && item.Status != APInvoiceStatus.Voided)
            .OrderByDescending(item => item.InvoiceDate)
            .Select(item => new { item.Id, Name = item.InvoiceNumber + " - " + (item.Supplier != null ? item.Supplier.SupplierName : "Supplier") + " - " + item.Balance.ToString("C") })
            .ToListAsync(), "Id", "Name");
    }
}
