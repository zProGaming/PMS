using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.Documents;

public class DetailsModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public FinanceDocument FinanceDocument { get; set; } = new();

    [BindProperty]
    public FinanceDocumentLine NewLine { get; set; } = new() { Quantity = 1 };

    [BindProperty]
    public string? VoidReason { get; set; }

    [BindProperty]
    public int ARAccountId { get; set; }

    [BindProperty]
    public decimal DocumentPaymentAmount { get; set; }

    public SelectList ChargeCodeOptions { get; set; } = null!;
    public SelectList ARAccountOptions { get; set; } = null!;

    public bool CanEditLines => FinanceDocument.Status == FinanceDocumentStatus.Draft;

    public string NativeActionHandler { get; private set; } = string.Empty;
    public string NativeActionTitle { get; private set; } = string.Empty;
    public string NativeActionMessage { get; private set; } = string.Empty;
    public string NativeActionButtonText { get; private set; } = string.Empty;
    public string NativeActionButtonClass { get; private set; } = "vpms-btn-primary";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? Page() : NotFound();
    }

    public Task<IActionResult> OnGetIssueNativeAsync(int id) =>
        NativeConfirmAsync(
            id,
            "Issue",
            "Issue finance document",
            "Issue this draft document. Lines and totals will be recalculated before status changes.",
            "Issue Document",
            "vpms-btn-primary");

    public async Task<IActionResult> OnGetRecordPaymentNativeAsync(int id)
    {
        var found = await LoadAsync(id);
        if (!found)
        {
            return NotFound();
        }

        DocumentPaymentAmount = FinanceDocument.Balance;
        return NativePartial("_RecordPaymentNative");
    }

    public async Task<IActionResult> OnGetConvertToARNativeAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? NativePartial("_ConvertToARNative") : NotFound();
    }

    public async Task<IActionResult> OnGetVoidNativeAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? NativePartial("_VoidNative") : NotFound();
    }

    public async Task<IActionResult> OnPostAddLineAsync(int id)
    {
        var document = await _context.FinanceDocuments
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (document is null)
        {
            return NotFound();
        }

        if (document.Status != FinanceDocumentStatus.Draft)
        {
            TempData["ErrorMessage"] = "Lines can be edited only while the document is draft.";
            return RedirectToPage(new { id });
        }

        if (NewLine.Quantity <= 0)
        {
            ModelState.AddModelError(nameof(NewLine.Quantity), "Quantity must be greater than zero.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(id);
            return Page();
        }

        NewLine.FinanceDocumentId = id;
        _financeService.RecalculateFinanceDocumentLine(NewLine);
        _context.FinanceDocumentLines.Add(NewLine);
        document.Lines.Add(NewLine);
        _financeService.RecalculateFinanceDocument(document);
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteLineAsync(int id, int lineId)
    {
        var document = await _context.FinanceDocuments.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id);
        if (document is null)
        {
            return NotFound();
        }

        if (document.Status != FinanceDocumentStatus.Draft)
        {
            TempData["ErrorMessage"] = "Lines can be removed only while the document is draft.";
            return RedirectToPage(new { id });
        }

        var line = document.Lines.FirstOrDefault(item => item.Id == lineId);
        if (line is not null)
        {
            _context.FinanceDocumentLines.Remove(line);
            _financeService.RecalculateFinanceDocument(document);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostIssueAsync(int id)
    {
        var document = await _context.FinanceDocuments.Include(item => item.Lines).FirstOrDefaultAsync(item => item.Id == id);
        if (document is null)
        {
            return NotFound();
        }

        if (document.Status != FinanceDocumentStatus.Draft)
        {
            TempData["ErrorMessage"] = "Only draft documents can be issued.";
            return RedirectToPage(new { id });
        }

        if (document.Lines.Count == 0)
        {
            TempData["ErrorMessage"] = "Add at least one line before issuing.";
            return RedirectToPage(new { id });
        }

        _financeService.RecalculateFinanceDocument(document);
        document.Status = document.Balance == 0 ? FinanceDocumentStatus.Paid : FinanceDocumentStatus.Issued;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostVoidAsync(int id)
    {
        var document = await _context.FinanceDocuments.FindAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        if (document.Status is FinanceDocumentStatus.Paid or FinanceDocumentStatus.Voided)
        {
            TempData["ErrorMessage"] = "This document cannot be voided.";
            return RedirectToPage(new { id });
        }

        document.Status = FinanceDocumentStatus.Voided;
        document.VoidedBy = User.Identity?.Name ?? "System";
        document.VoidedAt = DateTime.Now;
        document.VoidReason = VoidReason;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRecordPaymentAsync(int id)
    {
        var document = await _context.FinanceDocuments.FindAsync(id);
        if (document is null)
        {
            return NotFound();
        }

        if (document.Status is not (FinanceDocumentStatus.Issued or FinanceDocumentStatus.PartiallyPaid))
        {
            TempData["ErrorMessage"] = "Payments can be recorded only for issued documents.";
            return RedirectToPage(new { id });
        }

        if (DocumentPaymentAmount <= 0 || DocumentPaymentAmount > document.Balance)
        {
            TempData["ErrorMessage"] = "Payment amount must be greater than zero and cannot exceed document balance.";
            return RedirectToPage(new { id });
        }

        document.AmountPaid += DocumentPaymentAmount;
        document.Balance = Math.Max(0, document.TotalAmount - document.AmountPaid);
        document.Status = document.Balance == 0 ? FinanceDocumentStatus.Paid : FinanceDocumentStatus.PartiallyPaid;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostConvertToARAsync(int id)
    {
        var document = await _context.FinanceDocuments.FirstOrDefaultAsync(item => item.Id == id);
        if (document is null)
        {
            return NotFound();
        }

        if (ARAccountId <= 0)
        {
            TempData["ErrorMessage"] = "Select an AR account.";
            return RedirectToPage(new { id });
        }

        if (document.Status is not (FinanceDocumentStatus.Issued or FinanceDocumentStatus.PartiallyPaid))
        {
            TempData["ErrorMessage"] = "Only issued unpaid documents can be converted to AR.";
            return RedirectToPage(new { id });
        }

        var exists = await _context.ARInvoices.AnyAsync(invoice => invoice.FinanceDocumentId == id);
        if (exists)
        {
            TempData["ErrorMessage"] = "This document already has an AR invoice.";
            return RedirectToPage(new { id });
        }

        var invoice = new ARInvoice
        {
            ARAccountId = ARAccountId,
            FinanceDocumentId = id,
            InvoiceNumber = await _financeService.GenerateSimpleNumberAsync("ARINV"),
            InvoiceDate = DateTime.Today,
            DueDate = document.DueDate ?? DateTime.Today.AddDays(30),
            OriginalAmount = document.TotalAmount,
            AmountPaid = document.AmountPaid,
            Balance = document.Balance,
            Status = document.Balance == 0 ? ARInvoiceStatus.Paid : ARInvoiceStatus.Open,
            CreatedAt = DateTime.Now,
            CreatedBy = User.Identity?.Name ?? "System",
            Notes = $"Created from finance document {document.DocumentNumber}."
        };

        _context.ARInvoices.Add(invoice);
        await _context.SaveChangesAsync();
        await _financeService.RecalculateARAccountBalanceAsync(ARAccountId);
        return RedirectToPage("/AccountsReceivable/ARInvoices/Index");
    }

    private async Task<bool> LoadAsync(int id)
    {
        var document = await _context.FinanceDocuments
            .AsNoTracking()
            .Include(item => item.Folio)
            .Include(item => item.Reservation)
            .Include(item => item.Guest)
            .Include(item => item.SalesAccount)
            .Include(item => item.BanquetEvent)
            .Include(item => item.Lines)
                .ThenInclude(line => line.ChargeCode)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (document is null)
        {
            return false;
        }

        FinanceDocument = document;
        await LoadOptionsAsync();
        return true;
    }

    private async Task LoadOptionsAsync()
    {
        var chargeCodes = await _context.ChargeCodes
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.Code)
            .Select(item => new { item.Id, Name = item.Code + " - " + item.Name })
            .ToListAsync();

        var arAccounts = await _context.ARAccounts
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.AccountName)
            .ToListAsync();

        ChargeCodeOptions = new SelectList(chargeCodes, "Id", "Name");
        ARAccountOptions = new SelectList(arAccounts, "Id", "AccountName");
    }

    private async Task<IActionResult> NativeConfirmAsync(
        int id,
        string handler,
        string title,
        string message,
        string buttonText,
        string buttonClass)
    {
        var found = await LoadAsync(id);
        if (!found)
        {
            return NotFound();
        }

        NativeActionHandler = handler;
        NativeActionTitle = title;
        NativeActionMessage = message;
        NativeActionButtonText = buttonText;
        NativeActionButtonClass = buttonClass;

        return NativePartial("_ConfirmActionNative");
    }

    private PartialViewResult NativePartial(string viewName)
    {
        return new PartialViewResult
        {
            ViewName = viewName,
            ViewData = new ViewDataDictionary<DetailsModel>(ViewData, this)
        };
    }
}
