using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.Outlets;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Outlet Outlet { get; set; } = new() { IsActive = true };

    public IEnumerable<SelectListItem> OutletTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IActionResult OnGet()
    {
        LoadOutletTypeOptions();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            LoadOutletTypeOptions();
            return Page();
        }

        _context.Outlets.Add(Outlet);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private void LoadOutletTypeOptions()
    {
        OutletTypeOptions = Enum.GetValues<OutletType>().Select(type => new SelectListItem
        {
            Value = type.ToString(),
            Text = type.ToString(),
            Selected = type == Outlet.OutletType
        });
    }
}
