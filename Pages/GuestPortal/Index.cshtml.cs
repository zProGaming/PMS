using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortal;

public class IndexModel(GuestPortalService guestPortalService) : PageModel
{
    private readonly GuestPortalService _guestPortalService = guestPortalService;

    [BindProperty]
    public string Reference { get; set; } = string.Empty;

    [BindProperty]
    public string? GuestEmail { get; set; }

    [BindProperty]
    public string? GuestPhone { get; set; }

    public GuestPortalSetting Setting { get; set; } = new();

    public async Task OnGetAsync()
    {
        Setting = await _guestPortalService.GetSettingsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Setting = await _guestPortalService.GetSettingsAsync();
        var result = await _guestPortalService.LookupAsync(Reference, GuestEmail, GuestPhone);
        if (!result.Succeeded || result.Access is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }

        return RedirectToPage("Home", new { token = result.Access.AccessToken });
    }
}
