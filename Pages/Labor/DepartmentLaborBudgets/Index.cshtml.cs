using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Labor;

namespace Vantage.PMS.Pages.Labor.DepartmentLaborBudgets;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public DepartmentLaborBudget Input { get; set; } = new();

    public IList<BudgetRow> Rows { get; private set; } = [];

    public SelectList DepartmentOptions { get; private set; } = default!;
    public SelectList USALIOptions { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Input.Month is < 1 or > 12)
        {
            ModelState.AddModelError("Input.Month", "Month must be between 1 and 12.");
        }

        if (Input.Year < 2000)
        {
            ModelState.AddModelError("Input.Year", "Year is required.");
        }

        if (Input.BudgetedLaborCost < 0 || Input.BudgetedLaborHours < 0 || Input.BudgetedHeadcount < 0)
        {
            ModelState.AddModelError(string.Empty, "Budget values cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        context.DepartmentLaborBudgets.Add(Input);
        await context.SaveChangesAsync();
        StatusMessage = "Department labor budget saved.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var budgets = await context.DepartmentLaborBudgets
            .AsNoTracking()
            .Include(budget => budget.Department)
            .Include(budget => budget.USALIDepartment)
            .OrderByDescending(budget => budget.Year)
            .ThenByDescending(budget => budget.Month)
            .Take(200)
            .ToListAsync();
        var actuals = await context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.PayrollPeriod != null && entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .GroupBy(entry => new
            {
                entry.DepartmentId,
                Month = entry.PayrollPeriod!.StartDate.Month,
                Year = entry.PayrollPeriod.StartDate.Year
            })
            .Select(group => new
            {
                group.Key.DepartmentId,
                group.Key.Month,
                group.Key.Year,
                Cost = group.Sum(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay),
                Hours = group.Sum(entry => entry.RegularHours + entry.OvertimeHours + entry.NightDifferentialHours)
            })
            .ToListAsync();

        Rows = budgets.Select(budget =>
        {
            var actual = actuals.FirstOrDefault(item => item.DepartmentId == budget.DepartmentId && item.Month == budget.Month && item.Year == budget.Year);
            return new BudgetRow
            {
                Budget = budget,
                ActualCost = actual?.Cost ?? 0,
                ActualHours = actual?.Hours ?? 0
            };
        }).ToList();
        DepartmentOptions = new SelectList(await context.Departments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.Name).ToListAsync(), "Id", "Name");
        USALIOptions = new SelectList(await context.USALIDepartments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.SortOrder).ToListAsync(), "Id", "Name");
    }
}

public class BudgetRow
{
    public DepartmentLaborBudget Budget { get; set; } = default!;

    public decimal ActualCost { get; set; }

    public decimal ActualHours { get; set; }

    public decimal CostVariance => ActualCost - Budget.BudgetedLaborCost;

    public decimal HoursVariance => ActualHours - Budget.BudgetedLaborHours;
}
