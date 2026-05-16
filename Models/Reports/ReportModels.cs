namespace Vantage.PMS.Models.Reports;

public enum ReportCategory
{
    FrontOffice = 0,
    Housekeeping = 1,
    Finance = 2,
    Accounting = 3,
    USALI = 4,
    PhilippineFinance = 5,
    AccountsReceivable = 6,
    AccountsPayable = 7,
    Banking = 8,
    PayrollLabor = 9,
    FoodBeverage = 10,
    Kitchen = 11,
    Banquet = 12,
    Sales = 13,
    Revenue = 14,
    BookingEngine = 15,
    GuestPortal = 16,
    Inventory = 17,
    Purchasing = 18,
    ManagementAI = 19,
    Executive = 20,
    AuditSystem = 21,
    Demo = 22,
    Other = 23
}

public enum ReportExportType
{
    Print = 0,
    BrowserPdf = 1,
    Csv = 2,
    Html = 3,
    ExcelPlaceholder = 4,
    PdfPlaceholder = 5
}

public class ReportTemplateSetting
{
    public int Id { get; set; }

    public string ReportKey { get; set; } = string.Empty;

    public string ReportName { get; set; } = string.Empty;

    public ReportCategory ReportCategory { get; set; } = ReportCategory.Other;

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

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class ReportExportLog
{
    public int Id { get; set; }

    public string ReportKey { get; set; } = string.Empty;

    public string ReportName { get; set; } = string.Empty;

    public ReportCategory ReportCategory { get; set; } = ReportCategory.Other;

    public ReportExportType ExportType { get; set; } = ReportExportType.Csv;

    public string? ExportedBy { get; set; }

    public DateTime ExportedAt { get; set; } = DateTime.Now;

    public DateTime? DateRangeStart { get; set; }

    public DateTime? DateRangeEnd { get; set; }

    public string? FileName { get; set; }

    public string? Notes { get; set; }
}

public class SavedReportRun
{
    public int Id { get; set; }

    public string ReportKey { get; set; } = string.Empty;

    public string ReportName { get; set; } = string.Empty;

    public ReportCategory ReportCategory { get; set; } = ReportCategory.Other;

    public DateTime? DateRangeStart { get; set; }

    public DateTime? DateRangeEnd { get; set; }

    public string? ParametersJson { get; set; }

    public string? RunBy { get; set; }

    public DateTime RunAt { get; set; } = DateTime.Now;

    public string? Notes { get; set; }
}

public class ReportCatalogItem
{
    public int Id { get; set; }

    public string ReportKey { get; set; } = string.Empty;

    public string ReportName { get; set; } = string.Empty;

    public ReportCategory ReportCategory { get; set; } = ReportCategory.Other;

    public string Description { get; set; } = string.Empty;

    public string? RoutePath { get; set; }

    public string RequiredRoles { get; set; } = string.Empty;

    public bool SupportsPrint { get; set; } = true;

    public bool SupportsCsvExport { get; set; }

    public bool SupportsHtmlExport { get; set; } = true;

    public bool SupportsPdfViaBrowserPrint { get; set; } = true;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
