using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Banquet;
using Vantage.PMS.Models.Booking;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FoodBeverage;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class ExecutiveKPIService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ExecutiveSummaryMetrics> GetSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        var endExclusive = end.AddDays(1);
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);

        var totalRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive);
        var occupiedRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.Occupied);
        var dirtyRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.Dirty);
        var outOfOrderRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && room.Status == RoomStatus.OutOfOrder);
        var availableRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive && (room.Status == RoomStatus.Available || room.Status == RoomStatus.Clean || room.Status == RoomStatus.Inspected));

        var roomRevenue = await SumFolioRevenueAsync(start, endExclusive, ChargeCategory.Room);
        var fbRevenue = await _context.POSOrders
            .AsNoTracking()
            .Where(order => order.OrderDate >= start && order.OrderDate < endExclusive && order.OrderStatus != POSOrderStatus.Cancelled)
            .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
        fbRevenue += await SumFolioRevenueAsync(start, endExclusive, ChargeCategory.FoodBeverage);

        var banquetRevenue = await _context.BanquetCharges
            .AsNoTracking()
            .Where(charge => charge.ChargeDate >= start && charge.ChargeDate < endExclusive && !charge.IsVoided)
            .SumAsync(charge => (decimal?)charge.Amount) ?? 0;
        banquetRevenue += await _context.BanquetEvents
            .AsNoTracking()
            .Where(banquetEvent => banquetEvent.EventDate >= start && banquetEvent.EventDate < endExclusive && banquetEvent.EventStatus != BanquetEventStatus.Cancelled && banquetEvent.EventStatus != BanquetEventStatus.Lost)
            .SumAsync(banquetEvent => (decimal?)((banquetEvent.BanquetPackage != null ? banquetEvent.BanquetPackage.PricePerPax : 0) * banquetEvent.GuaranteedPax)) ?? 0;

        var otherRevenue = await _context.FolioItems
            .AsNoTracking()
            .Where(item => !item.IsVoided && item.PostingDate >= start && item.PostingDate < endExclusive &&
                (item.ChargeCodeDefinition == null ||
                    item.ChargeCodeDefinition.ChargeCategory == ChargeCategory.Miscellaneous ||
                    item.ChargeCodeDefinition.ChargeCategory == ChargeCategory.Adjustment))
            .SumAsync(item => (decimal?)item.Amount) ?? 0;

        var totalRevenue = roomRevenue + fbRevenue + banquetRevenue + otherRevenue;
        var roomsSold = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation =>
                (reservation.Status == ReservationStatus.CheckedIn ||
                reservation.Status == ReservationStatus.CheckedOut) &&
                reservation.ArrivalDate < endExclusive &&
                reservation.DepartureDate > start);
        var adr = roomsSold <= 0 ? 0 : roomRevenue / roomsSold;
        var revPar = totalRooms <= 0 ? 0 : roomRevenue / totalRooms;

        var totalPayments = await _context.Payments
            .AsNoTracking()
            .Where(payment => payment.PaymentDate >= start && payment.PaymentDate < endExclusive && payment.Status == PaymentStatus.Completed)
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0;
        totalPayments += await _context.ARPayments
            .AsNoTracking()
            .Where(payment => payment.PaymentDate >= start && payment.PaymentDate < endExclusive)
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0;

        var paymentsToday = await _context.Payments
            .AsNoTracking()
            .Where(payment => payment.PaymentDate >= today && payment.PaymentDate < tomorrow && payment.Status == PaymentStatus.Completed)
            .SumAsync(payment => (decimal?)payment.Amount) ?? 0;

        var arBalance = await _context.ARInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Balance > 0 && invoice.Status != ARInvoiceStatus.Cancelled && invoice.Status != ARInvoiceStatus.WrittenOff)
            .SumAsync(invoice => (decimal?)invoice.Balance) ?? 0;
        var apBalance = await _context.APInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Balance > 0 && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
            .SumAsync(invoice => (decimal?)invoice.Balance) ?? 0;

        var laborCost = await _context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < endExclusive &&
                entry.PayrollPeriod.EndDate >= start &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .SumAsync(entry => (decimal?)(entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay)) ?? 0;
        var laborCostMonthToDate = await _context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < nextMonth &&
                entry.PayrollPeriod.EndDate >= monthStart &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .SumAsync(entry => (decimal?)(entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay)) ?? 0;

        var accountBalances = await GetPostedAccountTypeBalancesAsync(start, endExclusive);
        var glRevenue = CreditNormalAmount(accountBalances, GLAccountType.Revenue, GLAccountType.OtherIncome);
        var costOfSales = DebitNormalAmount(accountBalances, GLAccountType.CostOfSales);
        var expenses = DebitNormalAmount(accountBalances, GLAccountType.Expense, GLAccountType.OtherExpense);
        var grossOperatingProfit = glRevenue == 0 && totalRevenue > 0 ? totalRevenue - costOfSales - expenses : glRevenue - costOfSales - expenses;
        var netIncome = grossOperatingProfit;

        var guestSatisfaction = await _context.GuestFeedbacks
            .AsNoTracking()
            .Where(feedback => feedback.SubmittedAt >= start && feedback.SubmittedAt < endExclusive)
            .AverageAsync(feedback => (decimal?)feedback.Rating);

        var totalReservations = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation => reservation.ArrivalDate >= start && reservation.ArrivalDate < endExclusive);
        var cancellations = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation => reservation.ArrivalDate >= start && reservation.ArrivalDate < endExclusive && reservation.Status == ReservationStatus.Cancelled);
        var noShows = await _context.Reservations
            .AsNoTracking()
            .CountAsync(reservation => reservation.ArrivalDate >= start && reservation.ArrivalDate < endExclusive && reservation.Status == ReservationStatus.NoShow);
        var bookingRequests = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(request => request.CreatedAt >= start && request.CreatedAt < endExclusive);
        var convertedBookings = await _context.BookingRequests
            .AsNoTracking()
            .CountAsync(request => request.CreatedAt >= start && request.CreatedAt < endExclusive && request.BookingStatus == BookingRequestStatus.ConvertedToReservation);

        var openServiceRequests = await _context.GuestServiceRequests
            .AsNoTracking()
            .CountAsync(request => request.Status != GuestServiceRequestStatus.Completed && request.Status != GuestServiceRequestStatus.Cancelled);
        var lowStockItems = await _context.InventoryItems
            .AsNoTracking()
            .CountAsync(item => item.IsActive && item.CurrentStock <= item.ReorderLevel);
        var pendingApprovals = await CountPendingApprovalsAsync();
        var highBalanceFolios = await CountHighBalanceFoliosAsync(50000);
        var openCriticalAlerts = await _context.ExecutiveAlerts
            .AsNoTracking()
            .CountAsync(alert => !alert.IsResolved && (alert.Severity == KPIStatus.Critical || alert.Severity == KPIStatus.Warning));

        return new ExecutiveSummaryMetrics
        {
            BusinessDate = today,
            PeriodStart = start,
            PeriodEnd = end,
            HotelName = await GetHotelNameAsync(),
            TotalRooms = totalRooms,
            OccupiedRooms = occupiedRooms,
            AvailableRooms = availableRooms,
            DirtyRooms = dirtyRooms,
            OutOfOrderRooms = outOfOrderRooms,
            OccupancyPercentage = Percent(occupiedRooms, totalRooms),
            ADR = adr,
            RevPAR = revPar,
            TotalRoomRevenue = roomRevenue,
            TotalFBRevenue = fbRevenue,
            TotalBanquetRevenue = banquetRevenue,
            TotalOtherRevenue = otherRevenue,
            TotalRevenue = totalRevenue,
            TotalRevenueMonthToDate = await GetMonthToDateRevenueAsync(monthStart, nextMonth),
            TotalPayments = totalPayments,
            CashPaymentsToday = paymentsToday,
            GrossOperatingProfit = grossOperatingProfit,
            NetIncome = netIncome,
            ARBalance = arBalance,
            APBalance = apBalance,
            LaborCost = laborCost,
            LaborCostMonthToDate = laborCostMonthToDate,
            LaborCostPercentage = Percent(laborCost, totalRevenue),
            GuestSatisfactionScore = guestSatisfaction,
            OpenCriticalIssues = openCriticalAlerts,
            PendingApprovals = pendingApprovals,
            LowStockRisk = lowStockItems,
            HighBalanceFolios = highBalanceFolios,
            Arrivals = await _context.Reservations.AsNoTracking().CountAsync(reservation => reservation.ArrivalDate >= today && reservation.ArrivalDate < tomorrow && reservation.Status == ReservationStatus.Reserved),
            Departures = await _context.Reservations.AsNoTracking().CountAsync(reservation => reservation.DepartureDate >= today && reservation.DepartureDate < tomorrow && reservation.Status == ReservationStatus.CheckedIn),
            InHouseGuests = await _context.Reservations.AsNoTracking().CountAsync(reservation => reservation.Status == ReservationStatus.CheckedIn),
            NoShows = noShows,
            Cancellations = cancellations,
            OpenServiceRequests = openServiceRequests,
            BanquetEventsToday = await _context.BanquetEvents.AsNoTracking().CountAsync(banquetEvent => banquetEvent.EventDate >= today && banquetEvent.EventDate < tomorrow && banquetEvent.EventStatus != BanquetEventStatus.Cancelled && banquetEvent.EventStatus != BanquetEventStatus.Lost),
            BookingConversionRate = Percent(convertedBookings, bookingRequests),
            CancellationRate = Percent(cancellations, totalReservations),
            NoShowRate = Percent(noShows, totalReservations),
            DirtyRoomsPercentage = Percent(dirtyRooms, totalRooms),
            OutOfOrderRoomsPercentage = Percent(outOfOrderRooms, totalRooms),
            FoodCostPercentage = CalculateCostPercentage(accountBalances, "Food"),
            BeverageCostPercentage = CalculateCostPercentage(accountBalances, "Beverage"),
            BanquetProfitMargin = Percent(banquetRevenue, banquetRevenue),
            RevenuePerLaborHour = await CalculateRevenuePerLaborHourAsync(totalRevenue, start, endExclusive)
        };
    }

    public async Task<IList<ExecutiveKpiScoreRow>> GetScorecardAsync(DateTime startDate, DateTime endDate)
    {
        var summary = await GetSummaryAsync(startDate, endDate);
        var actuals = summary.ToKpiActuals();
        var kpis = await _context.ExecutiveKPIs
            .AsNoTracking()
            .Where(kpi => kpi.IsActive)
            .OrderBy(kpi => kpi.SortOrder)
            .ToListAsync();
        var benchmarks = await _context.KPIBenchmarkSettings
            .AsNoTracking()
            .Where(setting => setting.IsActive && setting.EffectiveFrom <= endDate && (setting.EffectiveTo == null || setting.EffectiveTo >= startDate))
            .ToListAsync();

        return kpis.Select(kpi =>
        {
            var benchmark = benchmarks
                .Where(setting => setting.KPIName == kpi.KPIName)
                .OrderByDescending(setting => setting.EffectiveFrom)
                .FirstOrDefault();
            var target = benchmark?.TargetValue ?? kpi.TargetValue;
            var warning = benchmark?.WarningThreshold ?? kpi.WarningThreshold;
            var critical = benchmark?.CriticalThreshold ?? kpi.CriticalThreshold;
            var actual = actuals.TryGetValue(kpi.KPICode, out var value) ? value : 0;
            decimal? variance = target is null ? null : actual - target.Value;
            decimal? variancePercentage = target is null || target.Value == 0 ? null : variance / Math.Abs(target.Value) * 100;
            return new ExecutiveKpiScoreRow(
                kpi.Id,
                kpi.KPIName,
                kpi.KPICode,
                kpi.Category,
                actual,
                target,
                variance,
                variancePercentage,
                ResolveStatus(actual, target, warning, critical, kpi.IsHigherBetter),
                kpi.IsHigherBetter,
                kpi.FormulaDescription ?? string.Empty);
        }).ToList();
    }

    public static KPIStatus ResolveStatus(decimal actual, decimal? target, decimal? warning, decimal? critical, bool isHigherBetter)
    {
        if (target is null)
        {
            return KPIStatus.NotAvailable;
        }

        if (isHigherBetter)
        {
            if (actual >= target.Value)
            {
                return KPIStatus.Excellent;
            }

            if (critical is not null && actual <= critical.Value)
            {
                return KPIStatus.Critical;
            }

            if (warning is not null && actual <= warning.Value)
            {
                return KPIStatus.Warning;
            }

            return KPIStatus.Watch;
        }

        if (actual <= target.Value)
        {
            return KPIStatus.Excellent;
        }

        if (critical is not null && actual >= critical.Value)
        {
            return KPIStatus.Critical;
        }

        if (warning is not null && actual >= warning.Value)
        {
            return KPIStatus.Warning;
        }

        return KPIStatus.Watch;
    }

    private async Task<decimal> SumFolioRevenueAsync(DateTime start, DateTime endExclusive, ChargeCategory category)
    {
        return await _context.FolioItems
            .AsNoTracking()
            .Where(item => !item.IsVoided &&
                item.PostingDate >= start &&
                item.PostingDate < endExclusive &&
                ((item.ChargeCodeDefinition != null && item.ChargeCodeDefinition.ChargeCategory == category) ||
                    (category == ChargeCategory.Room && item.ChargeCode.ToUpper().StartsWith("ROOM")) ||
                    (category == ChargeCategory.FoodBeverage && item.ChargeCode.ToUpper().StartsWith("FB"))))
            .SumAsync(item => (decimal?)item.Amount) ?? 0;
    }

    private async Task<string> GetHotelNameAsync()
    {
        return await _context.Hotels.AsNoTracking().OrderBy(hotel => hotel.Id).Select(hotel => hotel.Name).FirstOrDefaultAsync()
            ?? "Vantage Grand Hotel";
    }

    private async Task<decimal> GetMonthToDateRevenueAsync(DateTime monthStart, DateTime nextMonth)
    {
        var folioRevenue = await _context.FolioItems
            .AsNoTracking()
            .Where(item => !item.IsVoided && item.PostingDate >= monthStart && item.PostingDate < nextMonth)
            .SumAsync(item => (decimal?)item.Amount) ?? 0;
        var posRevenue = await _context.POSOrders
            .AsNoTracking()
            .Where(order => order.OrderDate >= monthStart && order.OrderDate < nextMonth && order.OrderStatus != POSOrderStatus.Cancelled)
            .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
        var banquetRevenue = await _context.BanquetCharges
            .AsNoTracking()
            .Where(charge => charge.ChargeDate >= monthStart && charge.ChargeDate < nextMonth && !charge.IsVoided)
            .SumAsync(charge => (decimal?)charge.Amount) ?? 0;
        return folioRevenue + posRevenue + banquetRevenue;
    }

    private async Task<int> CountPendingApprovalsAsync()
    {
        return await _context.VoidRequests.CountAsync(request => request.Status == ApprovalStatus.Pending)
            + await _context.DiscountApprovals.CountAsync(approval => approval.Status == ApprovalStatus.Pending)
            + await _context.RefundTransactions.CountAsync(refund => refund.Status == RefundStatus.Requested || refund.Status == RefundStatus.ForApproval)
            + await _context.PaymentVouchers.CountAsync(voucher => voucher.Status == PaymentVoucherStatus.ForApproval)
            + await _context.APInvoices.CountAsync(invoice => invoice.Status == APInvoiceStatus.ForApproval)
            + await _context.PayrollPeriods.CountAsync(period => period.Status == PayrollPeriodStatus.ForApproval)
            + await _context.ServiceChargePools.CountAsync(pool => pool.Status == ServiceChargePoolStatus.ForApproval);
    }

    private async Task<int> CountHighBalanceFoliosAsync(decimal threshold)
    {
        var balances = await _context.Folios
            .AsNoTracking()
            .Select(folio => new
            {
                Balance = (_context.FolioItems.Where(item => item.FolioId == folio.Id && !item.IsVoided).Sum(item => (decimal?)item.Amount) ?? 0)
                    - (_context.Payments.Where(payment => payment.FolioId == folio.Id && payment.Status == PaymentStatus.Completed).Sum(payment => (decimal?)payment.Amount) ?? 0)
            })
            .ToListAsync();
        return balances.Count(folio => folio.Balance >= threshold);
    }

    private async Task<IList<AccountTypeBalance>> GetPostedAccountTypeBalancesAsync(DateTime start, DateTime endExclusive)
    {
        return await _context.JournalEntryLines
            .AsNoTracking()
            .Where(line => line.JournalEntry != null &&
                line.GLAccount != null &&
                line.JournalEntry.Status == JournalEntryStatus.Posted &&
                line.JournalEntry.JournalDate >= start &&
                line.JournalEntry.JournalDate < endExclusive)
            .Select(line => new AccountTypeBalance(
                line.GLAccount!.AccountType,
                line.GLAccount.AccountName,
                line.DebitAmount,
                line.CreditAmount))
            .ToListAsync();
    }

    private static decimal CreditNormalAmount(IEnumerable<AccountTypeBalance> rows, params GLAccountType[] types)
    {
        return rows.Where(row => types.Contains(row.AccountType)).Sum(row => row.CreditAmount - row.DebitAmount);
    }

    private static decimal DebitNormalAmount(IEnumerable<AccountTypeBalance> rows, params GLAccountType[] types)
    {
        return rows.Where(row => types.Contains(row.AccountType)).Sum(row => row.DebitAmount - row.CreditAmount);
    }

    private static decimal CalculateCostPercentage(IEnumerable<AccountTypeBalance> rows, string accountNameHint)
    {
        var revenue = rows
            .Where(row => row.AccountType == GLAccountType.Revenue && row.AccountName.Contains(accountNameHint, StringComparison.OrdinalIgnoreCase))
            .Sum(row => row.CreditAmount - row.DebitAmount);
        var cost = rows
            .Where(row => row.AccountType == GLAccountType.CostOfSales && row.AccountName.Contains(accountNameHint, StringComparison.OrdinalIgnoreCase))
            .Sum(row => row.DebitAmount - row.CreditAmount);
        return Percent(cost, revenue);
    }

    private async Task<decimal> CalculateRevenuePerLaborHourAsync(decimal totalRevenue, DateTime start, DateTime endExclusive)
    {
        var laborHours = await _context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < endExclusive &&
                entry.PayrollPeriod.EndDate >= start &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .SumAsync(entry => (decimal?)(entry.RegularHours + entry.OvertimeHours + entry.NightDifferentialHours)) ?? 0;
        return laborHours <= 0 ? 0 : totalRevenue / laborHours;
    }

    private static decimal Percent(decimal numerator, decimal denominator)
    {
        return denominator <= 0 ? 0 : numerator / denominator * 100;
    }
}

