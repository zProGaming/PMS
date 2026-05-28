using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Authorization;
using Vantage.PMS.Models.Reports;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Reports;

public class ExportModel(ReportCatalogService catalogService, ReportExportService exportService) : PageModel
{
    public IActionResult OnGet()
    {
        return RedirectToPage("/Reports/Center");
    }

    public async Task<IActionResult> OnGetCsvAsync(string reportKey, DateTime? dateRangeStart, DateTime? dateRangeEnd)
    {
        var catalogEntry = catalogService.Find(reportKey);
        if (catalogEntry is null)
        {
            return RedirectToPage("/Reports/Placeholder", new { reportName = reportKey, category = "Reports" });
        }

        if (!User.IsInRole(PmsRoles.SystemAdmin) && catalogEntry.RequiredRoles.Length > 0 && !catalogEntry.RequiredRoles.Any(User.IsInRole))
        {
            return Forbid();
        }

        if (!catalogEntry.SupportsCsvExport)
        {
            return RedirectToPage("/Reports/Placeholder", new { reportName = catalogEntry.ReportName, category = ReportCatalogService.FormatCategory(catalogEntry.ReportCategory), message = "CSV export is not currently enabled for this report." });
        }

        var csv = await exportService.BuildCsvForReportAsync(catalogEntry, dateRangeStart, dateRangeEnd);
        if (csv is null)
        {
            return RedirectToPage("/Reports/Placeholder", new { reportName = catalogEntry.ReportName, category = ReportCatalogService.FormatCategory(catalogEntry.ReportCategory), message = "CSV export is not currently enabled for this report." });
        }

        await exportService.LogExportAsync(catalogEntry, ReportExportType.Csv, User.Identity?.Name ?? "System", dateRangeStart, dateRangeEnd, csv.FileName);
        return File(csv.Content, csv.ContentType, csv.FileName);
    }
}
