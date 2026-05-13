using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Pages.Admin.RoomTypes;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public RoomType RoomType { get; set; } = new() { IsActive = true };

    public SelectList PropertyOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadPropertyOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadPropertyOptionsAsync(RoomType.PropertyId);
            return Page();
        }

        _context.RoomTypes.Add(RoomType);
        await _context.SaveChangesAsync();

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
}
