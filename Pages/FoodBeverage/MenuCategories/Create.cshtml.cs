using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.MenuCategories;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public MenuCategory MenuCategory { get; set; } = new() { IsActive = true };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.MenuCategories.Add(MenuCategory);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
