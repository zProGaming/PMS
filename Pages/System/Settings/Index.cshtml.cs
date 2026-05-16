using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Pages.System.Settings;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public IList<SystemSetting> Settings { get; set; } = new List<SystemSetting>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        foreach (var input in Settings)
        {
            var setting = await _context.SystemSettings.FindAsync(input.Id);
            if (setting is null || !setting.IsEditable)
            {
                continue;
            }

            setting.SettingValue = input.SettingValue ?? string.Empty;
            setting.Description = input.Description;
            setting.UpdatedAt = DateTime.Now;
            setting.UpdatedBy = User.Identity?.Name ?? "System";
        }

        await _context.SaveChangesAsync();
        StatusMessage = "System settings saved.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Settings = await _context.SystemSettings
            .AsNoTracking()
            .OrderBy(setting => setting.Module)
            .ThenBy(setting => setting.SettingKey)
            .ToListAsync();
    }
}
