using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RoomInventoryControls;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public RoomInventoryControl RoomInventoryControl { get; set; } = default!;

    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var roomInventoryControl = await _context.RoomInventoryControls.FindAsync(id);
        if (roomInventoryControl == null)
        {
            return NotFound();
        }

        RoomInventoryControl = roomInventoryControl;
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (RoomInventoryControl.RoomsToSell + RoomInventoryControl.OverbookingLimit < 0)
        {
            ModelState.AddModelError("RoomInventoryControl.RoomsToSell", "Sellable inventory cannot be negative.");
        }

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync();
            return Page();
        }

        RoomInventoryControl.InventoryDate = RoomInventoryControl.InventoryDate.Date;
        _context.Attach(RoomInventoryControl).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.RoomInventoryControls.AnyAsync(e => e.Id == RoomInventoryControl.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        RoomTypeOptions = new SelectList(
            await _context.RoomTypes.OrderBy(r => r.Code).ToListAsync(),
            "Id",
            "Code",
            RoomInventoryControl.RoomTypeId);
    }
}
