using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive.OwnerPackages;

public class PrintModel(OwnerReportPackageService packageService) : PageModel
{
    public OwnerPackagePrintView? View { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        View = await packageService.GetPrintViewAsync(id);
        return View is null ? NotFound() : Page();
    }
}
