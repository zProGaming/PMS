using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Departments;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Department Department { get; set; } = default!;

    public SelectList PropertyOptions { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var department = await _context.Departments.FindAsync(id);
        if (department is null)
        {
            return NotFound();
        }

        Department = department;
        await LoadPropertyOptionsAsync(Department.PropertyId);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadPropertyOptionsAsync(Department.PropertyId);
            return Page();
        }

        _context.Attach(Department).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await DepartmentExistsAsync(Department.Id))
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

    private Task<bool> DepartmentExistsAsync(int id)
    {
        return _context.Departments.AnyAsync(department => department.Id == id);
    }
}
