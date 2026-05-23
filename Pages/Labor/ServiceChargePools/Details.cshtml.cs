using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Labor.ServiceChargePools;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public ServiceChargeDistributionLine Input { get; set; } = new();

    public ServiceChargePool? Pool { get; private set; }

    public SelectList EmployeeOptions { get; private set; } = default!;

    public SelectList DepartmentOptions { get; private set; } = default!;

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadAsync(id);
        return Pool is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAddLineAsync(int id)
    {
        await LoadAsync(id);
        if (Pool is null)
        {
            return NotFound();
        }

        if (Pool.Status is ServiceChargePoolStatus.Posted or ServiceChargePoolStatus.Cancelled)
        {
            ModelState.AddModelError(string.Empty, "Posted or cancelled pools cannot be edited.");
        }

        if (Input.Amount < 0 || Input.EligibleDays < 0 || Input.EligibleHours < 0 || Input.DistributionPercentage < 0)
        {
            ModelState.AddModelError(string.Empty, "Distribution values cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Input.EmployeeCostProfileId is not null)
        {
            var employee = await context.EmployeeCostProfiles.AsNoTracking().FirstOrDefaultAsync(item => item.Id == Input.EmployeeCostProfileId);
            if (!ServiceChargeEligibility.IsEligible(employee))
            {
                ModelState.AddModelError("Input.EmployeeCostProfileId", "Selected employee profile is inactive, agency, or managerial/executive. Review service-charge eligibility before adding this line.");
            }

            Input.DepartmentId ??= employee?.DepartmentId;
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Input.ServiceChargePoolId = id;
        context.ServiceChargeDistributionLines.Add(Input);
        await context.SaveChangesAsync();
        StatusMessage = "Distribution line added.";
        return RedirectToPage(new { id });
    }

    private async Task LoadAsync(int id)
    {
        Pool = await context.ServiceChargePools
            .AsNoTracking()
            .Include(pool => pool.DistributionLines)
            .ThenInclude(line => line.EmployeeCostProfile)
            .Include(pool => pool.DistributionLines)
            .ThenInclude(line => line.Department)
            .Include(pool => pool.JournalEntry)
            .FirstOrDefaultAsync(pool => pool.Id == id);
        var eligibleEmployees = (await context.EmployeeCostProfiles
                .AsNoTracking()
                .Where(employee => employee.IsActive)
                .OrderBy(employee => employee.FullName)
                .ToListAsync())
            .Where(ServiceChargeEligibility.IsEligible)
            .Select(employee => new
            {
                employee.Id,
                Label = string.IsNullOrWhiteSpace(employee.Position)
                    ? employee.FullName
                    : $"{employee.FullName} - {employee.Position}"
            })
            .ToList();
        EmployeeOptions = new SelectList(eligibleEmployees, "Id", "Label");
        DepartmentOptions = new SelectList(await context.Departments.AsNoTracking().Where(department => department.IsActive).OrderBy(department => department.Name).ToListAsync(), "Id", "Name");
    }
}
