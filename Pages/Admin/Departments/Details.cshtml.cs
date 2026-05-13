using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Departments;

public class DetailsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

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
}
