using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.Outlets;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Outlet Outlet { get; set; } = default!;

    public IEnumerable<SelectListItem> OutletTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var outlet = await _context.Outlets.FindAsync(id);
        if (outlet is null)
        {
            return NotFound();
        }

        Outlet = outlet;
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

        _context.Attach(Outlet).State = EntityState.Modified;
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
