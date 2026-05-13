using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.Departments;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<Department> Departments { get; set; } = new List<Department>();

    public async Task OnGetAsync()
    {
        Departments = await _context.Departments
            .Include(department => department.Property)
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync();
    }
}
