using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RatePlans;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public RatePlan RatePlan { get; set; } = new() { IsActive = true };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        RatePlan.Code = RatePlan.Code.Trim().ToUpperInvariant();
        RatePlan.CreatedAt = DateTime.Now;
        RatePlan.CreatedBy = User.Identity?.Name ?? Environment.UserName;

        _context.RatePlans.Add(RatePlan);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
