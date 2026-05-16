using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Accounting.Reports;

public class CashMovementModel(ApplicationDbContext context, CashFlowReportService cashFlowReportService) : PageModel
{
    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public int? CashAccountId { get; private set; }

    public CashFlowSection? Section { get; private set; }

    public int? CategoryId { get; private set; }

    public SourceModule? SourceModule { get; private set; }

    public string? MappingStatus { get; private set; }

    public IList<CashMovementRow> Rows { get; private set; } = [];

    public SelectList CashAccountOptions { get; private set; } = default!;

    public SelectList CategoryOptions { get; private set; } = default!;

    public async Task OnGetAsync(
        DateTime? startDate,
        DateTime? endDate,
        int? cashAccountId,
        CashFlowSection? section,
        int? categoryId,
        SourceModule? sourceModule,
        string? mappingStatus)
    {
        var today = DateTime.Today;
        StartDate = startDate?.Date ?? new DateTime(today.Year, today.Month, 1);
        EndDate = endDate?.Date ?? today;
        if (EndDate < StartDate)
        {
            (StartDate, EndDate) = (EndDate, StartDate);
        }

        CashAccountId = cashAccountId;
        Section = section;
        CategoryId = categoryId;
        SourceModule = sourceModule;
        MappingStatus = mappingStatus;
        var mapped = mappingStatus switch
        {
            "mapped" => true,
            "unmapped" => false,
            _ => (bool?)null
        };

        Rows = await cashFlowReportService.GetCashMovementsAsync(StartDate, EndDate, CashAccountId, Section, CategoryId, SourceModule, mapped);
        await LoadOptionsAsync();
    }

    private async Task LoadOptionsAsync()
    {
        var cashAccounts = await context.CashAccountSettings
            .AsNoTracking()
            .Where(setting => setting.IsActive)
            .OrderBy(setting => setting.AccountName)
            .Select(setting => new { setting.GLAccountId, setting.AccountName })
            .ToListAsync();
        CashAccountOptions = new SelectList(cashAccounts, "GLAccountId", "AccountName", CashAccountId);

        var categories = await context.CashFlowCategories
            .AsNoTracking()
            .Where(category => category.IsActive && !category.IsSubtotal)
            .OrderBy(category => category.CashFlowSection)
            .ThenBy(category => category.SortOrder)
            .Select(category => new { category.Id, Name = $"{category.CashFlowSection} - {category.Name}" })
            .ToListAsync();
        CategoryOptions = new SelectList(categories, "Id", "Name", CategoryId);
    }
}
