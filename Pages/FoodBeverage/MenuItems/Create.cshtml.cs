using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.MenuItems;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public MenuItem MenuItem { get; set; } = new()
    {
        IsAvailable = true,
        IsTaxable = true,
        IsServiceChargeable = true
    };

    public SelectList CategoryOptions { get; set; } = default!;

    public SelectList KitchenStationOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateMenuItem();

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(MenuItem.MenuCategoryId, MenuItem.KitchenStationId);
            return Page();
        }

        _context.MenuItems.Add(MenuItem);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private void ValidateMenuItem()
    {
        if (MenuItem.Price < 0)
        {
            ModelState.AddModelError("MenuItem.Price", "Price cannot be negative.");
        }
    }

    private async Task LoadOptionsAsync(object? selectedCategory = null, object? selectedKitchenStation = null)
    {
        var categories = await _context.MenuCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => new { category.Id, category.Name })
            .ToListAsync();

        var stations = await _context.KitchenStations
            .AsNoTracking()
            .Where(station => station.IsActive)
            .OrderBy(station => station.Name)
            .Select(station => new { station.Id, station.Name })
            .ToListAsync();

        CategoryOptions = new SelectList(categories, "Id", "Name", selectedCategory);
        KitchenStationOptions = new SelectList(stations, "Id", "Name", selectedKitchenStation);
    }
}
