using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Departments;

public class DeleteModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public Department Department { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var department = await _context.Departments
            .Include(department => department.Property)
            .AsNoTracking()
            .FirstOrDefaultAsync(department => department.Id == id);

        if (department is null)
        {
            return NotFound();
        }

        Department = department;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var department = await _context.Departments.FindAsync(id);
        if (department is not null)
        {
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