public class DepartmentPerformanceService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IList<DepartmentPerformanceRow>> GetDepartmentPerformanceAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var endExclusive = endDate.Date.AddDays(1);
        var rows = new Dictionary<string, DepartmentPerformanceRow>(StringComparer.OrdinalIgnoreCase);

        var glRows = await _context.JournalEntryLines
            .AsNoTracking()
            .Where(line => line.JournalEntry != null &&
                line.GLAccount != null &&
                line.JournalEntry.Status == JournalEntryStatus.Posted &&
                line.JournalEntry.JournalDate >= start &&
                line.JournalEntry.JournalDate < endExclusive)
            .Select(line => new
            {
                line.GLAccount!.AccountType,
                line.DebitAmount,
                line.CreditAmount,
                DepartmentId = line.GLAccount.UsaliDepartmentId,
                DepartmentName = line.GLAccount.UsaliDepartment != null ? line.GLAccount.UsaliDepartment.Name : "Unmapped"
            })
            .ToListAsync();

        foreach (var group in glRows.GroupBy(row => new { row.DepartmentId, row.DepartmentName }))
        {
            var key = group.Key.DepartmentName;
            var row = GetOrAdd(rows, key);
            row.USALIDepartmentId = group.Key.DepartmentId;
            row.Revenue += group.Where(line => line.AccountType is GLAccountType.Revenue or GLAccountType.OtherIncome).Sum(line => line.CreditAmount - line.DebitAmount);
            row.CostOfSales += group.Where(line => line.AccountType == GLAccountType.CostOfSales).Sum(line => line.DebitAmount - line.CreditAmount);
            row.OtherExpenses += group.Where(line => line.AccountType is GLAccountType.Expense or GLAccountType.OtherExpense).Sum(line => line.DebitAmount - line.CreditAmount);
        }

        var payroll = await _context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.PayrollPeriod != null &&
                entry.PayrollPeriod.StartDate < endExclusive &&
                entry.PayrollPeriod.EndDate >= start &&
                entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .Select(entry => new
            {
                entry.DepartmentId,
                DepartmentName = entry.Department != null ? entry.Department.Name : entry.USALIDepartment != null ? entry.USALIDepartment.Name : "Unassigned",
                entry.USALIDepartmentId,
                Cost = entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay
            })
            .ToListAsync();
        foreach (var group in payroll.GroupBy(entry => new { entry.DepartmentId, entry.USALIDepartmentId, entry.DepartmentName }))
        {
            var row = GetOrAdd(rows, group.Key.DepartmentName);
            row.DepartmentId = group.Key.DepartmentId;
            row.USALIDepartmentId ??= group.Key.USALIDepartmentId;
            row.PayrollCost += group.Sum(entry => entry.Cost);
        }

        if (rows.Count == 0)
        {
            await AddOperationalFallbackRowsAsync(rows, start, endExclusive);
        }

        var budgets = await _context.DepartmentLaborBudgets
            .AsNoTracking()
            .Where(budget => budget.Year == start.Year && budget.Month == start.Month)
            .Select(budget => new
            {
                budget.DepartmentId,
                budget.USALIDepartmentId,
                budget.BudgetedLaborCost
            })
            .ToListAsync();

        foreach (var row in rows.Values)
        {
            row.DepartmentProfit = row.Revenue - row.CostOfSales - row.PayrollCost - row.OtherExpenses;
            row.DepartmentProfitMargin = Percent(row.DepartmentProfit, row.Revenue);
            row.LaborCostPercentage = Percent(row.PayrollCost, row.Revenue);
            var budget = budgets.FirstOrDefault(item =>
                (row.DepartmentId != null && item.DepartmentId == row.DepartmentId) ||
                (row.USALIDepartmentId != null && item.USALIDepartmentId == row.USALIDepartmentId));
            row.BudgetAmount = budget?.BudgetedLaborCost;
            row.VarianceAmount = budget is null ? null : row.PayrollCost - budget.BudgetedLaborCost;
            row.VariancePercentage = budget is null ? null : Percent(row.VarianceAmount ?? 0, budget.BudgetedLaborCost);
        }

        return rows.Values.OrderByDescending(row => row.Revenue).ThenBy(row => row.DepartmentName).ToList();
    }

    private async Task AddOperationalFallbackRowsAsync(IDictionary<string, DepartmentPerformanceRow> rows, DateTime start, DateTime endExclusive)
    {
        var roomRevenue = await _context.FolioItems
            .Where(item => !item.IsVoided && item.PostingDate >= start && item.PostingDate < endExclusive &&
                ((item.ChargeCodeDefinition != null && item.ChargeCodeDefinition.ChargeCategory == ChargeCategory.Room) || item.ChargeCode.ToUpper().StartsWith("ROOM")))
            .SumAsync(item => (decimal?)item.Amount) ?? 0;
        var fbRevenue = await _context.POSOrders
            .Where(order => order.OrderDate >= start && order.OrderDate < endExclusive && order.OrderStatus != POSOrderStatus.Cancelled)
            .SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
        var banquetRevenue = await _context.BanquetCharges
            .Where(charge => charge.ChargeDate >= start && charge.ChargeDate < endExclusive && !charge.IsVoided)
            .SumAsync(charge => (decimal?)charge.Amount) ?? 0;
        GetOrAdd(rows, "Rooms").Revenue = roomRevenue;
        GetOrAdd(rows, "Food and Beverage").Revenue = fbRevenue;
        GetOrAdd(rows, "Banquet").Revenue = banquetRevenue;
    }

    private static DepartmentPerformanceRow GetOrAdd(IDictionary<string, DepartmentPerformanceRow> rows, string name)
    {
        if (!rows.TryGetValue(name, out var row))
        {
            row = new DepartmentPerformanceRow { DepartmentName = name };
            rows[name] = row;
        }

        return row;
    }

    private static decimal Percent(decimal numerator, decimal denominator)
    {
        return denominator <= 0 ? 0 : numerator / denominator * 100;
    }
}

