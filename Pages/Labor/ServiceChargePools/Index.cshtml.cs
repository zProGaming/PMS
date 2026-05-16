using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Labor.ServiceChargePools;

public class IndexModel(ApplicationDbContext context, LaborCostingService laborCostingService) : PageModel
{
    [BindProperty]
    public ServiceChargePool Input { get; set; } = new()
    {
        PeriodStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
        PeriodEnd = DateTime.Today
    };

    public IList<ServiceChargePool> Pools { get; private set; } = [];

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
        if (string.IsNullOrWhiteSpace(Input.PoolName))
        {
            ModelState.AddModelError("Input.PoolName", "Pool name is required.");
        }

        if (Input.PeriodStart.Date > Input.PeriodEnd.Date)
        {
            ModelState.AddModelError("Input.PeriodEnd", "Period end must be on or after period start.");
        }

        if (Input.TotalServiceChargeCollected < 0)
        {
            ModelState.AddModelError("Input.TotalServiceChargeCollected", "Total service charge cannot be negative.");
        }

        if (await context.ServiceChargePools.AnyAsync(pool => pool.PoolName == Input.PoolName))
        {
            ModelState.AddModelError("Input.PoolName", "Pool name already exists.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.PreparedBy = User.Identity?.Name ?? "System";
        Input.Status = ServiceChargePoolStatus.Draft;
        context.ServiceChargePools.Add(Input);
        await context.SaveChangesAsync();
        StatusMessage = "Service charge pool created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSubmitAsync(int id)
    {
        var pool = await context.ServiceChargePools.FindAsync(id);
        if (pool is null)
        {
            return NotFound();
        }

        if (pool.Status == ServiceChargePoolStatus.Draft)
        {
            pool.Status = ServiceChargePoolStatus.ForApproval;
            await context.SaveChangesAsync();
            StatusMessage = "Service charge pool submitted.";
        }
        else
        {
            ErrorMessage = "Only draft service charge pools can be submitted.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerateAsync(int id)
    {
        var errors = await laborCostingService.GenerateServiceChargeDistributionAsync(id);
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(" ", errors);
        }
        else
        {
            StatusMessage = "Distribution lines generated.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostApproveAsync(int id)
    {
        var errors = await laborCostingService.ApproveServiceChargePoolAsync(id, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(" ", errors);
        }
        else
        {
            StatusMessage = "Service charge pool approved.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPostAsync(int id)
    {
        var errors = await laborCostingService.PostServiceChargePoolAsync(id, User.Identity?.Name ?? "System");
        if (errors.Count > 0)
        {
            ErrorMessage = string.Join(" ", errors);
        }
        else
        {
            StatusMessage = "Service charge pool posted.";
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Pools = await context.ServiceChargePools
            .AsNoTracking()
            .Include(pool => pool.DistributionLines)
            .Include(pool => pool.JournalEntry)
            .OrderByDescending(pool => pool.PeriodEnd)
            .Take(200)
            .ToListAsync();
    }
}
