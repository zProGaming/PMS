using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Models.Executive;

public enum ExecutiveReportType
{
    DailyFlashReport = 0,
    WeeklyExecutiveSummary = 1,
    MonthlyOwnerReport = 2,
    QuarterlyPerformanceReview = 3,
    AnnualPerformanceSummary = 4,
    Custom = 5
}

public enum ExecutiveKPICategory
{
    Occupancy = 0,
    Revenue = 1,
    Profitability = 2,
    CashFlow = 3,
    GuestExperience = 4,
    Housekeeping = 5,
    Maintenance = 6,
    FoodBeverage = 7,
    Banquet = 8,
    Sales = 9,
    Inventory = 10,
    Labor = 11,
    FinanceControl = 12,
    AccountsReceivable = 13,
    AccountsPayable = 14,
    RevenueManagement = 15,
    Risk = 16,
    Other = 17
}

public enum KPIStatus
{
    Excellent = 0,
    Good = 1,
    Watch = 2,
    Warning = 3,
    Critical = 4,
    NotAvailable = 5
}

public enum ExecutiveAlertType
{
    Performance = 0,
    FinancialRisk = 1,
    OperationalRisk = 2,
    GuestExperience = 3,
    RevenueOpportunity = 4,
    CostControl = 5,
    Compliance = 6,
    SystemHealth = 7,
    Other = 8
}

public enum OwnerReportPackageStatus
{
    Draft = 0,
    Ready = 1,
    Reviewed = 2,
    Sent = 3,
    Archived = 4
}

public class ExecutiveReportSnapshot
{
    public int Id { get; set; }

    public DateTime ReportDate { get; set; } = DateTime.Today;

    public DateTime PeriodStart { get; set; } = DateTime.Today;

    public DateTime PeriodEnd { get; set; } = DateTime.Today;

    public ExecutiveReportType ReportType { get; set; } = ExecutiveReportType.DailyFlashReport;

    public string HotelName { get; set; } = string.Empty;

    public string PreparedBy { get; set; } = string.Empty;

    public DateTime PreparedAt { get; set; } = DateTime.Now;

    public decimal OccupancyPercentage { get; set; }

    public decimal ADR { get; set; }

    public decimal RevPAR { get; set; }

    public decimal TotalRoomRevenue { get; set; }

    public decimal TotalFBRevenue { get; set; }

    public decimal TotalBanquetRevenue { get; set; }

    public decimal TotalOtherRevenue { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal TotalPayments { get; set; }

    public decimal GrossOperatingProfit { get; set; }

    public decimal NetIncome { get; set; }

    public decimal ARBalance { get; set; }

    public decimal APBalance { get; set; }

    public decimal LaborCost { get; set; }

    public decimal LaborCostPercentage { get; set; }

    public decimal? GuestSatisfactionScore { get; set; }

    public int OpenCriticalIssues { get; set; }

    public string? SummaryText { get; set; }

    public string? Notes { get; set; }
}

public class ExecutiveKPI
{
    public int Id { get; set; }

    public string KPIName { get; set; } = string.Empty;

    public string KPICode { get; set; } = string.Empty;

    public ExecutiveKPICategory Category { get; set; } = ExecutiveKPICategory.Other;

    public string? Description { get; set; }

    public string? FormulaDescription { get; set; }

    public decimal? TargetValue { get; set; }

    public decimal? WarningThreshold { get; set; }

    public decimal? CriticalThreshold { get; set; }

    public bool IsHigherBetter { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public ICollection<ExecutiveKPIResult> Results { get; set; } = new List<ExecutiveKPIResult>();
}

public class ExecutiveKPIResult
{
    public int Id { get; set; }

    public int ExecutiveKPIId { get; set; }

    public ExecutiveKPI? ExecutiveKPI { get; set; }

    public DateTime ResultDate { get; set; } = DateTime.Today;

    public DateTime PeriodStart { get; set; } = DateTime.Today;

    public DateTime PeriodEnd { get; set; } = DateTime.Today;

    public decimal ActualValue { get; set; }

    public decimal? TargetValue { get; set; }

    public decimal? Variance { get; set; }

    public decimal? VariancePercentage { get; set; }

    public KPIStatus Status { get; set; } = KPIStatus.NotAvailable;

    public string? Notes { get; set; }
}

public class DepartmentPerformanceSnapshot
{
    public int Id { get; set; }

    public DateTime SnapshotDate { get; set; } = DateTime.Today;

    public DateTime PeriodStart { get; set; } = DateTime.Today;

    public DateTime PeriodEnd { get; set; } = DateTime.Today;

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public decimal Revenue { get; set; }

    public decimal CostOfSales { get; set; }

    public decimal PayrollCost { get; set; }

    public decimal OtherExpenses { get; set; }

    public decimal DepartmentProfit { get; set; }

    public decimal DepartmentProfitMargin { get; set; }

    public decimal LaborCostPercentage { get; set; }

    public decimal? BudgetAmount { get; set; }

    public decimal? VarianceAmount { get; set; }

    public decimal? VariancePercentage { get; set; }

    public string? Notes { get; set; }
}

public class ExecutiveAlert
{
    public int Id { get; set; }

    public DateTime AlertDate { get; set; } = DateTime.Today;

    public ExecutiveAlertType AlertType { get; set; } = ExecutiveAlertType.Other;

    public KPIStatus Severity { get; set; } = KPIStatus.Watch;

    public string Module { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? RecommendedAction { get; set; }

    public string? RelatedReferenceType { get; set; }

    public int? RelatedReferenceId { get; set; }

    public bool IsResolved { get; set; }

    public string? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class OwnerReportPackage
{
    public int Id { get; set; }

    public string PackageName { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; } = DateTime.Today;

    public DateTime PeriodEnd { get; set; } = DateTime.Today;

    public string PreparedFor { get; set; } = string.Empty;

    public string PreparedBy { get; set; } = string.Empty;

    public DateTime PreparedAt { get; set; } = DateTime.Now;

    public OwnerReportPackageStatus Status { get; set; } = OwnerReportPackageStatus.Draft;

    public string? Notes { get; set; }

    public ICollection<OwnerReportPackageItem> Items { get; set; } = new List<OwnerReportPackageItem>();
}

public class OwnerReportPackageItem
{
    public int Id { get; set; }

    public int OwnerReportPackageId { get; set; }

    public OwnerReportPackage? OwnerReportPackage { get; set; }

    public string ReportName { get; set; } = string.Empty;

    public string ReportType { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsIncluded { get; set; } = true;

    public string? Notes { get; set; }
}

public class KPIBenchmarkSetting
{
    public int Id { get; set; }

    public string BenchmarkName { get; set; } = string.Empty;

    public string KPIName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public decimal TargetValue { get; set; }

    public decimal? WarningThreshold { get; set; }

    public decimal? CriticalThreshold { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    public DateTime? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}
