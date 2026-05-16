using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Items;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public InventoryItem InventoryItem { get; set; } = new() { IsActive = true, CreatedAt = DateTime.Now };

    public SelectList CategoryOptions { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync()
    {
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

        InventoryItem.CreatedAt = DateTime.Now;
        InventoryItem.CreatedBy = User.Identity?.Name ?? "System";
        _context.InventoryItems.Add(InventoryItem);
        await _context.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        var categories = await _context.InventoryCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.Name)
            .ToListAsync();

        CategoryOptions = new SelectList(categories, "Id", "Name");
    }
}
