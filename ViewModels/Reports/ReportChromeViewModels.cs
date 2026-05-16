namespace Vantage.PMS.ViewModels.Reports;

public class ReportHeaderViewModel
{
    public string HotelName { get; set; } = "Vantage Grand Hotel";

    public string ReportTitle { get; set; } = "Report";

    public string ReportCategory { get; set; } = "Reports";

    public DateTime? DateRangeStart { get; set; }

    public DateTime? DateRangeEnd { get; set; }

    public string? GeneratedBy { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public string? PreparedBy { get; set; }

    public string? ReviewedBy { get; set; }

    public string? Disclaimer { get; set; }
}

public class ReportFooterViewModel
{
    public string? FooterText { get; set; }

    public bool ShowComplianceNote { get; set; } = true;

    public DateTime PrintedAt { get; set; } = DateTime.Now;
}

public class ReportActionsViewModel
{
    public string? CsvUrl { get; set; }

    public string? HtmlUrl { get; set; }

    public string? BackUrl { get; set; }

    public bool CsvAvailable { get; set; }

    public bool HtmlAvailable { get; set; }

    public string PdfHelpText { get; set; } = "Use browser print to save as PDF.";
}