public class ExecutiveAlertService(ApplicationDbContext context)
{
    private readonly ApplicationDbContext _context = context;

    public async Task<IList<ExecutiveAlert>> GetOpenAlertsAsync(int take = 25)
    {
        return await _context.ExecutiveAlerts
            .AsNoTracking()
            .Where(alert => !alert.IsResolved)
            .OrderByDescending(alert => alert.Severity)
            .ThenByDescending(alert => alert.AlertDate)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IList<ExecutiveAlert>> GenerateAlertsAsync(ExecutiveSummaryMetrics summary)
    {
        var alerts = new List<ExecutiveAlert>();
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.Performance, KPIStatus.Warning, "Rooms", "Occupancy below target", summary.OccupancyPercentage < 60, $"Occupancy is {summary.OccupancyPercentage:N1}%, below the current executive watch threshold.", "Review rate strategy, low-demand dates, and sales account pickup.", "Occupancy", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.OperationalRisk, KPIStatus.Warning, "Housekeeping", "High occupancy with dirty room pressure", summary.OccupancyPercentage > 95 && summary.DirtyRoomsPercentage > 15, $"Occupancy is {summary.OccupancyPercentage:N1}% and dirty rooms are {summary.DirtyRoomsPercentage:N1}% of inventory.", "Prioritize departure room turnaround and inspect rooms needed for arrivals.", "Rooms", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.Performance, KPIStatus.Warning, "Revenue", "ADR below target", summary.ADR > 0 && summary.ADR < 3200, $"ADR is {summary.ADR:C}, below the configured watch level.", "Review discounts, rate plans, and channel mix.", "ADR", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.Performance, KPIStatus.Warning, "Revenue", "RevPAR below target", summary.RevPAR > 0 && summary.RevPAR < 2100, $"RevPAR is {summary.RevPAR:C}, below the configured watch level.", "Review occupancy and ADR improvement actions for the next booking window.", "RevPAR", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.CostControl, KPIStatus.Warning, "Labor", "Labor cost above target", summary.LaborCostPercentage > 35, $"Labor cost is {summary.LaborCostPercentage:N1}% of revenue.", "Review staffing, overtime, and department productivity.", "LaborCost", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.FinancialRisk, KPIStatus.Warning, "Accounts Receivable", "AR balance above threshold", summary.ARBalance > 400000, $"Open AR balance is {summary.ARBalance:C}.", "Prioritize city ledger collection follow-up.", "ARInvoices", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.FinancialRisk, KPIStatus.Warning, "Accounts Payable", "AP balance above threshold", summary.APBalance > 350000, $"Open AP balance is {summary.APBalance:C}.", "Review supplier payment priorities and payment voucher approval queues.", "APInvoices", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.GuestExperience, KPIStatus.Critical, "Guest Portal", "Guest rating below target", summary.GuestSatisfactionScore is not null && summary.GuestSatisfactionScore < 3.5m, $"Average guest rating is {summary.GuestSatisfactionScore:N1}.", "Contact low-rating guests and document recovery actions.", "GuestFeedback", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.OperationalRisk, KPIStatus.Warning, "Guest Portal", "Urgent service requests open", summary.OpenServiceRequests > 10, $"{summary.OpenServiceRequests} service requests are still open.", "Assign open requests and track completion by priority.", "GuestServiceRequests", null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.CostControl, KPIStatus.Warning, "Inventory", "Low stock risk", summary.LowStockRisk > 5, $"{summary.LowStockRisk} active inventory items are at or below reorder level.", "Review purchasing actions for low-stock operational items.", "InventoryItems", null);

        var openPreviousShifts = await _context.CashierShifts.CountAsync(shift => shift.Status == CashierShiftStatus.Open && shift.BusinessDate < summary.BusinessDate.Date);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.FinancialRisk, KPIStatus.Critical, "Finance", "Open cashier shifts from prior business date", openPreviousShifts > 0, $"{openPreviousShifts} cashier shift(s) from a prior business date are still open.", "Close or audit old cashier shifts before finance close.", "CashierShifts", null);

