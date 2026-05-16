using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;

namespace Vantage.PMS.Pages.Accounting.USALIReportLines;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<USALIReportLine> Lines { get; private set; } = [];

    [BindProperty]
    public USALIReportLine Input { get; set; } = new();

    public async Task OnGetAsync() => Lines = await context.USALIReportLines.AsNoTracking().OrderBy(item => item.SortOrder).ToListAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Code) || string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError(string.Empty, "Code and name are required.");
        }

        if (await context.USALIReportLines.AnyAsync(item => item.Code == Input.Code))
        {
            ModelState.AddModelError("Input.Code", "Code must be unique.");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        Input.IsActive = true;
        context.USALIReportLines.Add(Input);
        await context.SaveChangesAsync();
        return RedirectToPage();
    }
}
