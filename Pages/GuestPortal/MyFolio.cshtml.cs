using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortal;

public class MyFolioModel(GuestPortalService guestPortalService) : PageModel
{
    private readonly GuestPortalService _guestPortalService = guestPortalService;

    public string Token { get; set; } = string.Empty;
    public GuestPortalAccess? Access { get; set; }
    public GuestPortalSetting Setting { get; set; } = new();
    public Folio? Folio { get; set; }

    public async Task OnGetAsync(string token)
    {
        Token = token;
        Setting = await _guestPortalService.GetSettingsAsync();
        Access = await _guestPortalService.GetAccessAsync(token);
        if (Access is not null)
        {
            Folio = _guestPortalService.GetPrimaryFolio(Access);
        }
    }
}
