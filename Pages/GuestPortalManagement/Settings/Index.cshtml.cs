using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.GuestPortal;

namespace Vantage.PMS.Pages.GuestPortalManagement.Settings;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public GuestPortalSetting Setting { get; set; } = new();

    public async Task OnGetAsync()
    {
        Setting = await _context.GuestPortalSettings.OrderBy(setting => setting.Id).FirstOrDefaultAsync()
            ?? new GuestPortalSetting();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Setting.UpdatedAt = DateTime.Now;
        if (Setting.Id == 0)
        {
            Setting.CreatedAt = DateTime.Now;
            _context.GuestPortalSettings.Add(Setting);
        }
        else
        {
            _context.Attach(Setting).State = EntityState.Modified;
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}
