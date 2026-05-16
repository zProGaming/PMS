using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Models.Labor;

public enum EmploymentType
{
    Regular = 0,
    Probationary = 1,
    Contractual = 2,
    Seasonal = 3,
    Casual = 4,
    Agency = 5,
    Other = 6
}

public enum PayrollPeriodStatus
{
    Draft = 0,
    ForApproval = 1,
    Approved = 2,
    Posted = 3,
    Closed = 4,
    Cancelled = 5
}

public enum ServiceChargeDistributionMethod
{
    EqualShare = 0,
    ByHoursWorked = 1,
    ByEligibleDays = 2,
    ByDepartmentWeight = 3,
    Manual = 4
}

public enum ServiceChargePoolStatus
{
    Draft = 0,
    ForApproval = 1,
    Approved = 2,
    Posted = 3,
    Cancelled = 4
}

public class EmployeeCostProfile
{
    public int Id { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public string? Position { get; set; }

    public EmploymentType EmploymentType { get; set; } = EmploymentType.Regular;

    public int? DefaultLaborGLAccountId { get; set; }

    public GLAccount? DefaultLaborGLAccount { get; set; }

    public int? DefaultPayrollLiabilityGLAccountId { get; set; }

    public GLAccount? DefaultPayrollLiabilityGLAccount { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;
}

public class PayrollPeriod
{
    public int Id { get; set; }

    public string PeriodName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today;

    public DateTime? PayDate { get; set; }

    public PayrollPeriodStatus Status { get; set; } = PayrollPeriodStatus.Draft;

    public string? PreparedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? PostedBy { get; set; }

    public DateTime? PostedAt { get; set; }

    public int? JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }

    public string? Notes { get; set; }

    public ICollection<PayrollCostEntry> Entries { get; set; } = new List<PayrollCostEntry>();
}

public class PayrollCostEntry
{
    public int Id { get; set; }

    public int PayrollPeriodId { get; set; }

    public PayrollPeriod? PayrollPeriod { get; set; }

    public int? EmployeeCostProfileId { get; set; }

    public EmployeeCostProfile? EmployeeCostProfile { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public string? Position { get; set; }

    public decimal RegularHours { get; set; }

    public decimal OvertimeHours { get; set; }

    public decimal NightDifferentialHours { get; set; }

    public decimal RegularPay { get; set; }

    public decimal OvertimePay { get; set; }

    public decimal NightDifferentialPay { get; set; }

    public decimal Allowances { get; set; }

    public decimal ServiceChargeShare { get; set; }

    public decimal OtherEarnings { get; set; }

    public decimal GrossPay { get; set; }

    public decimal EmployerCost { get; set; }

    public decimal Deductions { get; set; }

    public decimal NetPay { get; set; }

    public int? LaborGLAccountId { get; set; }

    public GLAccount? LaborGLAccount { get; set; }

    public int? PayrollLiabilityGLAccountId { get; set; }

    public GLAccount? PayrollLiabilityGLAccount { get; set; }

    public string? Notes { get; set; }
}

public class PayrollAllocationRule
{
    public int Id { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public int? EmployeeCostProfileId { get; set; }

    public EmployeeCostProfile? EmployeeCostProfile { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public decimal AllocationPercentage { get; set; } = 100;

    public int? LaborGLAccountId { get; set; }

    public GLAccount? LaborGLAccount { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}

public class DepartmentLaborBudget
{
    public int Id { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public int Month { get; set; } = DateTime.Today.Month;

    public int Year { get; set; } = DateTime.Today.Year;

    public decimal BudgetedLaborCost { get; set; }

    public decimal BudgetedLaborHours { get; set; }

    public int BudgetedHeadcount { get; set; }

    public string? Notes { get; set; }
}

public class ServiceChargePool
{
    public int Id { get; set; }

    public string PoolName { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; } = DateTime.Today;

    public DateTime PeriodEnd { get; set; } = DateTime.Today;

    public decimal TotalServiceChargeCollected { get; set; }

    public ServiceChargeDistributionMethod DistributionMethod { get; set; } = ServiceChargeDistributionMethod.EqualShare;

    public ServiceChargePoolStatus Status { get; set; } = ServiceChargePoolStatus.Draft;

    public string? PreparedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? PostedBy { get; set; }

    public DateTime? PostedAt { get; set; }

    public int? JournalEntryId { get; set; }

    public JournalEntry? JournalEntry { get; set; }

    public string? Notes { get; set; }

    public ICollection<ServiceChargeDistributionLine> DistributionLines { get; set; } = new List<ServiceChargeDistributionLine>();
}

public class ServiceChargeDistributionLine
{
    public int Id { get; set; }

    public int ServiceChargePoolId { get; set; }

    public ServiceChargePool? ServiceChargePool { get; set; }

    public int? EmployeeCostProfileId { get; set; }

    public EmployeeCostProfile? EmployeeCostProfile { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public decimal EligibleDays { get; set; }

    public decimal EligibleHours { get; set; }

    public decimal DistributionPercentage { get; set; }

    public decimal Amount { get; set; }

    public string? Notes { get; set; }
}

public class LaborProductivitySnapshot
{
    public int Id { get; set; }

    public DateTime SnapshotDate { get; set; } = DateTime.Today;

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int? USALIDepartmentId { get; set; }

    public USALIDepartment? USALIDepartment { get; set; }

    public decimal LaborHours { get; set; }

    public decimal LaborCost { get; set; }

    public decimal DepartmentRevenue { get; set; }

    public decimal LaborCostPercentage { get; set; }

    public decimal RevenuePerLaborHour { get; set; }

    public int? RoomsCleaned { get; set; }

    public int? CoversServed { get; set; }

    public int? EventsServed { get; set; }

    public string? Notes { get; set; }
}