        var trialBalance = await _context.JournalEntryLines
            .Where(line => line.JournalEntry != null && line.JournalEntry.Status == JournalEntryStatus.Posted)
            .GroupBy(_ => 1)
            .Select(group => new { Debit = group.Sum(line => line.DebitAmount), Credit = group.Sum(line => line.CreditAmount) })
            .FirstOrDefaultAsync();
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.FinancialRisk, KPIStatus.Critical, "Accounting", "Trial balance out of balance", (trialBalance?.Debit ?? 0) != (trialBalance?.Credit ?? 0), "Posted journal entry totals are not balanced.", "Review journal entries before releasing owner reports.", "TrialBalance", null);

        var confirmedEventsWithoutBeo = await _context.BanquetEvents.CountAsync(e => e.EventStatus == BanquetEventStatus.Confirmed && e.BanquetEventOrder == null);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.OperationalRisk, KPIStatus.Warning, "Banquet", "Confirmed banquet event without BEO", confirmedEventsWithoutBeo > 0, $"{confirmedEventsWithoutBeo} confirmed banquet event(s) do not have a BEO.", "Prepare and approve BEOs before event operations.", "BanquetEventOrder", null);

        var bankReconPending = await _context.BankReconciliations.CountAsync(recon => recon.Status != BankReconciliationStatus.Approved && recon.Status != BankReconciliationStatus.Cancelled);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.FinancialRisk, KPIStatus.Watch, "Banking", "Bank reconciliation pending", bankReconPending > 0, $"{bankReconPending} bank reconciliation(s) are pending.", "Complete bank reconciliation before month-end review.", "BankReconciliations", null);

        var monthEndIncomplete = await _context.MonthEndCloseChecklists.CountAsync(item => item.Status == MonthEndChecklistStatus.Pending || item.Status == MonthEndChecklistStatus.IssueFound);
        await AddIfAsync(alerts, summary.BusinessDate, ExecutiveAlertType.Compliance, KPIStatus.Watch, "Month-End", "Month-end close checklist incomplete", monthEndIncomplete > 0, $"{monthEndIncomplete} month-end checklist item(s) are not complete.", "Resolve close checklist exceptions before locking the period.", "MonthEndCloseChecklist", null);

        await _context.SaveChangesAsync();
        return alerts;
    }

    public async Task<bool> ResolveAsync(int id, string resolvedBy)
    {
        var alert = await _context.ExecutiveAlerts.FindAsync(id);
        if (alert is null)
        {
            return false;
        }

        alert.IsResolved = true;
        alert.ResolvedBy = resolvedBy;
        alert.ResolvedAt = DateTime.Now;
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task AddIfAsync(ICollection<ExecutiveAlert> alerts, DateTime date, ExecutiveAlertType type, KPIStatus severity, string module, string title, bool condition, string message, string recommendation, string referenceType, int? referenceId)
    {
        if (!condition)
        {
            return;
        }

        var exists = await _context.ExecutiveAlerts.AnyAsync(alert =>
            !alert.IsResolved &&
            alert.AlertDate == date.Date &&
            alert.Module == module &&
            alert.Title == title &&
            alert.RelatedReferenceType == referenceType &&
            alert.RelatedReferenceId == referenceId);
        if (exists)
        {
            return;
        }

        var alert = new ExecutiveAlert
        {
            AlertDate = date.Date,
            AlertType = type,
            Severity = severity,
            Module = module,
            Title = title,
            Message = message,
            RecommendedAction = recommendation,
            RelatedReferenceType = referenceType,
            RelatedReferenceId = referenceId,
            CreatedAt = DateTime.Now
        };
        _context.ExecutiveAlerts.Add(alert);
        alerts.Add(alert);
    }
}

