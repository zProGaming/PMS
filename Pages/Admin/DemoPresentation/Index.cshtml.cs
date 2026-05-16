using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vantage.PMS.Pages.Admin.DemoPresentation;

public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/System/DemoPresentation/Index");
    }
}
