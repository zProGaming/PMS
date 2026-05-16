using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.USALIDepartments;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<USALIDepartment> Departments { get; private set; } = [];

    [BindProperty]
    public USALIDepartment Input { get; set; } = new();

    public async Task OnGetAsync() => Departments = await context.USALIDepartments.AsNoTracking().OrderBy(item => item.SortOrder).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Code) || string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError(string.Empty, "Code and name are required.");
        }

        if (await context.USALIDepartments.AnyAsync(item => item.Code == Input.Code))
        {
            ModelState.AddModelError("Input.Code", "Code must be unique.");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        Input.IsActive = true;
        context.USALIDepartments.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }
}