public class ExecutiveReportingService(
    ApplicationDbContext context,
    ExecutiveKPIService kpiService,
    DepartmentPerformanceService departmentPerformanceService,
    ExecutiveAlertService alertService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly ExecutiveKPIService _kpiService = kpiService;
    private readonly DepartmentPerformanceService _departmentPerformanceService = departmentPerformanceService;
    private readonly ExecutiveAlertService _alertService = alertService;

    public async Task<ExecutiveDashboardView> GetDashboardAsync(DateTime startDate, DateTime endDate)
    {
        var summary = await _kpiService.GetSummaryAsync(startDate, endDate);
        var scorecard = await _kpiService.GetScorecardAsync(startDate, endDate);
        var departments = await _departmentPerformanceService.GetDepartmentPerformanceAsync(startDate, endDate);
        var alerts = await _alertService.GetOpenAlertsAsync(8);
        var trend = await GetSevenDayTrendAsync(summary.BusinessDate);
        return new ExecutiveDashboardView(summary, scorecard.Take(12).ToList(), departments.Take(6).ToList(), alerts, trend);
    }

    public async Task<ExecutiveReportSnapshot> GenerateSnapshotAsync(DateTime startDate, DateTime endDate, ExecutiveReportType reportType, string preparedBy)
    {
        var summary = await _kpiService.GetSummaryAsync(startDate, endDate);
        var snapshot = new ExecutiveReportSnapshot
        {
            ReportDate = DateTime.Today,
            PeriodStart = startDate.Date,
            PeriodEnd = endDate.Date,
            ReportType = reportType,
            HotelName = summary.HotelName,
            PreparedBy = preparedBy,
            PreparedAt = DateTime.Now,
            OccupancyPercentage = summary.OccupancyPercentage,
            ADR = summary.ADR,
            RevPAR = summary.RevPAR,
            TotalRoomRevenue = summary.TotalRoomRevenue,
            TotalFBRevenue = summary.TotalFBRevenue,
            TotalBanquetRevenue = summary.TotalBanquetRevenue,
            TotalOtherRevenue = summary.TotalOtherRevenue,
            TotalRevenue = summary.TotalRevenue,
            TotalPayments = summary.TotalPayments,
            GrossOperatingProfit = summary.GrossOperatingProfit,
            NetIncome = summary.NetIncome,
            ARBalance = summary.ARBalance,
            APBalance = summary.APBalance,
            LaborCost = summary.LaborCost,
            LaborCostPercentage = summary.LaborCostPercentage,
            GuestSatisfactionScore = summary.GuestSatisfactionScore,
            OpenCriticalIssues = summary.OpenCriticalIssues,
            SummaryText = BuildExecutiveSummaryText(summary),
            Notes = "Management report for internal decision-making. Accounting mappings and statutory reports must be reviewed by authorized finance personnel."
        };
        _context.ExecutiveReportSnapshots.Add(snapshot);

        var kpiRows = await _kpiService.GetScorecardAsync(startDate, endDate);
        foreach (var row in kpiRows)
        {
            _context.ExecutiveKPIResults.Add(new ExecutiveKPIResult
            {
                ExecutiveKPIId = row.ExecutiveKPIId,
                ResultDate = DateTime.Today,
                PeriodStart = startDate.Date,
                PeriodEnd = endDate.Date,
                ActualValue = row.ActualValue,
                TargetValue = row.TargetValue,
                Variance = row.Variance,
                VariancePercentage = row.VariancePercentage,
                Status = row.Status,
                Notes = row.FormulaDescription
            });
        }

        var departmentRows = await _departmentPerformanceService.GetDepartmentPerformanceAsync(startDate, endDate);
        foreach (var row in departmentRows)
        {
            _context.DepartmentPerformanceSnapshots.Add(new DepartmentPerformanceSnapshot
            {
                SnapshotDate = DateTime.Today,
                PeriodStart = startDate.Date,
                PeriodEnd = endDate.Date,
                DepartmentId = row.DepartmentId,
                USALIDepartmentId = row.USALIDepartmentId,
                DepartmentName = row.DepartmentName,
                Revenue = row.Revenue,
                CostOfSales = row.CostOfSales,
                PayrollCost = row.PayrollCost,
                OtherExpenses = row.OtherExpenses,
                DepartmentProfit = row.DepartmentProfit,
                DepartmentProfitMargin = row.DepartmentProfitMargin,
                LaborCostPercentage = row.LaborCostPercentage,
                BudgetAmount = row.BudgetAmount,
                VarianceAmount = row.VarianceAmount,
                VariancePercentage = row.VariancePercentage,
                Notes = "Generated from executive snapshot."
            });
        }

        await _alertService.GenerateAlertsAsync(summary);
        await _context.SaveChangesAsync();
        return snapshot;
    }

    public async Task<IList<ExecutiveTrendRow>> GetSevenDayTrendAsync(DateTime businessDate)
    {
        var rows = new List<ExecutiveTrendRow>();
        for (var i = 6; i >= 0; i--)
        {
            var date = businessDate.Date.AddDays(-i);
            var next = date.AddDays(1);
            var roomRevenue = await _context.FolioItems.AsNoTracking().Where(item => !item.IsVoided && item.PostingDate >= date && item.PostingDate < next && ((item.ChargeCodeDefinition != null && item.ChargeCodeDefinition.ChargeCategory == ChargeCategory.Room) || item.ChargeCode.ToUpper().StartsWith("ROOM"))).SumAsync(item => (decimal?)item.Amount) ?? 0;
            var posRevenue = await _context.POSOrders.AsNoTracking().Where(order => order.OrderDate >= date && order.OrderDate < next && order.OrderStatus != POSOrderStatus.Cancelled).SumAsync(order => (decimal?)order.TotalAmount) ?? 0;
            var totalRooms = await _context.Rooms.AsNoTracking().CountAsync(room => room.IsActive);
            var occupied = await _context.Reservations.AsNoTracking().CountAsync(reservation => reservation.Status == ReservationStatus.CheckedIn && reservation.ArrivalDate <= date && reservation.DepartureDate > date);
            rows.Add(new ExecutiveTrendRow(date, Percent(occupied, totalRooms), roomRevenue + posRevenue));
        }

        return rows;
    }

    public static string BuildExecutiveSummaryText(ExecutiveSummaryMetrics summary)
    {
        return $"For the selected period, the hotel achieved {summary.OccupancyPercentage:N1}% occupancy with ADR of {summary.ADR:C} and RevPAR of {summary.RevPAR:C}. Total revenue reached {summary.TotalRevenue:C}, led by rooms revenue of {summary.TotalRoomRevenue:C} and F&B revenue of {summary.TotalFBRevenue:C}. Labor cost was {summary.LaborCostPercentage:N1}% of revenue. AR balance is {summary.ARBalance:C} and AP balance is {summary.APBalance:C}. Management should review critical alerts, collection follow-up, and room readiness before the next briefing.";
    }

    private static decimal Percent(decimal numerator, decimal denominator)
    {
        return denominator <= 0 ? 0 : numerator / denominator * 100;
    }
}

