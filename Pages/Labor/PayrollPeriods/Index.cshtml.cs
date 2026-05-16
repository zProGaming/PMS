using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Labor.PayrollPeriods;

public class IndexModel(ApplicationDbContext context, LaborCostingService laborCostingService) : PageModel
{
    [BindProperty]
    public PayrollPeriod Input { get; set; } = new()
    {
        StartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
        EndDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month))
    };

    public IList<PayrollPeriod> Periods { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.PeriodName))
        {
            ModelState.AddModelError("Input.PeriodName", "Period name is required.");
        }

        if (Input.StartDate.Date > Input.EndDate.Date)
        {
            ModelState.AddModelError("Input.EndDate", "End date must be on or after start date.");
        }

        var overlaps = await context.PayrollPeriods.AnyAsync(period =>
            period.Status != PayrollPeriodStatus.Cancelled &&
            (period.PeriodName == Input.PeriodName ||
                (Input.StartDate.Date <= period.EndDate.Date && Input.EndDate.Date >= period.StartDate.Date)));
        if (overlaps)
        {
            ModelState.AddModelError("Input.PeriodName", "Payroll period name or date range overlaps an existing active period.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.PreparedBy = User.Identity?.Name ?? "System";
        Input.Status = PayrollPeriodStatus.Draft;
        context.PayrollPeriods.Add(Input);
        await context.SaveChangesAsync();
        StatusMessage = "Payroll period created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var period = await context.PayrollPeriods.FindAsync(id);
        if (period is null)
        {
            return NotFound();
        }

        if (period.Status == PayrollPeriodStatus.Draft)
        {
            period.Status = PayrollPeriodStatus.ForApproval;
            await context.SaveChangesAsync();
            StatusMessage = "Payroll period submitted for approval.";
        }
        else
        {
            ErrorMessage = "Only draft payroll periods can be submitted.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var errors = await laborCostingService.ApprovePayrollPeriodAsync(id, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(" ", errors);
        }
        else
        {
            StatusMessage = "Payroll period approved.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPostAsync(int id)
    {
        var errors = await laborCostingService.PostPayrollPeriodAsync(id, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(" ", errors);
        }
        else
        {
            StatusMessage = "Payroll period posted to the general ledger.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var period = await context.PayrollPeriods.FindAsync(id);
        if (period is null)
        {
            return NotFound();
        }

        if (period.Status == PayrollPeriodStatus.Posted)
        {
            period.Status = PayrollPeriodStatus.Closed;
            await context.SaveChangesAsync();
            StatusMessage = "Payroll period closed.";
        }
        else
        {
            ErrorMessage = "Only posted payroll periods can be closed.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var period = await context.PayrollPeriods.FindAsync(id);
        if (period is null)
        {
            return NotFound();
        }

        if (period.Status is PayrollPeriodStatus.Posted or PayrollPeriodStatus.Closed)
        {
            ErrorMessage = "Posted or closed payroll periods cannot be cancelled.";
        }
        else
        {
            period.Status = PayrollPeriodStatus.Cancelled;
            await context.SaveChangesAsync();
            StatusMessage = "Payroll period cancelled.";
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Periods = await context.PayrollPeriods
            .AsNoTracking()
            .Include(period => period.Entries)
            .Include(period => period.JournalEntry)
            .OrderByDescending(period => period.StartDate)
            .Take(200)
            .ToListAsync();
    }
}
