using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vantage.PMS.Pages.Executive.KPIBenchmarks;

public class EditModel : PageModel
{
    public IActionResult OnGet(int id) => RedirectToPage("/Executive/KPIBenchmarks/Index", new { id });
}
