using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.ManagementAI;

namespace Vantage.PMS.Pages.ManagementAI.Settings;

[Authorize(Roles = PmsRoles.SystemAdmin)]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public AIIntegrationSetting Setting { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadSettingAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await _context.AIIntegrationSettings.FirstOrDefaultAsync();
        if (existing is null)
        {
            Setting.CreatedAt = DateTime.Now;
            Setting.UpdatedAt = DateTime.Now;
            Setting.ApiKeyConfigured = false;
            _context.AIIntegrationSettings.Add(Setting);
        }
        else
        {
            existing.ProviderName = Setting.ProviderName;
            existing.IsEnabled = false;
            existing.ModelName = Setting.ModelName;
            existing.ApiKeyConfigured = false;
            existing.Notes = Setting.Notes;
            existing.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        StatusMessage = "AI integration settings saved. External AI remains disabled for the MVP.";
        return RedirectToPage();
    }

    private async Task LoadSettingAsync()
    {
        Setting = await _context.AIIntegrationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync()
            ?? new AIIntegrationSetting
            {
                ProviderName = "Rule-Based MVP",
                IsEnabled = false,
                ApiKeyConfigured = false,
                Notes = "External AI integration is disabled in MVP. Rule-based recommendations are currently used."
            };
    }
}
