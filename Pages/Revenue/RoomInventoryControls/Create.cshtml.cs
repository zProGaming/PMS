using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Pages.Revenue.RoomInventoryControls;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public RoomInventoryControl RoomInventoryControl { get; set; } = new()
    {
        InventoryDate = DateTime.Today
    };

    public SelectList RoomTypeOptions { get; set; } = default!;

    public async Task OnGetAsync()
    {
        await LoadOptionsAsync();
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
        _context.RoomInventoryControls.Add(RoomInventoryControl);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }

    private async Task LoadOptionsAsync()
    {
        RoomTypeOptions = new SelectList(
            await _context.RoomTypes.OrderBy(r => r.Code).ToListAsync(),
            "Id",
            "Code");
    }
}
