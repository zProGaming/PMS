using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Pages.FoodBeverage.DiningTables;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public DiningTable DiningTable { get; set; } = default!;

    public SelectList OutletOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var table = await _context.DiningTables.FindAsync(id);
        if (table is null)
        {
            return NotFound();
        }

        DiningTable = table;
        await LoadOptionsAsync(DiningTable.OutletId, DiningTable.Status);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateDiningTable();

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(DiningTable.OutletId, DiningTable.Status);
            return Page();
        }

        _context.Attach(DiningTable).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    private void ValidateDiningTable()
    {
        if (DiningTable.SeatingCapacity <= 0)
        {
            ModelState.AddModelError("DiningTable.SeatingCapacity", "Seating capacity must be greater than zero.");
        }
    }

    private async Task LoadOptionsAsync(object? selectedOutlet = null, DiningTableStatus selectedStatus = DiningTableStatus.Available)
    {
        var outlets = await _context.Outlets
            .AsNoTracking()
            .OrderBy(outlet => outlet.Name)
            .Select(outlet => new { outlet.Id, outlet.Name })
            .ToListAsync();

        OutletOptions = new SelectList(outlets, "Id", "Name", selectedOutlet);
        StatusOptions = Enum.GetValues<DiningTableStatus>().Select(status => new SelectListItem
        {
            Value = status.ToString(),
            Text = status.ToString(),
            Selected = status == selectedStatus
        });
    }
}
