using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.AccountsReceivable.CreditMemos;

public class IndexModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public IList<CreditMemo> CreditMemos { get; set; } = new List<CreditMemo>();

    [BindProperty]
    public CreditMemo CreditMemo { get; set; } = new() { CreditMemoDate = DateTime.Today };

    public SelectList ARAccountOptions { get; set; } = null!;
    public SelectList ARInvoiceOptions { get; set; } = null!;
    public SelectList FinanceDocumentOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        CreditMemo.CreditMemoNumber = await _financeService.GenerateSimpleNumberAsync("CM");
        CreditMemo.CreatedBy = User.Identity?.Name ?? string.Empty;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (CreditMemo.Amount <= 0)
        {
            ModelState.AddModelError(nameof(CreditMemo.Amount), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(CreditMemo.CreditMemoNumber))
        {
            CreditMemo.CreditMemoNumber = await _financeService.GenerateSimpleNumberAsync("CM");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        CreditMemo.Status = MemoStatus.Draft;
        _context.CreditMemos.Add(CreditMemo);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForApprovalAsync(int id)
    {
        var memo = await _context.CreditMemos.FindAsync(id);
        if (memo is null) return NotFound();
        if (memo.Status == MemoStatus.Draft) memo.Status = MemoStatus.ForApproval;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var memo = await _context.CreditMemos.FindAsync(id);
        if (memo is null) return NotFound();
        if (memo.Status == MemoStatus.ForApproval)
        {
            memo.Status = MemoStatus.Approved;
            memo.ApprovedBy = User.Identity?.Name ?? "System";
            memo.ApprovedAt = DateTime.Now;
        }
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApplyAsync(int id)
    {
        var errors = await _financeService.ApplyCreditMemoAsync(id);
        if (errors.Count > 0)
        {
            TempData["ErrorMessage"] = string.Join(" ", errors);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var memo = await _context.CreditMemos.FindAsync(id);
        if (memo is null) return NotFound();
        if (memo.Status != MemoStatus.Applied) memo.Status = MemoStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        CreditMemos = await _context.CreditMemos
            .AsNoTracking()
            .Include(memo => memo.ARAccount)
            .Include(memo => memo.ARInvoice)
            .Include(memo => memo.FinanceDocument)
            .OrderByDescending(memo => memo.CreditMemoDate)
            .Take(200)
            .ToListAsync();

        var accounts = await _context.ARAccounts.AsNoTracking().OrderBy(account => account.AccountName).ToListAsync();
        var invoices = await _context.ARInvoices.AsNoTracking().Where(invoice => invoice.Balance > 0).OrderBy(invoice => invoice.InvoiceNumber).ToListAsync();
        var documents = await _context.FinanceDocuments.AsNoTracking().OrderByDescending(document => document.DocumentDate).ToListAsync();

        ARAccountOptions = new SelectList(accounts, "Id", "AccountName", CreditMemo.ARAccountId);
        ARInvoiceOptions = new SelectList(invoices, "Id", "InvoiceNumber", CreditMemo.ARInvoiceId);
        FinanceDocumentOptions = new SelectList(documents, "Id", "DocumentNumber", CreditMemo.FinanceDocumentId);
    }
}
