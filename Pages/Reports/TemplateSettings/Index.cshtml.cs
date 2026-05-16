using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Reports;

namespace Vantage.PMS.Pages.Reports.TemplateSettings;

[Authorize(Policy = PmsPolicies.ReportAdministration)]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<ReportTemplateSetting> Settings { get; private set; } = [];

    [BindProperty]
    public TemplateInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task OnGetEditAsync(int id)
    {
        var setting = await context.ReportTemplateSettings.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
        if (setting is not null)
        {
            Input = new TemplateInput
            {
                Id = setting.Id,
                ReportKey = setting.ReportKey,
                ReportName = setting.ReportName,
                ReportCategory = setting.ReportCategory,
                HeaderTitle = setting.HeaderTitle,
                FooterText = setting.FooterText,
                ShowLogo = setting.ShowLogo,
                ShowHotelName = setting.ShowHotelName,
                ShowPreparedBy = setting.ShowPreparedBy,
                ShowReviewedBy = setting.ShowReviewedBy,
                ShowGeneratedDate = setting.ShowGeneratedDate,
                ShowDateRange = setting.ShowDateRange,
                ShowDisclaimer = setting.ShowDisclaimer,
                DisclaimerText = setting.DisclaimerText,
                IsLandscape = setting.IsLandscape,
                IsActive = setting.IsActive
            };
        }

        await LoadAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var duplicate = await context.ReportTemplateSettings.AnyAsync(setting =>
            setting.ReportKey == Input.ReportKey && setting.Id != Input.Id);
        if (duplicate)
        {
            ModelState.AddModelError($"{nameof(Input)}.{nameof(Input.ReportKey)}", "Report key must be unique.");
            await LoadAsync();
            return Page();
        }

        var entity = Input.Id == 0
            ? new ReportTemplateSetting { CreatedAt = DateTime.Now }
            : await context.ReportTemplateSettings.FirstOrDefaultAsync(setting => setting.Id == Input.Id);
        if (entity is null)
        {
            return RedirectToPage();
        }

        entity.ReportKey = Input.ReportKey.Trim();
        entity.ReportName = Input.ReportName.Trim();
        entity.ReportCategory = Input.ReportCategory;
        entity.HeaderTitle = Input.HeaderTitle.Trim();
        entity.FooterText = Input.FooterText;
        entity.ShowLogo = Input.ShowLogo;
        entity.ShowHotelName = Input.ShowHotelName;
        entity.ShowPreparedBy = Input.ShowPreparedBy;
        entity.ShowReviewedBy = Input.ShowReviewedBy;
        entity.ShowGeneratedDate = Input.ShowGeneratedDate;
        entity.ShowDateRange = Input.ShowDateRange;
        entity.ShowDisclaimer = Input.ShowDisclaimer;
        entity.DisclaimerText = Input.DisclaimerText;
        entity.IsLandscape = Input.IsLandscape;
        entity.IsActive = Input.IsActive;
        entity.UpdatedAt = DateTime.Now;

        if (entity.Id == 0)
        {
            context.ReportTemplateSettings.Add(entity);
        }

        await context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var setting = await context.ReportTemplateSettings.FirstOrDefaultAsync(item => item.Id == id);
        if (setting is not null)
        {
            setting.IsActive = !setting.IsActive;
            setting.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Settings = await context.ReportTemplateSettings
            .AsNoTracking()
            .OrderBy(setting => setting.ReportCategory)
            .ThenBy(setting => setting.ReportName)
            .ToListAsync();
    }

    public class TemplateInput
    {
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string ReportKey { get; set; } = string.Empty;

        [Required]
        [StringLength(180)]
        public string ReportName { get; set; } = string.Empty;

        public ReportCategory ReportCategory { get; set; } = ReportCategory.Other;

        [Required]
        [StringLength(180)]
        public string HeaderTitle { get; set; } = string.Empty;

        public string? FooterText { get; set; }

        public bool ShowLogo { get; set; } = true;

        public bool ShowHotelName { get; set; } = true;

        public bool ShowPreparedBy { get; set; } = true;

        public bool ShowReviewedBy { get; set; }

        public bool ShowGeneratedDate { get; set; } = true;

        public bool ShowDateRange { get; set; } = true;

        public bool ShowDisclaimer { get; set; } = true;

        public string? DisclaimerText { get; set; }

        public bool IsLandscape { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
