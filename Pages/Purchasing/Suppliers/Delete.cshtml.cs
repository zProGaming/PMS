using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Pages.Purchasing.Suppliers;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Supplier Supplier { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier is null)
        {
            return NotFound();
        }

        Supplier = supplier;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var supplier = await _context.Suppliers.FindAsync(Supplier.Id);
        if (supplier is not null)
        {
            supplier.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
