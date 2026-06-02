using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.AccountsReceivable.ARInvoices;

public class IndexModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public IList<ARInvoice> Invoices { get; set; } = new List<ARInvoice>();

    [BindProperty]
    public ARInvoice Invoice { get; set; } = new() { InvoiceDate = DateTime.Today, DueDate = DateTime.Today.AddDays(30) };

    public SelectList ARAccountOptions { get; set; } = null!;
    public SelectList FinanceDocumentOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Invoice.InvoiceNumber = await _financeService.GenerateSimpleNumberAsync("ARINV");
        await LoadAsync();
    }

    public async Task<IActionResult> OnGetNativeAsync()
    {
        Invoice.InvoiceNumber = await _financeService.GenerateSimpleNumberAsync("ARINV");
        await LoadAsync();
        return NativePartial();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Invoice.InvoiceNumber))
        {
            Invoice.InvoiceNumber = await _financeService.GenerateSimpleNumberAsync("ARINV");
        }

        if (Invoice.OriginalAmount <= 0)
        {
            ModelState.AddModelError(nameof(Invoice.OriginalAmount), "Original amount must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return NativePartialOrPage();
        }

        Invoice.Balance = Invoice.OriginalAmount - Invoice.AmountPaid;
        Invoice.Status = Invoice.Balance <= 0 ? ARInvoiceStatus.Paid : Invoice.AmountPaid > 0 ? ARInvoiceStatus.PartiallyPaid : ARInvoiceStatus.Open;
        Invoice.CreatedAt = DateTime.Now;
        Invoice.CreatedBy = User.Identity?.Name ?? "System";
        _context.ARInvoices.Add(Invoice);
        await _context.SaveChangesAsync();
        await _financeService.RecalculateARAccountBalanceAsync(Invoice.ARAccountId);
        return RedirectToPage();
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
            ViewName = "_CreateInvoiceNative",
            ViewData = new ViewDataDictionary<IndexModel>(ViewData, this)
        };
    }

    private async Task LoadAsync()
    {
        Invoices = await _context.ARInvoices
            .AsNoTracking()
            .Include(invoice => invoice.ARAccount)
            .Include(invoice => invoice.FinanceDocument)
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .Take(200)
            .ToListAsync();

        var accounts = await _context.ARAccounts.AsNoTracking().Where(account => account.IsActive).OrderBy(account => account.AccountName).ToListAsync();
        var documents = await _context.FinanceDocuments
            .AsNoTracking()
            .Where(document => document.Status == FinanceDocumentStatus.Issued || document.Status == FinanceDocumentStatus.PartiallyPaid)
            .OrderByDescending(document => document.DocumentDate)
            .Select(document => new { document.Id, Name = document.DocumentNumber + " - " + document.BillingName })
            .ToListAsync();

        ARAccountOptions = new SelectList(accounts, "Id", "AccountName", Invoice.ARAccountId);
        FinanceDocumentOptions = new SelectList(documents, "Id", "Name", Invoice.FinanceDocumentId);
    }
}
