namespace Vantage.PMS.Models.Accounting;

public enum CashFlowSection
{
    Operating = 0,
    Investing = 1,
    Financing = 2,
    BeginningCash = 3,
    EndingCash = 4,
    Reconciliation = 5
}

public enum CashFlowMethod
{
    Direct = 0,
    Indirect = 1
}

public class CashFlowCategory
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public CashFlowSection CashFlowSection { get; set; } = CashFlowSection.Operating;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsSubtotal { get; set; }

    public bool IsActive { get; set; } = true;
}

public class CashFlowMappingRule
{
    public int Id { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public int? GLAccountId { get; set; }

    public GLAccount? GLAccount { get; set; }

    public SourceModule? SourceModule { get; set; }

    public SourceTransactionType? SourceTransactionType { get; set; }

    public int CashFlowCategoryId { get; set; }

    public CashFlowCategory? CashFlowCategory { get; set; }

    public CashFlowSection CashFlowSection { get; set; } = CashFlowSection.Operating;

    public bool IsCashAccountRule { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;
}

public class CashFlowReportSnapshot
{
    public int Id { get; set; }

    public string ReportName { get; set; } = "Statement of Cash Flows";

    public DateTime PeriodStart { get; set; } = DateTime.Today;

    public DateTime PeriodEnd { get; set; } = DateTime.Today;

    public decimal BeginningCashBalance { get; set; }

    public decimal NetCashFromOperatingActivities { get; set; }

    public decimal NetCashFromInvestingActivities { get; set; }

    public decimal NetCashFromFinancingActivities { get; set; }

    public decimal NetIncreaseDecreaseInCash { get; set; }

    public decimal EndingCashBalance { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    public string? GeneratedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<CashFlowReportSnapshotLine> Lines { get; set; } = new List<CashFlowReportSnapshotLine>();
}

public class CashFlowReportSnapshotLine
{
    public int Id { get; set; }

    public int CashFlowReportSnapshotId { get; set; }

    public CashFlowReportSnapshot? CashFlowReportSnapshot { get; set; }

    public CashFlowSection CashFlowSection { get; set; } = CashFlowSection.Operating;

    public int? CashFlowCategoryId { get; set; }

    public CashFlowCategory? CashFlowCategory { get; set; }

    public string LineCode { get; set; } = string.Empty;

    public string LineName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int SortOrder { get; set; }

    public bool IsSubtotal { get; set; }
}

public class CashAccountSetting
{
    public int Id { get; set; }

    public int GLAccountId { get; set; }

    public GLAccount? GLAccount { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public bool IsCashOnHand { get; set; }

    public bool IsCashInBank { get; set; }

    public bool IsEWallet { get; set; }

    public bool IsCashEquivalent { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}
