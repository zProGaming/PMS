using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Items;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public InventoryItem InventoryItem { get; set; } = new();

    public SelectList CategoryOptions { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        InventoryItem = item;
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        _context.Attach(InventoryItem).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        var categories = await _context.InventoryCategories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ToListAsync();

        CategoryOptions = new SelectList(categories, "Id", "Name", InventoryItem.InventoryCategoryId);
    }
}
