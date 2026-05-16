using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vantage.PMS.Pages.Executive.KPIBenchmarks;

public class CreateModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Executive/KPIBenchmarks/Index");
}
