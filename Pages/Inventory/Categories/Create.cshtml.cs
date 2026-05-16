using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Inventory.Categories;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    [BindProperty] public InventoryCategory Category { get; set; } = new() { IsActive = true };
    public IActionResult OnGet() => Page();
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        _context.InventoryCategories.Add(Category);
        await _context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
