using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.CashierShifts;

public class DetailsModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public CashierShift CashierShift { get; set; } = new();

    [BindProperty]
    public decimal ClosingCashCount { get; set; }

    [BindProperty]
    public decimal CashDropAmount { get; set; }

    [BindProperty]
    public string? CashDropReceivedBy { get; set; }

    [BindProperty]
    public string? CashDropNotes { get; set; }

    public decimal ExpectedCash { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var shift = await _context.CashierShifts
            .Include(item => item.Transactions)
            .Include(item => item.CashDrops)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (shift is null)
        {
            return NotFound();
        }

        if (shift.Status != CashierShiftStatus.Open)
        {
            TempData["ErrorMessage"] = "Only open cashier shifts can be closed.";
            return RedirectToPage(new { id });
        }

        var expected = _financeService.CalculateExpectedCash(shift);
        shift.ClosingCashCount = ClosingCashCount;
        shift.ExpectedCashAmount = expected;
        shift.CashOverShort = ClosingCashCount - expected;
        shift.ClosedBy = User.Identity?.Name ?? "System";
        shift.ClosedAt = DateTime.Now;
        shift.Status = CashierShiftStatus.Closed;
        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCashDropAsync(int id)
    {
        var shift = await _context.CashierShifts.FindAsync(id);
        if (shift is null)
        {
            return NotFound();
        }

        if (shift.Status != CashierShiftStatus.Open)
        {
            TempData["ErrorMessage"] = "Cash drops can be recorded only for open shifts.";
            return RedirectToPage(new { id });
        }

        if (CashDropAmount <= 0)
        {
            TempData["ErrorMessage"] = "Cash drop amount must be greater than zero.";
            return RedirectToPage(new { id });
        }

        _context.CashDrops.Add(new CashDrop
        {
            CashierShiftId = shift.Id,
            DropDate = DateTime.Now,
            Amount = CashDropAmount,
            DroppedBy = User.Identity?.Name ?? shift.OpenedBy,
            ReceivedBy = CashDropReceivedBy,
            Notes = CashDropNotes
        });

        _context.CashierTransactions.Add(new CashierTransaction
        {
            CashierShiftId = shift.Id,
            TransactionDate = DateTime.Now,
            TransactionType = CashierTransactionType.CashDrop,
            Amount = CashDropAmount,
            PaymentMethod = FinancePaymentMethod.Cash,
            Notes = CashDropNotes,
            CreatedBy = User.Identity?.Name ?? "System"
        });

        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        var shift = await _context.CashierShifts
            .AsNoTracking()
            .Include(item => item.Transactions)
                .ThenInclude(transaction => transaction.Payment)
            .Include(item => item.CashDrops)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (shift is null)
        {
            return false;
        }

        CashierShift = shift;
        ExpectedCash = _financeService.CalculateExpectedCash(shift);
        ClosingCashCount = shift.ClosingCashCount ?? ExpectedCash;
        return true;
    }
}
