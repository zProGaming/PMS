using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.CashFlow.MappingRules;

public class IndexModel(ApplicationDbContext context, AuditLogService auditLogService) : PageModel
{
    public IList<CashFlowMappingRule> Rules { get; private set; } = [];

    public SelectList AccountOptions { get; private set; } = default!;

    public SelectList CategoryOptions { get; private set; } = default!;

    [BindProperty]
    public CashFlowMappingRule Input { get; set; } = new();

    public async Task OnGetAsync(int? glAccountId, SourceModule? sourceModule, SourceTransactionType? sourceTransactionType)
    {
        Input.GLAccountId = glAccountId;
        Input.SourceModule = sourceModule;
        Input.SourceTransactionType = sourceTransactionType;
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        Input.RuleName = (Input.RuleName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(Input.RuleName))
        {
            ModelState.AddModelError("Input.RuleName", "Rule name is required.");
        }

        if (Input.CashFlowCategoryId <= 0)
        {
            ModelState.AddModelError("Input.CashFlowCategoryId", "Cash flow category is required.");
        }

        if (Input.GLAccountId is null && Input.SourceModule is null && Input.SourceTransactionType is null)
        {
            ModelState.AddModelError(string.Empty, "Map at least one GL account, source module, or source transaction type.");
        }

        var category = await context.CashFlowCategories.FirstOrDefaultAsync(item => item.Id == Input.CashFlowCategoryId && item.IsActive);
        if (category is null)
        {
            ModelState.AddModelError("Input.CashFlowCategoryId", "Select an active cash flow category.");
        }

        if (Input.GLAccountId is not null && !await context.GLAccounts.AnyAsync(account => account.Id == Input.GLAccountId.Value && account.IsActive))
        {
            ModelState.AddModelError("Input.GLAccountId", "GL account must be active.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        Input.CashFlowSection = category!.CashFlowSection;
        Input.IsActive = true;
        Input.CreatedAt = DateTime.Now;
        Input.CreatedBy = User.Identity?.Name ?? "System";
        context.CashFlowMappingRules.Add(Input);
        await context.SaveChangesAsync();

        await auditLogService.LogAsync(AuditActionType.Create, "Accounting", nameof(CashFlowMappingRule), Input.Id.ToString(), null, Input, User.Identity?.Name);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var rule = await context.CashFlowMappingRules.FindAsync(id);
        if (rule is not null)
        {
            var old = new { rule.IsActive };
            rule.IsActive = !rule.IsActive;
            await context.SaveChangesAsync();
            await auditLogService.LogAsync(AuditActionType.Update, "Accounting", nameof(CashFlowMappingRule), rule.Id.ToString(), old, new { rule.IsActive }, User.Identity?.Name);
        }

        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        Rules = await context.CashFlowMappingRules
            .AsNoTracking()
            .Include(rule => rule.GLAccount)
            .Include(rule => rule.CashFlowCategory)
            .OrderBy(rule => rule.CashFlowSection)
            .ThenBy(rule => rule.CashFlowCategory!.SortOrder)
            .ThenBy(rule => rule.RuleName)
            .ToListAsync();

        var accounts = await context.GLAccounts
            .AsNoTracking()
            .Where(account => account.IsActive)
            .OrderBy(account => account.AccountCode)
            .Select(account => new { account.Id, Name = $"{account.AccountCode} - {account.AccountName}" })
            .ToListAsync();
        AccountOptions = new SelectList(accounts, "Id", "Name", Input.GLAccountId);

        var categories = await context.CashFlowCategories
            .AsNoTracking()
            .Where(category => category.IsActive && !category.IsSubtotal)
            .OrderBy(category => category.CashFlowSection)
            .ThenBy(category => category.SortOrder)
            .Select(category => new { category.Id, Name = $"{category.CashFlowSection} - {category.Name}" })
            .ToListAsync();
        CategoryOptions = new SelectList(categories, "Id", "Name", Input.CashFlowCategoryId);
    }
}
