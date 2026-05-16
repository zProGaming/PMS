using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Categories;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    [BindProperty] public InventoryCategory Category { get; set; } = default!;
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Category = await _context.InventoryCategories.FindAsync(id) ?? new InventoryCategory();
        return Category.Id == 0 ? NotFound() : Page();
    }
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var category = await _context.InventoryCategories.FindAsync(id);
        if (category is not null)
        {
            category.IsActive = false;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage("Index");
    }
}
