using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Items;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public InventoryItem InventoryItem { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var item = await _context.InventoryItems
            .Include(inventoryItem => inventoryItem.InventoryCategory)
            .FirstOrDefaultAsync(inventoryItem => inventoryItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        InventoryItem = item;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var item = await _context.InventoryItems.FindAsync(InventoryItem.Id);
        if (item is not null)
        {
            item.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