public class OwnerReportPackageService(ApplicationDbContext context, ExecutiveKPIService kpiService, DepartmentPerformanceService departmentPerformanceService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly ExecutiveKPIService _kpiService = kpiService;
    private readonly DepartmentPerformanceService _departmentPerformanceService = departmentPerformanceService;

    public async Task<OwnerReportPackage> CreateDefaultAsync(string packageName, string preparedFor, DateTime startDate, DateTime endDate, string preparedBy)
    {
        var package = new OwnerReportPackage
        {
            PackageName = packageName,
            PreparedFor = preparedFor,
            PeriodStart = startDate.Date,
            PeriodEnd = endDate.Date,
            PreparedBy = preparedBy,
            PreparedAt = DateTime.Now,
            Status = OwnerReportPackageStatus.Draft,
            Notes = "Management report for internal decision-making. Accounting mappings and statutory reports must be reviewed by authorized finance personnel."
        };

        var defaults = new[]
        {
            ("Cover Page", "Cover", "Hotel name, period, prepared by, and executive context."),
            ("Executive Summary", "Summary", "Rule-based summary of operational and financial performance."),
            ("KPI Scorecard", "KPI", "Executive KPIs, targets, variance, and status."),
            ("Department Performance", "Department", "Revenue, costs, labor, profit, and budget variance."),
            ("Financial Summary", "Finance", "Revenue, GOP, net income, AR, AP, and cash collection."),
            ("USALI Operating Summary", "USALI", "USALI-style management report shortcut."),
            ("Guest Experience", "Guest", "Guest ratings, requests, and service risks."),
            ("Labor Cost", "Labor", "Payroll cost, labor percentage, and productivity."),
            ("Revenue Intelligence", "Revenue", "Forward occupancy, bookings, and revenue opportunities."),
            ("Cost Control", "Cost", "Inventory, purchasing, AP, and labor cost controls."),
            ("Management Action Items", "Actions", "Recommended actions and unresolved executive alerts.")
        };

        var sort = 10;
        foreach (var (name, type, notes) in defaults)
        {
            package.Items.Add(new OwnerReportPackageItem
            {
                ReportName = name,
                ReportType = type,
                SortOrder = sort,
                IsIncluded = true,
                Notes = notes
            });
            sort += 10;
        }

        _context.OwnerReportPackages.Add(package);
        await _context.SaveChangesAsync();
        return package;
    }

    public async Task<OwnerPackagePrintView?> GetPrintViewAsync(int id)
    {
        var package = await _context.OwnerReportPackages
            .AsNoTracking()
            .Include(item => item.Items)
            .FirstOrDefaultAsync(item => item.Id == id);
        if (package is null)
        {
            return null;
        }

        var summary = await _kpiService.GetSummaryAsync(package.PeriodStart, package.PeriodEnd);
        var kpis = await _kpiService.GetScorecardAsync(package.PeriodStart, package.PeriodEnd);
        var departments = await _departmentPerformanceService.GetDepartmentPerformanceAsync(package.PeriodStart, package.PeriodEnd);
        return new OwnerPackagePrintView(package, summary, kpis, departments);
    }
}

