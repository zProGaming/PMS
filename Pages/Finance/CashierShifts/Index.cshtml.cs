using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.CashierShifts;

public class IndexModel(ApplicationDbContext context, FinanceService financeService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly FinanceService _financeService = financeService;

    public IList<CashierShift> Shifts { get; set; } = new List<CashierShift>();

    [BindProperty]
    public decimal OpeningCashFloat { get; set; }

    [BindProperty]
    public string? Notes { get; set; }

    public async Task OnGetAsync()
    {
        Shifts = await _context.CashierShifts
            .AsNoTracking()
            .OrderByDescending(shift => shift.OpenedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostOpenAsync()
    {
        var userName = User.Identity?.Name ?? "Cashier";
        var existing = await _financeService.GetOpenShiftForUserAsync(userName);
        if (existing is not null)
        {
            TempData["ErrorMessage"] = "You already have an open cashier shift.";
            return RedirectToPage();
        }

        var businessDate = await _context.BusinessDateSettings.AsNoTracking().FirstOrDefaultAsync();
        var shift = new CashierShift
        {
            ShiftNumber = await _financeService.GenerateSimpleNumberAsync("SHIFT"),
            BusinessDate = businessDate?.CurrentBusinessDate.Date ?? DateTime.Today,
            OpenedBy = userName,
            OpenedAt = DateTime.Now,
            OpeningCashFloat = OpeningCashFloat,
            Status = CashierShiftStatus.Open,
            Notes = Notes
        };

        _context.CashierShifts.Add(shift);
        await _context.SaveChangesAsync();
        return RedirectToPage("Details", new { id = shift.Id });
    }
}
