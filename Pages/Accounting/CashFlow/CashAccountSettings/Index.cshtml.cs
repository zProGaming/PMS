using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.CashFlow.CashAccountSettings;

public class IndexModel(ApplicationDbContext context, AuditLogService auditLogService) : PageModel
{
    public IList<CashAccountSetting> Settings { get; private set; } = [];

    public SelectList AccountOptions { get; private set; } = default!;

    [BindProperty]
    public CashAccountSetting Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (Input.GLAccountId <= 0)
        {
            ModelState.AddModelError("Input.GLAccountId", "Select an active GL account.");
        }

        var account = await context.GLAccounts.FirstOrDefaultAsync(gl => gl.Id == Input.GLAccountId && gl.IsActive);
        if (account is null)
        {
            ModelState.AddModelError("Input.GLAccountId", "Cash account setting must point to an active GL account.");
        }

        if (await context.CashAccountSettings.AnyAsync(setting => setting.GLAccountId == Input.GLAccountId))
        {
            ModelState.AddModelError("Input.GLAccountId", "This GL account is already configured as a cash account.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.AccountName = $"{account!.AccountCode} - {account.AccountName}";
        Input.IsCashEquivalent = Input.IsCashEquivalent || Input.IsCashOnHand || Input.IsCashInBank || Input.IsEWallet;
        Input.IsActive = true;
        context.CashAccountSettings.Add(Input);
        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Accounting", nameof(CashAccountSetting), Input.Id.ToString(), null, Input, User.Identity?.Name);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync(
        int id,
        bool isCashOnHand,
        bool isCashInBank,
        bool isEWallet,
        bool isCashEquivalent,
        bool isActive,
        string? notes)
    {
        var setting = await context.CashAccountSettings.Include(item => item.GLAccount).FirstOrDefaultAsync(item => item.Id == id);
        if (setting is null)
        {
            return RedirectToPage();
        }

        var oldValues = new
        {
            setting.IsCashOnHand,
            setting.IsCashInBank,
            setting.IsEWallet,
            setting.IsCashEquivalent,
            setting.IsActive,
            setting.Notes
        };

        setting.IsCashOnHand = isCashOnHand;
        setting.IsCashInBank = isCashInBank;
        setting.IsEWallet = isEWallet;
        setting.IsCashEquivalent = isCashEquivalent || isCashOnHand || isCashInBank || isEWallet;
        setting.IsActive = isActive && (setting.GLAccount?.IsActive ?? false);
        setting.Notes = notes;
        await context.SaveChangesAsync();

        await auditLogService.LogAsync(AuditActionType.Update, "Accounting", nameof(CashAccountSetting), setting.Id.ToString(), oldValues, setting, User.Identity?.Name);
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Settings = await context.CashAccountSettings
            .AsNoTracking()
            .Include(setting => setting.GLAccount)
            .OrderBy(setting => setting.GLAccount!.AccountCode)
            .ToListAsync();

        var configuredIds = Settings.Select(setting => setting.GLAccountId).ToHashSet();
        var accounts = await context.GLAccounts
            .AsNoTracking()
            .Where(account => account.IsActive && account.AccountType == GLAccountType.Asset && !configuredIds.Contains(account.Id))
            .OrderBy(account => account.AccountCode)
            .Select(account => new { account.Id, Name = $"{account.AccountCode} - {account.AccountName}" })
            .ToListAsync();
        AccountOptions = new SelectList(accounts, "Id", "Name");
    }
}