public record AccountTypeBalance(GLAccountType AccountType, string AccountName, decimal DebitAmount, decimal CreditAmount);

public record ExecutiveDashboardView(
    ExecutiveSummaryMetrics Summary,
    IList<ExecutiveKpiScoreRow> Kpis,
    IList<DepartmentPerformanceRow> Departments,
    IList<ExecutiveAlert> Alerts,
    IList<ExecutiveTrendRow> Trend);

public record ExecutiveKpiScoreRow(
    int ExecutiveKPIId,
    string KPIName,
    string KPICode,
    ExecutiveKPICategory Category,
    decimal ActualValue,
    decimal? TargetValue,
    decimal? Variance,
    decimal? VariancePercentage,
    KPIStatus Status,
    bool IsHigherBetter,
    string FormulaDescription);

public record ExecutiveTrendRow(DateTime Date, decimal OccupancyPercentage, decimal Revenue);

public record OwnerPackagePrintView(
    OwnerReportPackage Package,
    ExecutiveSummaryMetrics Summary,
    IList<ExecutiveKpiScoreRow> Kpis,
    IList<DepartmentPerformanceRow> Departments);

public class DepartmentPerformanceRow
{
    public int? DepartmentId { get; set; }
    public int? USALIDepartmentId { get; set; }
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
}

