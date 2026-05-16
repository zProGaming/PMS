using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Labor;

namespace Vantage.PMS.Pages.Labor.EmployeeCostProfiles;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public EmployeeCostProfile Input { get; set; } = new();

    public IList<EmployeeCostProfile> Employees { get; private set; } = [];

    public SelectList DepartmentOptions { get; private set; } = default!;

    public SelectList USALIOptions { get; private set; } = default!;

    public SelectList LaborAccountOptions { get; private set; } = default!;

    public SelectList LiabilityAccountOptions { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.FullName))
        {
            ModelState.AddModelError("Input.FullName", "Full name is required.");
        }

        if (!string.IsNullOrWhiteSpace(Input.EmployeeCode) &&
            await context.EmployeeCostProfiles.AnyAsync(employee => employee.EmployeeCode == Input.EmployeeCode.Trim()))
        {
            ModelState.AddModelError("Input.EmployeeCode", "Employee code already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.EmployeeCode = Input.EmployeeCode?.Trim() ?? string.Empty;
        Input.FullName = Input.FullName.Trim();
        Input.CreatedAt = DateTime.Now;
        Input.CreatedBy = User.Identity?.Name ?? "System";
        Input.IsActive = true;
        context.EmployeeCostProfiles.Add(Input);
        await context.SaveChangesAsync();
        StatusMessage = "Employee cost profile created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, int? departmentId, int? usaliDepartmentId, int? defaultLaborGLAccountId, int? defaultPayrollLiabilityGLAccountId, string? position, EmploymentType employmentType, string? notes, bool isActive)
    {
        var employee = await context.EmployeeCostProfiles.FindAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        employee.DepartmentId = departmentId;
        employee.USALIDepartmentId = usaliDepartmentId;
        employee.DefaultLaborGLAccountId = defaultLaborGLAccountId;
        employee.DefaultPayrollLiabilityGLAccountId = defaultPayrollLiabilityGLAccountId;
        employee.Position = position;
        employee.EmploymentType = employmentType;
        employee.Notes = notes;
        employee.IsActive = isActive;
        await context.SaveChangesAsync();
        StatusMessage = "Employee cost profile updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var employee = await context.EmployeeCostProfiles.FindAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        employee.IsActive = false;
        await context.SaveChangesAsync();
        StatusMessage = "Employee cost profile deactivated.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Employees = await context.EmployeeCostProfiles
            .AsNoTracking()
            .Include(employee => employee.Department)
            .Include(employee => employee.USALIDepartment)
            .Include(employee => employee.DefaultLaborGLAccount)
            .Include(employee => employee.DefaultPayrollLiabilityGLAccount)
            .OrderBy(employee => employee.FullName)
            .ToListAsync();
        DepartmentOptions = new SelectList(await context.Departments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.Name).ToListAsync(), "Id", "Name");
        USALIOptions = new SelectList(await context.USALIDepartments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.SortOrder).ToListAsync(), "Id", "Name");
        LaborAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive && account.AccountType == GLAccountType.Expense).OrderBy(account => account.AccountCode).ToListAsync(), "Id", "AccountName");
        LiabilityAccountOptions = new SelectList(await context.GLAccounts.AsNoTracking().Where(account => account.IsActive && account.AccountType == GLAccountType.Liability).OrderBy(account => account.AccountCode).ToListAsync(), "Id", "AccountName");
    }
}
