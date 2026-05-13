using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Admin.RoomTypes;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public RoomType RoomType { get; set; } = default!;

    public SelectList PropertyOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var roomType = await _context.RoomTypes.FindAsync(id);
        if (roomType is null)
        {
            return NotFound();
        }

        RoomType = roomType;
        await LoadPropertyOptionsAsync(RoomType.PropertyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadPropertyOptionsAsync(RoomType.PropertyId);
            return Page();
        }

        _context.Attach(RoomType).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await RoomTypeExistsAsync(RoomType.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToPage("./Index");
    }

    private async Task LoadPropertyOptionsAsync(object? selectedProperty = null)
    {
        var properties = await _context.Properties
            .AsNoTracking()
            .OrderBy(property => property.Name)
            .Select(property => new { property.Id, property.Name })
            .ToListAsync();

        PropertyOptions = new SelectList(properties, "Id", "Name", selectedProperty);
    }

    private Task<bool> RoomTypeExistsAsync(int id)
    {
        return _context.RoomTypes.AnyAsync(roomType => roomType.Id == id);
    }
}