public class ExecutiveSummaryMetrics
{
    public DateTime BusinessDate { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string HotelName { get; set; } = "Vantage Grand Hotel";
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public int AvailableRooms { get; set; }
    public int DirtyRooms { get; set; }
    public int OutOfOrderRooms { get; set; }
    public decimal OccupancyPercentage { get; set; }
    public decimal ADR { get; set; }
    public decimal RevPAR { get; set; }
    public decimal TotalRoomRevenue { get; set; }
    public decimal TotalFBRevenue { get; set; }
    public decimal TotalBanquetRevenue { get; set; }
    public decimal TotalOtherRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalRevenueMonthToDate { get; set; }
    public decimal TotalPayments { get; set; }
    public decimal CashPaymentsToday { get; set; }
    public decimal GrossOperatingProfit { get; set; }
    public decimal NetIncome { get; set; }
    public decimal ARBalance { get; set; }
    public decimal APBalance { get; set; }
    public decimal LaborCost { get; set; }
    public decimal LaborCostMonthToDate { get; set; }
    public decimal LaborCostPercentage { get; set; }
    public decimal? GuestSatisfactionScore { get; set; }
    public int OpenCriticalIssues { get; set; }
    public int PendingApprovals { get; set; }
    public int LowStockRisk { get; set; }
    public int HighBalanceFolios { get; set; }
    public int Arrivals { get; set; }
    public int Departures { get; set; }
    public int InHouseGuests { get; set; }
    public int NoShows { get; set; }
    public int Cancellations { get; set; }
    public int OpenServiceRequests { get; set; }
    public int BanquetEventsToday { get; set; }
    public decimal BookingConversionRate { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal NoShowRate { get; set; }
    public decimal DirtyRoomsPercentage { get; set; }
    public decimal OutOfOrderRoomsPercentage { get; set; }
    public decimal FoodCostPercentage { get; set; }
    public decimal BeverageCostPercentage { get; set; }
    public decimal BanquetProfitMargin { get; set; }
    public decimal RevenuePerLaborHour { get; set; }

    public Dictionary<string, decimal> ToKpiActuals() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["OCC"] = OccupancyPercentage,
        ["ADR"] = ADR,
        ["REVPAR"] = RevPAR,
        ["TOTAL_REV"] = TotalRevenue,
        ["ROOM_REV"] = TotalRoomRevenue,
        ["FB_REV"] = TotalFBRevenue,
        ["BANQUET_REV"] = TotalBanquetRevenue,
        ["GOP"] = GrossOperatingProfit,
        ["NET_INCOME"] = NetIncome,
        ["LABOR_PCT"] = LaborCostPercentage,
        ["AR_BAL"] = ARBalance,
        ["AP_BAL"] = APBalance,
        ["AVG_GUEST_RATING"] = GuestSatisfactionScore ?? 0,
        ["OPEN_REQUESTS"] = OpenServiceRequests,
        ["DIRTY_ROOM_PCT"] = DirtyRoomsPercentage,
        ["OOO_ROOM_PCT"] = OutOfOrderRoomsPercentage,
        ["FOOD_COST_PCT"] = FoodCostPercentage,
        ["BEV_COST_PCT"] = BeverageCostPercentage,
        ["BANQUET_MARGIN"] = BanquetProfitMargin,
        ["REV_PER_LABOR_HOUR"] = RevenuePerLaborHour,
        ["LOW_STOCK"] = LowStockRisk,
        ["BOOKING_CONVERSION"] = BookingConversionRate,
        ["CANCEL_RATE"] = CancellationRate,
        ["NOSHOW_RATE"] = NoShowRate
    };
}
