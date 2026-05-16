using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Categories;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    [BindProperty] public InventoryCategory Category { get; set; } = default!;
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Category = await _context.InventoryCategories.FindAsync(id) ?? new InventoryCategory();
        return Category.Id == 0 ? NotFound() : Page();
    }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        _context.Attach(Category).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
