using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Labor;

namespace Vantage.PMS.Pages.Labor.PayrollAllocationRules;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public PayrollAllocationRule Input { get; set; } = new();

    public IList<PayrollAllocationRule> Rules { get; private set; } = [];

    public SelectList EmployeeOptions { get; private set; } = default!;
    public SelectList DepartmentOptions { get; private set; } = default!;
    public SelectList USALIOptions { get; private set; } = default!;
    public SelectList LaborAccountOptions { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.RuleName))
        {
            ModelState.AddModelError("Input.RuleName", "Rule name is required.");
        }

        if (Input.AllocationPercentage <= 0 || Input.AllocationPercentage > 100)
        {
            ModelState.AddModelError("Input.AllocationPercentage", "Allocation percentage must be between 0 and 100.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.IsActive = true;
        context.PayrollAllocationRules.Add(Input);
        await context.SaveChangesAsync();
        StatusMessage = "Payroll allocation rule created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var rule = await context.PayrollAllocationRules.FindAsync(id);
        if (rule is null)
        {
            return NotFound();
        }

        rule.IsActive = false;
        await context.SaveChangesAsync();
        StatusMessage = "Payroll allocation rule deactivated.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Rules = await context.PayrollAllocationRules
            .AsNoTracking()
            .Include(rule => rule.EmployeeCostProfile)
            .Include(rule => rule.Department)
            .Include(rule => rule.USALIDepartment)
            .Include(rule => rule.LaborGLAccount)
            .OrderBy(rule => rule.RuleName)
            .ToListAsync();
        EmployeeOptions = new SelectList(await context.EmployeeCostProfiles.AsNoTracking().Where(employee => employee.IsActive).OrderBy(employee => employee.FullName).ToListAsync(), "Id", "FullName");
        DepartmentOptions = new SelectList(await context.Departments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.Name).ToListAsync(), "Id", "Name");
        USALIOptions = new SelectList(await context.USALIDepartments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.SortOrder).ToListAsync(), "Id", "Name");
        LaborAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive && account.AccountType == GLAccountType.Expense).OrderBy(account => account.AccountCode).ToListAsync(), "Id", "AccountName");
    }
}
