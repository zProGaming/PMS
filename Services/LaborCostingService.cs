using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.Labor;

namespace Vantage.PMS.Services;

public class LaborCostingService(ApplicationDbContext context, AccountingPostingService accountingPostingService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly AccountingPostingService _accountingPostingService = accountingPostingService;

    public void Recalculate(PayrollCostEntry entry)
    {
        entry.GrossPay = entry.RegularPay +
            entry.OvertimePay +
            entry.NightDifferentialPay +
            entry.Allowances +
            entry.ServiceChargeShare +
            entry.OtherEarnings;
        entry.EmployerCost = entry.EmployerCost <= 0 ? entry.GrossPay : entry.EmployerCost;
        entry.NetPay = entry.GrossPay - entry.Deductions;
    }

    public async Task<IList<string>> ApprovePayrollPeriodAsync(int payrollPeriodId, string approvedBy)
    {
        var errors = new List<string>();
        var period = await _context.PayrollPeriods
            .Include(item => item.Entries)
            .FirstOrDefaultAsync(item => item.Id == payrollPeriodId);

        if (period is null)
        {
            errors.Add("Payroll period was not found.");
            return errors;
        }

        if (period.Status is not (PayrollPeriodStatus.Draft or PayrollPeriodStatus.ForApproval))
        {
            errors.Add("Only draft or for-approval payroll periods can be approved.");
        }

        if (period.StartDate.Date > period.EndDate.Date)
        {
            errors.Add("Payroll period start date must be before or equal to end date.");
        }

        if (!period.Entries.Any())
        {
            errors.Add("Payroll period must have at least one cost entry before approval.");
        }

        if (period.Entries.Any(HasNegativePayrollAmount))
        {
            errors.Add("Payroll entries cannot contain negative amounts for MVP payroll costing.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        foreach (var entry in period.Entries)
        {
            Recalculate(entry);
        }

        period.Status = PayrollPeriodStatus.Approved;
        period.ApprovedBy = approvedBy;
        period.ApprovedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return errors;
    }

    public async Task<IList<string>> PostPayrollPeriodAsync(int payrollPeriodId, string postedBy)
    {
        var errors = new List<string>();
        var period = await _context.PayrollPeriods
            .Include(item => item.Entries)
            .ThenInclude(entry => entry.EmployeeCostProfile)
            .FirstOrDefaultAsync(item => item.Id == payrollPeriodId);

        if (period is null)
        {
            errors.Add("Payroll period was not found.");
            return errors;
        }

        if (period.Status != PayrollPeriodStatus.Approved)
        {
            errors.Add("Only approved payroll periods can be posted.");
        }

        if (period.JournalEntryId is not null ||
            await HasPostedSourceAsync(SourceTransactionType.PayrollCost, period.Id))
        {
            errors.Add("This payroll period has already been posted to the general ledger.");
        }

        if (!period.Entries.Any())
        {
            errors.Add("Payroll period has no cost entries.");
        }

        if (period.Entries.Any(HasNegativePayrollAmount))
        {
            errors.Add("Payroll entries cannot contain negative amounts for MVP payroll costing.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        foreach (var entry in period.Entries)
        {
            Recalculate(entry);
        }

        var payrollLiabilityAccountId = await GetRequiredAccountIdAsync("2070", "2000");
        var journal = new JournalEntry
        {
            JournalNumber = await GenerateJournalNumberAsync("PAY"),
            JournalDate = (period.PayDate ?? period.EndDate).Date,
            SourceModule = SourceModule.Finance,
            SourceTransactionType = SourceTransactionType.PayrollCost,
            SourceReferenceId = period.Id,
            SourceReferenceNumber = period.PeriodName,
            Description = $"Payroll cost posting - {period.PeriodName}",
            Status = JournalEntryStatus.Draft,
            CreatedBy = postedBy
        };

        foreach (var entry in period.Entries.Where(entry => (entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay) > 0))
        {
            var amount = entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay;
            var laborAccountId = entry.LaborGLAccountId ??
                entry.EmployeeCostProfile?.DefaultLaborGLAccountId ??
                await ResolveLaborAccountIdAsync(entry.USALIDepartmentId ?? entry.EmployeeCostProfile?.USALIDepartmentId);
            var liabilityAccountId = entry.PayrollLiabilityGLAccountId ??
                entry.EmployeeCostProfile?.DefaultPayrollLiabilityGLAccountId ??
                payrollLiabilityAccountId;
            var description = BuildPayrollLineDescription(entry);

            journal.Lines.Add(new JournalEntryLine
            {
                GLAccountId = laborAccountId,
                USALIDepartmentId = entry.USALIDepartmentId ?? entry.EmployeeCostProfile?.USALIDepartmentId,
                DebitAmount = amount,
                CreditAmount = 0,
                Description = description,
                LineReferenceType = nameof(PayrollCostEntry),
                LineReferenceId = entry.Id
            });
            journal.Lines.Add(new JournalEntryLine
            {
                GLAccountId = liabilityAccountId,
                USALIDepartmentId = entry.USALIDepartmentId ?? entry.EmployeeCostProfile?.USALIDepartmentId,
                DebitAmount = 0,
                CreditAmount = amount,
                Description = description,
                LineReferenceType = nameof(PayrollCostEntry),
                LineReferenceId = entry.Id
            });
        }

        if (!journal.Lines.Any())
        {
            errors.Add("Payroll period has no positive employer cost to post.");
            return errors;
        }

        _context.JournalEntries.Add(journal);
        await _context.SaveChangesAsync();

        var postErrors = await _accountingPostingService.PostJournalEntryAsync(journal.Id, postedBy);
        if (postErrors.Count > 0)
        {
            return postErrors;
        }

        period.Status = PayrollPeriodStatus.Posted;
        period.PostedBy = postedBy;
        period.PostedAt = DateTime.Now;
        period.JournalEntryId = journal.Id;
        await _context.SaveChangesAsync();
        return errors;
    }

    public async Task<IList<string>> GenerateServiceChargeDistributionAsync(int poolId)
    {
        var errors = new List<string>();
        var pool = await _context.ServiceChargePools
            .Include(item => item.DistributionLines)
            .FirstOrDefaultAsync(item => item.Id == poolId);

        if (pool is null)
        {
            errors.Add("Service charge pool was not found.");
            return errors;
        }

        if (pool.Status is ServiceChargePoolStatus.Posted or ServiceChargePoolStatus.Cancelled)
        {
            errors.Add("Posted or cancelled service charge pools cannot regenerate distribution lines.");
            return errors;
        }

        if (pool.TotalServiceChargeCollected <= 0)
        {
            pool.TotalServiceChargeCollected = await EstimateServiceChargeCollectedAsync(pool.PeriodStart, pool.PeriodEnd);
        }

        if (pool.TotalServiceChargeCollected <= 0)
        {
            errors.Add("Service charge pool amount must be greater than zero.");
            return errors;
        }

        if (pool.DistributionMethod == ServiceChargeDistributionMethod.Manual)
        {
            errors.Add("Manual service charge pools use user-entered distribution lines.");
            return errors;
        }

        _context.ServiceChargeDistributionLines.RemoveRange(pool.DistributionLines);

        var employees = await _context.EmployeeCostProfiles
            .AsNoTracking()
            .Where(employee => employee.IsActive)
            .OrderBy(employee => employee.FullName)
            .ToListAsync();
        employees = employees
            .Where(ServiceChargeEligibility.IsEligible)
            .ToList();

        if (employees.Count == 0)
        {
            errors.Add("No eligible active non-managerial employee cost profiles were found. Review employee positions, agency profiles, or add manual distribution lines for validated eligible workers.");
            return errors;
        }

        var hoursByEmployee = pool.DistributionMethod == ServiceChargeDistributionMethod.ByHoursWorked
            ? await GetPayrollHoursByEmployeeAsync(pool.PeriodStart, pool.PeriodEnd)
            : new Dictionary<int, decimal>();
        var totalHours = hoursByEmployee.Values.Sum();
        var totalEligibleDays = 0m;

        var draftLines = new List<ServiceChargeDistributionLine>();
        foreach (var employee in employees)
        {
            var eligibleHours = hoursByEmployee.GetValueOrDefault(employee.Id);
            var eligibleDays = 1m;
            totalEligibleDays += eligibleDays;
            draftLines.Add(new ServiceChargeDistributionLine
            {
                ServiceChargePoolId = pool.Id,
                EmployeeCostProfileId = employee.Id,
                DepartmentId = employee.DepartmentId,
                EligibleDays = eligibleDays,
                EligibleHours = eligibleHours,
                Notes = ServiceChargeEligibility.BuildGeneratedLineNote(employee)
            });
        }

        foreach (var line in draftLines)
        {
            line.DistributionPercentage = pool.DistributionMethod switch
            {
                ServiceChargeDistributionMethod.ByHoursWorked when totalHours > 0 => line.EligibleHours / totalHours * 100,
                ServiceChargeDistributionMethod.ByEligibleDays when totalEligibleDays > 0 => line.EligibleDays / totalEligibleDays * 100,
                _ => 100m / draftLines.Count
            };
            line.Amount = Math.Round(pool.TotalServiceChargeCollected * line.DistributionPercentage / 100, 2);
        }

        var difference = pool.TotalServiceChargeCollected - draftLines.Sum(line => line.Amount);
        draftLines[^1].Amount += difference;
        _context.ServiceChargeDistributionLines.AddRange(draftLines);
        await _context.SaveChangesAsync();
        return errors;
    }

    public async Task<IList<string>> ApproveServiceChargePoolAsync(int poolId, string approvedBy)
    {
        var errors = new List<string>();
        var pool = await _context.ServiceChargePools
            .Include(item => item.DistributionLines)
            .ThenInclude(line => line.EmployeeCostProfile)
            .FirstOrDefaultAsync(item => item.Id == poolId);

        if (pool is null)
        {
            errors.Add("Service charge pool was not found.");
            return errors;
        }

        if (pool.Status is not (ServiceChargePoolStatus.Draft or ServiceChargePoolStatus.ForApproval))
        {
            errors.Add("Only draft or for-approval service charge pools can be approved.");
        }

        if (pool.TotalServiceChargeCollected <= 0)
        {
            errors.Add("Service charge pool amount must be greater than zero.");
        }

        ValidateServiceChargeDistribution(pool, errors, "approval");

        if (errors.Count > 0)
        {
            return errors;
        }

        pool.Status = ServiceChargePoolStatus.Approved;
        pool.ApprovedBy = approvedBy;
        pool.ApprovedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return errors;
    }

    public async Task<IList<string>> PostServiceChargePoolAsync(int poolId, string postedBy)
    {
        var errors = new List<string>();
        var pool = await _context.ServiceChargePools
            .Include(item => item.DistributionLines)
            .ThenInclude(line => line.EmployeeCostProfile)
            .FirstOrDefaultAsync(item => item.Id == poolId);

        if (pool is null)
        {
            errors.Add("Service charge pool was not found.");
            return errors;
        }

        if (pool.Status != ServiceChargePoolStatus.Approved)
        {
            errors.Add("Only approved service charge pools can be posted.");
        }

        if (pool.JournalEntryId is not null ||
            await HasPostedSourceAsync(SourceTransactionType.ServiceChargeDistribution, pool.Id))
        {
            errors.Add("This service charge pool has already been posted.");
        }

        if (pool.TotalServiceChargeCollected <= 0)
        {
            errors.Add("Service charge pool amount must be greater than zero.");
        }

        ValidateServiceChargeDistribution(pool, errors, "posting");

        if (errors.Count > 0)
        {
            return errors;
        }

        var serviceChargePayableAccountId = await GetRequiredAccountIdAsync("2080", "2040");
        var payrollPayableAccountId = await GetRequiredAccountIdAsync("2070", "2000");
        var journal = new JournalEntry
        {
            JournalNumber = await GenerateJournalNumberAsync("SCD"),
            JournalDate = pool.PeriodEnd.Date,
            SourceModule = SourceModule.Finance,
            SourceTransactionType = SourceTransactionType.ServiceChargeDistribution,
            SourceReferenceId = pool.Id,
            SourceReferenceNumber = pool.PoolName,
            Description = $"Service charge distribution - {pool.PoolName}",
            Status = JournalEntryStatus.Draft,
            CreatedBy = postedBy,
            Lines =
            {
                new JournalEntryLine
                {
                    GLAccountId = serviceChargePayableAccountId,
                    DebitAmount = pool.TotalServiceChargeCollected,
                    CreditAmount = 0,
                    Description = $"Service charge distribution - {pool.PoolName}",
                    LineReferenceType = nameof(ServiceChargePool),
                    LineReferenceId = pool.Id
                },
                new JournalEntryLine
                {
                    GLAccountId = payrollPayableAccountId,
                    DebitAmount = 0,
                    CreditAmount = pool.TotalServiceChargeCollected,
                    Description = $"Service charge distribution payable - {pool.PoolName}",
                    LineReferenceType = nameof(ServiceChargePool),
                    LineReferenceId = pool.Id
                }
            }
        };

        _context.JournalEntries.Add(journal);
        await _context.SaveChangesAsync();

        var postErrors = await _accountingPostingService.PostJournalEntryAsync(journal.Id, postedBy);
        if (postErrors.Count > 0)
        {
            return postErrors;
        }

        pool.Status = ServiceChargePoolStatus.Posted;
        pool.PostedBy = postedBy;
        pool.PostedAt = DateTime.Now;
        pool.JournalEntryId = journal.Id;
        await _context.SaveChangesAsync();
        return errors;
    }

    public async Task<LaborDashboardMetrics> GetDashboardMetricsAsync(DateTime asOfDate)
    {
        var monthStart = new DateTime(asOfDate.Year, asOfDate.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var entries = _context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < nextMonth &&
                entry.PayrollPeriod.EndDate >= monthStart &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled);

        var totalLaborCost = await entries.SumAsync(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay);
        var totalLaborHours = await entries.SumAsync(entry => entry.RegularHours + entry.OvertimeHours + entry.NightDifferentialHours);
        var totalRevenue = await _context.JournalEntryLines
            .AsNoTracking()
            .Where(line => line.JournalEntry != null &&
                line.JournalEntry.Status == JournalEntryStatus.Posted &&
                line.JournalEntry.JournalDate >= monthStart &&
                line.JournalEntry.JournalDate < nextMonth &&
                line.GLAccount != null &&
                line.GLAccount.AccountType == GLAccountType.Revenue)
            .SumAsync(line => line.CreditAmount - line.DebitAmount);
        var actualByDepartment = await entries
            .GroupBy(entry => entry.DepartmentId)
            .Select(group => new { DepartmentId = group.Key, Cost = group.Sum(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay) })
            .ToListAsync();
        var budgets = await _context.DepartmentLaborBudgets
            .AsNoTracking()
            .Where(budget => budget.Month == asOfDate.Month && budget.Year == asOfDate.Year)
            .ToListAsync();
        var departmentsOverBudget = budgets.Count(budget =>
            actualByDepartment.Where(actual => actual.DepartmentId == budget.DepartmentId).Sum(actual => actual.Cost) > budget.BudgetedLaborCost);

        return new LaborDashboardMetrics
        {
            PayrollPeriodsPendingApproval = await _context.PayrollPeriods.CountAsync(period => period.Status == PayrollPeriodStatus.ForApproval),
            PayrollPeriodsPendingPosting = await _context.PayrollPeriods.CountAsync(period => period.Status == PayrollPeriodStatus.Approved),
            TotalLaborCostThisMonth = totalLaborCost,
            TotalLaborHoursThisMonth = totalLaborHours,
            RevenuePerLaborHour = totalLaborHours <= 0 ? 0 : totalRevenue / totalLaborHours,
            DepartmentsOverBudget = departmentsOverBudget,
            ServiceChargePoolsPendingApproval = await _context.ServiceChargePools.CountAsync(pool => pool.Status == ServiceChargePoolStatus.ForApproval),
            ServiceChargePoolsPendingPosting = await _context.ServiceChargePools.CountAsync(pool => pool.Status == ServiceChargePoolStatus.Approved),
            LaborCostPercentage = totalRevenue <= 0 ? 0 : totalLaborCost / totalRevenue * 100
        };
    }

    private async Task<decimal> EstimateServiceChargeCollectedAsync(DateTime periodStart, DateTime periodEnd)
    {
        var endExclusive = periodEnd.Date.AddDays(1);
        var posServiceCharge = await _context.POSOrders
            .AsNoTracking()
            .Where(order => order.OrderDate >= periodStart.Date && order.OrderDate < endExclusive && order.PaymentStatus != POSPaymentStatus.Voided)
            .SumAsync(order => (decimal?)order.ServiceCharge) ?? 0;
        var folioServiceCharge = await _context.FolioItems
            .AsNoTracking()
            .Where(item => item.PostingDate >= periodStart.Date &&
                item.PostingDate < endExclusive &&
                !item.IsVoided &&
                (item.ChargeCode == "SC" || (item.ChargeCodeDefinition != null && item.ChargeCodeDefinition.ChargeCategory == ChargeCategory.ServiceCharge)))
            .SumAsync(item => (decimal?)item.Amount) ?? 0;
        return posServiceCharge + folioServiceCharge;
    }

    private async Task<Dictionary<int, decimal>> GetPayrollHoursByEmployeeAsync(DateTime periodStart, DateTime periodEnd)
    {
        return await _context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.EmployeeCostProfileId != null &&
                entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate <= periodEnd.Date &&
                entry.PayrollPeriod.EndDate >= periodStart.Date)
            .GroupBy(entry => entry.EmployeeCostProfileId!.Value)
            .Select(group => new
            {
                EmployeeId = group.Key,
                Hours = group.Sum(entry => entry.RegularHours + entry.OvertimeHours + entry.NightDifferentialHours)
            })
            .ToDictionaryAsync(item => item.EmployeeId, item => item.Hours);
    }

    private async Task<int> ResolveLaborAccountIdAsync(int? usaliDepartmentId)
    {
        var departmentCode = usaliDepartmentId is null
            ? null
            : await _context.USALIDepartments
                .Where(department => department.Id == usaliDepartmentId.Value)
                .Select(department => department.Code)
                .FirstOrDefaultAsync();

        var preferredCode = departmentCode switch
        {
            "ROOMS" => "6000",
            "FB" => "6100",
            "BNQ" => "6150",
            "S&M" => "6300",
            "POM" => "6400",
            _ => "6200"
        };

        return await GetRequiredAccountIdAsync(preferredCode, "6200");
    }

    private async Task<int> GetRequiredAccountIdAsync(string preferredCode, string fallbackCode)
    {
        var accountId = await _context.GLAccounts
            .Where(account => account.IsActive && account.AccountCode == preferredCode)
            .Select(account => (int?)account.Id)
            .FirstOrDefaultAsync();
        accountId ??= await _context.GLAccounts
            .Where(account => account.IsActive && account.AccountCode == fallbackCode)
            .Select(account => (int?)account.Id)
            .FirstOrDefaultAsync();
        if (accountId is null)
        {
            throw new InvalidOperationException($"Required GL account {preferredCode} or fallback {fallbackCode} was not found.");
        }

        return accountId.Value;
    }

    private async Task<bool> HasPostedSourceAsync(SourceTransactionType transactionType, int sourceReferenceId)
    {
        return await _context.JournalEntries.AnyAsync(entry =>
            entry.SourceModule == SourceModule.Finance &&
            entry.SourceTransactionType == transactionType &&
            entry.SourceReferenceId == sourceReferenceId &&
            entry.Status == JournalEntryStatus.Posted);
    }

    private async Task<string> GenerateJournalNumberAsync(string prefix)
    {
        var todayPrefix = $"{prefix}-{DateTime.Today:yyyyMMdd}";
        var existingCount = await _context.JournalEntries.CountAsync(entry => entry.JournalNumber.StartsWith(todayPrefix));
        return $"{todayPrefix}-{existingCount + 1:0000}";
    }

    private static string BuildPayrollLineDescription(PayrollCostEntry entry)
    {
        var employee = entry.EmployeeCostProfile?.FullName;
        if (!string.IsNullOrWhiteSpace(employee))
        {
            return $"Payroll cost - {employee}";
        }

        return string.IsNullOrWhiteSpace(entry.Position) ? "Department payroll cost" : $"Payroll cost - {entry.Position}";
    }

    private static void ValidateServiceChargeDistribution(ServiceChargePool pool, IList<string> errors, string action)
    {
        if (!pool.DistributionLines.Any())
        {
            errors.Add($"Service charge pool must have distribution lines before {action}.");
            return;
        }

        if (pool.DistributionLines.Any(line => line.Amount < 0 || line.EligibleDays < 0 || line.EligibleHours < 0 || line.DistributionPercentage < 0))
        {
            errors.Add("Service charge distribution lines cannot contain negative amounts, days, hours, or percentages.");
        }

        var distributedAmount = pool.DistributionLines.Sum(line => line.Amount);
        if (Math.Abs(distributedAmount - pool.TotalServiceChargeCollected) > 0.01m)
        {
            errors.Add($"Service charge distribution total must match the pool total before {action}.");
        }

        var ineligibleNames = pool.DistributionLines
            .Where(line => line.EmployeeCostProfileId is not null && !ServiceChargeEligibility.IsEligible(line.EmployeeCostProfile))
            .Select(line => line.EmployeeCostProfile?.FullName ?? $"employee profile #{line.EmployeeCostProfileId}")
            .Distinct()
            .ToList();
        if (ineligibleNames.Count > 0)
        {
            errors.Add($"Service charge distribution includes inactive, agency, or managerial/executive employee profiles: {string.Join(", ", ineligibleNames)}. Review eligibility before {action}.");
        }
    }

    private static bool HasNegativePayrollAmount(PayrollCostEntry entry)
    {
        return entry.RegularHours < 0 ||
            entry.OvertimeHours < 0 ||
            entry.NightDifferentialHours < 0 ||
            entry.RegularPay < 0 ||
            entry.OvertimePay < 0 ||
            entry.NightDifferentialPay < 0 ||
            entry.Allowances < 0 ||
            entry.ServiceChargeShare < 0 ||
            entry.OtherEarnings < 0 ||
            entry.EmployerCost < 0 ||
            entry.Deductions < 0;
    }
}

public class LaborDashboardMetrics
{
    public int PayrollPeriodsPendingApproval { get; set; }

    public int PayrollPeriodsPendingPosting { get; set; }

    public decimal TotalLaborCostThisMonth { get; set; }

    public decimal TotalLaborHoursThisMonth { get; set; }

    public decimal LaborCostPercentage { get; set; }

    public decimal RevenuePerLaborHour { get; set; }

    public int DepartmentsOverBudget { get; set; }

    public int ServiceChargePoolsPendingApproval { get; set; }

    public int ServiceChargePoolsPendingPosting { get; set; }
}
