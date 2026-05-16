using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.AccountsReceivable.DebitMemos;

public class IndexModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public IList<DebitMemo> DebitMemos { get; set; } = new List<DebitMemo>();

    [BindProperty]
    public DebitMemo DebitMemo { get; set; } = new() { DebitMemoDate = DateTime.Today };

    public SelectList ARAccountOptions { get; set; } = null!;
    public SelectList ARInvoiceOptions { get; set; } = null!;
    public SelectList FinanceDocumentOptions { get; set; } = null!;

    public async Task OnGetAsync()
    {
        DebitMemo.DebitMemoNumber = await _financeService.GenerateSimpleNumberAsync("DM");
        DebitMemo.CreatedBy = User.Identity?.Name ?? string.Empty;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (DebitMemo.Amount <= 0)
        {
            ModelState.AddModelError(nameof(DebitMemo.Amount), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(DebitMemo.DebitMemoNumber))
        {
            DebitMemo.DebitMemoNumber = await _financeService.GenerateSimpleNumberAsync("DM");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        DebitMemo.Status = MemoStatus.Draft;
        _context.DebitMemos.Add(DebitMemo);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForApprovalAsync(int id)
    {
        var memo = await _context.DebitMemos.FindAsync(id);
        if (memo is null) return NotFound();
        if (memo.Status == MemoStatus.Draft) memo.Status = MemoStatus.ForApproval;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var memo = await _context.DebitMemos.FindAsync(id);
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
        var errors = await _financeService.ApplyDebitMemoAsync(id);
        if (errors.Count > 0)
        {
            TempData["ErrorMessage"] = string.Join(" ", errors);
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var memo = await _context.DebitMemos.FindAsync(id);
        if (memo is null) return NotFound();
        if (memo.Status != MemoStatus.Applied) memo.Status = MemoStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        DebitMemos = await _context.DebitMemos
            .AsNoTracking()
            .Include(memo => memo.ARAccount)
            .Include(memo => memo.ARInvoice)
            .Include(memo => memo.FinanceDocument)
            .OrderByDescending(memo => memo.DebitMemoDate)
            .Take(200)
            .ToListAsync();

        var accounts = await _context.ARAccounts.AsNoTracking().OrderBy(account => account.AccountName).ToListAsync();
        var invoices = await _context.ARInvoices.AsNoTracking().OrderBy(invoice => invoice.InvoiceNumber).ToListAsync();
        var documents = await _context.FinanceDocuments.AsNoTracking().OrderByDescending(document => document.DocumentDate).ToListAsync();

        ARAccountOptions = new SelectList(accounts, "Id", "AccountName", DebitMemo.ARAccountId);
        ARInvoiceOptions = new SelectList(invoices, "Id", "InvoiceNumber", DebitMemo.ARInvoiceId);
        FinanceDocumentOptions = new SelectList(documents, "Id", "DocumentNumber", DebitMemo.FinanceDocumentId);
    }
}
