using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Reports;

namespace Vantage.PMS.Pages.Reports.ExportLogs;

[Authorize(Policy = PmsPolicies.ReportAdministration)]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public IList<ReportExportLog> Logs { get; private set; } = [];

    public DateTime? DateRangeStart { get; private set; }

    public DateTime? DateRangeEnd { get; private set; }

    public ReportCategory? Category { get; private set; }

    public ReportExportType? ExportType { get; private set; }

    public string? UserName { get; private set; }

    public async Task OnGetAsync(DateTime? dateRangeStart, DateTime? dateRangeEnd, ReportCategory? category, ReportExportType? exportType, string? userName)
    {
        DateRangeStart = dateRangeStart?.Date;
        DateRangeEnd = dateRangeEnd?.Date;
        Category = category;
        ExportType = exportType;
        UserName = userName;

        var query = context.ReportExportLogs.AsNoTracking();
        if (DateRangeStart is not null)
        {
            query = query.Where(log => log.ExportedAt >= DateRangeStart.Value);
        }
        if (DateRangeEnd is not null)
        {
            var endExclusive = DateRangeEnd.Value.AddDays(1);
            query = query.Where(log => log.ExportedAt < endExclusive);
        }
        if (Category is not null)
        {
            query = query.Where(log => log.ReportCategory == Category);
        }
        if (ExportType is not null)
        {
            query = query.Where(log => log.ExportType == ExportType);
        }
        if (!string.IsNullOrWhiteSpace(UserName))
        {
            query = query.Where(log => log.ExportedBy != null && log.ExportedBy.Contains(UserName));
        }

        Logs = await query
            .OrderByDescending(log => log.ExportedAt)
            .Take(300)
            .ToListAsync();
    }
}
