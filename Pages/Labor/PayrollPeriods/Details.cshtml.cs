using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Labor.PayrollPeriods;

public class DetailsModel(ApplicationDbContext context, LaborCostingService laborCostingService) : PageModel
{
    [BindProperty]
    public PayrollCostEntry Input { get; set; } = new();

    public PayrollPeriod? Period { get; private set; }

    public SelectList EmployeeOptions { get; private set; } = default!;

    public SelectList DepartmentOptions { get; private set; } = default!;

    public SelectList USALIOptions { get; private set; } = default!;

    public SelectList LaborAccountOptions { get; private set; } = default!;

    public SelectList LiabilityAccountOptions { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadAsync(id);
        return Period is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAddEntryAsync(int id)
    {
        await LoadAsync(id);
        if (Period is null)
        {
            return NotFound();
        }

        if (Period.Status is not (PayrollPeriodStatus.Draft or PayrollPeriodStatus.ForApproval))
        {
            ErrorMessage = "Only draft or for-approval payroll periods can be edited.";
            return RedirectToPage(new { id });
        }

        if (HasNegative(Input))
        {
            ModelState.AddModelError(string.Empty, "Payroll amounts and hours cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.EmployeeCostProfileId is not null)
        {
            var employee = await context.EmployeeCostProfiles.AsNoTracking().FirstOrDefaultAsync(item => item.Id == Input.EmployeeCostProfileId);
            if (employee is not null)
            {
                Input.DepartmentId ??= employee.DepartmentId;
                Input.USALIDepartmentId ??= employee.USALIDepartmentId;
                Input.Position ??= employee.Position;
                Input.LaborGLAccountId ??= employee.DefaultLaborGLAccountId;
                Input.PayrollLiabilityGLAccountId ??= employee.DefaultPayrollLiabilityGLAccountId;
            }
        }

        Input.PayrollPeriodId = id;
        laborCostingService.Recalculate(Input);
        context.PayrollCostEntries.Add(Input);
        await context.SaveChangesAsync();
        StatusMessage = "Payroll cost entry added.";
        return RedirectToPage(new { id });
    }

    private async Task LoadAsync(int id)
    {
        Period = await context.PayrollPeriods
            .AsNoTracking()
            .Include(period => period.Entries)
            .ThenInclude(entry => entry.EmployeeCostProfile)
            .Include(period => period.Entries)
            .ThenInclude(entry => entry.Department)
            .Include(period => period.Entries)
            .ThenInclude(entry => entry.USALIDepartment)
            .Include(period => period.JournalEntry)
            .FirstOrDefaultAsync(period => period.Id == id);

        EmployeeOptions = new SelectList(await context.EmployeeCostProfiles.AsNoTracking().Where(employee => employee.IsActive).OrderBy(employee => employee.FullName).ToListAsync(), "Id", "FullName");
        DepartmentOptions = new SelectList(await context.Departments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.Name).ToListAsync(), "Id", "Name");
        USALIOptions = new SelectList(await context.USALIDepartments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.SortOrder).ToListAsync(), "Id", "Name");
        LaborAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive && account.AccountType == GLAccountType.Expense).OrderBy(account => account.AccountCode).ToListAsync(), "Id", "AccountName");
        LiabilityAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive && account.AccountType == GLAccountType.Liability).OrderBy(account => account.AccountCode).ToListAsync(), "Id", "AccountName");
    }

    private static bool HasNegative(PayrollCostEntry entry)
    {
        return entry.RegularHours < 0 ||
            entry.OvertimeHours < 0 ||
            entry.NightDifferentialHours < 0 ||
            entry.RegularPay < 0 ||
            entry.OvertimePay < 0 ||
            entry.NightDifferentialPay < 0 ||
            entry.Allowances < 0 ||
            entry.ServiceChargeShare < 0 ||
            entry.OtherEarnings < 0 ||
            entry.EmployerCost < 0 ||
            entry.Deductions < 0;
    }
}
