using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Finance.CashierShifts;

public class DetailsModel(ApplicationDbContext context, FinanceService financeService, CashierControlService controls) : PageModel
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

    public async Task<IActionResult> OnGetCashDropNativeAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? NativePartial("_CashDropNative") : NotFound();
    }

    public async Task<IActionResult> OnGetCloseNativeAsync(int id)
    {
        var found = await LoadAsync(id);
        return found ? NativePartial("_CloseNative") : NotFound();
    }

    public Task<IActionResult> OnPostCloseAsync(int id) => UpdateShiftAsync(id, true);
    public Task<IActionResult> OnPostCashDropAsync(int id) => UpdateShiftAsync(id, false);

    private async Task<IActionResult> UpdateShiftAsync(int id, bool close)
    {
        if (ModelState.IsValid)
            foreach (var error in await controls.UpdateAsync(id, User, close ? ClosingCashCount : CashDropAmount, close, CashDropReceivedBy, CashDropNotes))
                ModelState.AddModelError(string.Empty, error);
        if (!ModelState.IsValid)
        {
            if (IsNativeWorkflowRequest())
            {
                if (!await LoadAsync(id)) return NotFound();
                return NativePartial(close ? "_CloseNative" : "_CashDropNative");
            }
            TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
        }
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

    private bool IsNativeWorkflowRequest()
    {
        return string.Equals(Request.Query["vpmsNative"], "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(Request.Headers["X-VPMS-Native-Dialog"], "1", StringComparison.OrdinalIgnoreCase);
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
