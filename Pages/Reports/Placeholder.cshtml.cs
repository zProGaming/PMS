using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Vantage.PMS.Pages.Reports;

public class PlaceholderModel : PageModel
{
    public string ReportName { get; private set; } = "Report";

    public string Category { get; private set; } = "Reports";

    public string Message { get; private set; } = "This report is planned or not yet configured.";

    public void OnGet(string? reportName, string? category, string? message)
    {
        ReportName = string.IsNullOrWhiteSpace(reportName) ? "Report" : reportName;
        Category = string.IsNullOrWhiteSpace(category) ? "Reports" : category;
        Message = string.IsNullOrWhiteSpace(message) ? Message : message;
    }
}
