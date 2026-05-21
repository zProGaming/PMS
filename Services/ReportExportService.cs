using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Executive;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.Reports;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class ReportExportService(
    ApplicationDbContext context,
    AccountingReportService accountingReportService,
    CashFlowReportService cashFlowReportService,
    ARCollectionReportService arCollectionReportService,
    ExecutiveKPIService executiveKPIService,
    DepartmentPerformanceService departmentPerformanceService,
    AuditLogService auditLogService)
{
    private readonly ApplicationDbContext _context = context;
    private readonly AccountingReportService _accountingReportService = accountingReportService;
    private readonly CashFlowReportService _cashFlowReportService = cashFlowReportService;
    private readonly ARCollectionReportService _arCollectionReportService = arCollectionReportService;
    private readonly ExecutiveKPIService _executiveKPIService = executiveKPIService;
    private readonly DepartmentPerformanceService _departmentPerformanceService = departmentPerformanceService;
    private readonly AuditLogService _auditLogService = auditLogService;

    public async Task<ReportCsvResult?> BuildCsvForReportAsync(ReportCatalogEntry catalogEntry, DateTime? dateRangeStart, DateTime? dateRangeEnd)
    {
        var endDate = dateRangeEnd?.Date ?? DateTime.Today;
        var startDate = dateRangeStart?.Date ?? new DateTime(endDate.Year, endDate.Month, 1);
        var rows = new List<string[]>();

        switch (catalogEntry.ReportKey)
        {
            case "general-ledger":
            {
                rows.Add(["Date", "Journal Number", "Account Code", "Account Name", "Description", "Debit", "Credit"]);
                var lines = await _accountingReportService.GetLedgerLinesAsync(startDate, endDate, null);
                rows.AddRange(lines.Select(line => new[]
                {
                    FormatDate(line.JournalDate),
                    line.JournalNumber,
                    line.AccountCode,
                    line.AccountName,
                    line.Description,
                    FormatCurrency(line.DebitAmount),
                    FormatCurrency(line.CreditAmount)
                }));
                break;
            }
            case "trial-balance":
            {
                rows.Add(["Account Code", "Account Name", "Debit Balance", "Credit Balance"]);
                var balances = await _accountingReportService.GetAccountBalancesAsync(DateTime.MinValue.Date, endDate);
                rows.AddRange(balances.Select(row => new[]
                {
                    row.AccountCode,
                    row.AccountName,
                    FormatCurrency(row.DebitBalance),
                    FormatCurrency(row.CreditBalance)
                }));
                rows.Add(["Total", "", FormatCurrency(balances.Sum(row => row.DebitBalance)), FormatCurrency(balances.Sum(row => row.CreditBalance))]);
                break;
            }
            case "profit-loss":
            {
                var balances = await _accountingReportService.GetAccountBalancesAsync(startDate, endDate);
                var revenue = _accountingReportService.CreditNormalAmount(balances, GLAccountType.Revenue, GLAccountType.OtherIncome);
                var costOfSales = _accountingReportService.DebitNormalAmount(balances, GLAccountType.CostOfSales);
                var expenses = _accountingReportService.DebitNormalAmount(balances, GLAccountType.Expense, GLAccountType.OtherExpense);
                rows.Add(["Line", "Amount"]);
                rows.Add(["Revenue", FormatCurrency(revenue)]);
                rows.Add(["Cost of Sales", FormatCurrency(costOfSales)]);
                rows.Add(["Gross Profit", FormatCurrency(revenue - costOfSales)]);
                rows.Add(["Expenses", FormatCurrency(expenses)]);
                rows.Add(["Net Income", FormatCurrency(revenue - costOfSales - expenses)]);
                break;
            }
            case "balance-sheet":
            {
                var balances = await _accountingReportService.GetAccountBalancesAsync(DateTime.MinValue.Date, endDate);
                var assets = _accountingReportService.DebitNormalAmount(balances, GLAccountType.Asset);
                var liabilities = _accountingReportService.CreditNormalAmount(balances, GLAccountType.Liability);
                var equity = _accountingReportService.CreditNormalAmount(balances, GLAccountType.Equity);
                rows.Add(["Line", "Amount"]);
                rows.Add(["Assets", FormatCurrency(assets)]);
                rows.Add(["Liabilities", FormatCurrency(liabilities)]);
                rows.Add(["Equity", FormatCurrency(equity)]);
                rows.Add(["Liabilities and Equity", FormatCurrency(liabilities + equity)]);
                rows.Add(["Difference", FormatCurrency(assets - liabilities - equity)]);
                break;
            }
            case "statement-of-cash-flows":
            {
                var statement = await _cashFlowReportService.GenerateStatementAsync(startDate, endDate, CashFlowMethod.Direct);
                rows.Add(["Section", "Line Code", "Line Name", "Amount"]);
                rows.AddRange(statement.Lines.Select(line => new[]
                {
                    line.CashFlowSection.ToString(),
                    line.LineCode,
                    line.LineName,
                    FormatCurrency(line.Amount)
                }));
                rows.Add(Array.Empty<string>());
                rows.Add(["Summary", "BEGINNING_CASH", "Beginning Cash and Cash Equivalents", FormatCurrency(statement.BeginningCashBalance)]);
                rows.Add(["Summary", "NET_CHANGE", "Net Increase or Decrease in Cash", FormatCurrency(statement.NetIncreaseDecreaseInCash)]);
                rows.Add(["Summary", "ENDING_CASH", "Ending Cash and Cash Equivalents", FormatCurrency(statement.EndingCashBalance)]);
                rows.Add(["Summary", "RECONCILIATION_DIFFERENCE", "Reconciliation Difference", FormatCurrency(statement.ReconciliationDifference)]);
                break;
            }
            case "cash-movement-report":
            case "unmapped-cash-flow-items":
            {
                var movements = await _cashFlowReportService.GetCashMovementsAsync(
                    startDate,
                    endDate,
                    mapped: catalogEntry.ReportKey == "unmapped-cash-flow-items" ? false : null);
                rows.Add(["Date", "Journal Number", "Source Module", "Source Transaction", "Cash Account", "Offset Account", "Description", "Cash Inflow", "Cash Outflow", "Section", "Category", "Mapping Status"]);
                rows.AddRange(movements.Select(row => new[]
                {
                    FormatDate(row.JournalDate),
                    row.JournalNumber,
                    row.SourceModule.ToString(),
                    row.SourceTransactionType.ToString(),
                    $"{row.CashAccountCode} - {row.CashAccountName}",
                    $"{row.OffsetAccountCode} {row.OffsetAccountName}".Trim(),
                    row.Description,
                    FormatCurrency(row.CashInflow),
                    FormatCurrency(row.CashOutflow),
                    row.CashFlowSection.ToString(),
                    row.CashFlowCategoryName,
                    row.MappingStatus
                }));
                break;
            }
            case "usali-operating-statement":
            {
                var balances = await _accountingReportService.GetAccountBalancesAsync(startDate, endDate);
                rows.Add(["USALI Line", "Amount"]);
                foreach (var group in balances.GroupBy(row => row.UsaliReportLineName ?? "Unmapped").OrderBy(group => group.Key))
                {
                    rows.Add([group.Key, FormatCurrency(group.Sum(row => row.NetAmount))]);
                }
                break;
            }
            case "ar-aging":
            {
                rows.Add(new[] { "AR Account", "Invoice Number", "Invoice Date", "Due Date", "Original Amount", "Payments Applied", "Credit Memos", "Debit Memos", "Balance", "Aging Bucket", "Days Overdue", "Last Payment Date", "Status" });
                var aging = await _arCollectionReportService.GetAgingReportAsync(endDate);
                rows.AddRange(aging.Rows.Select(row => new[]
                {
                    row.AccountName,
                    row.InvoiceNumber,
                    FormatDate(row.InvoiceDate),
                    FormatDate(row.DueDate),
                    FormatCurrency(row.OriginalAmount),
                    FormatCurrency(row.PaymentsApplied),
                    FormatCurrency(row.CreditMemos),
                    FormatCurrency(row.DebitMemos),
                    FormatCurrency(row.Balance),
                    row.AgingBucket,
                    row.DaysOverdue.ToString(CultureInfo.InvariantCulture),
                    row.LastPaymentDate is null ? string.Empty : FormatDate(row.LastPaymentDate.Value),
                    row.Status.ToString()
                }));
                rows.Add(Array.Empty<string>());
                rows.Add(new[] { "Summary", "Current", FormatCurrency(aging.Summary.Current) });
                rows.Add(new[] { "Summary", "1-30 days", FormatCurrency(aging.Summary.Days1To30) });
                rows.Add(new[] { "Summary", "31-60 days", FormatCurrency(aging.Summary.Days31To60) });
                rows.Add(new[] { "Summary", "61-90 days", FormatCurrency(aging.Summary.Days61To90) });
                rows.Add(new[] { "Summary", "Over 90 days", FormatCurrency(aging.Summary.Over90) });
                rows.Add(new[] { "Summary", "Total", FormatCurrency(aging.Summary.Total) });
                break;
            }
            case "ar-collection-daily":
            case "ar-collection-weekly":
            case "ar-collection-monthly":
            case "ar-collection-custom":
            {
                var reportStart = startDate;
                var reportEnd = endDate;
                if (catalogEntry.ReportKey == "ar-collection-daily")
                {
                    reportStart = endDate;
                    reportEnd = endDate;
                }
                else if (catalogEntry.ReportKey == "ar-collection-weekly" && dateRangeStart is null)
                {
                    reportStart = endDate.AddDays(-6);
                }
                else if (catalogEntry.ReportKey == "ar-collection-monthly" && dateRangeStart is null)
                {
                    reportStart = new DateTime(endDate.Year, endDate.Month, 1);
                }

                var collection = await _arCollectionReportService.GetCollectionReportAsync(reportStart, reportEnd);
                rows.Add(new[] { "Metric", "Amount" });
                rows.Add(new[] { "Opening AR", FormatCurrency(collection.OpeningAR) });
                rows.Add(new[] { "Billings", FormatCurrency(collection.Billings) });
                rows.Add(new[] { "Debit Memos", FormatCurrency(collection.DebitMemos) });
                rows.Add(new[] { "Collections", FormatCurrency(collection.Collections) });
                rows.Add(new[] { "Credit Memos", FormatCurrency(collection.CreditMemos) });
                rows.Add(new[] { "Adjustments", FormatCurrency(collection.Adjustments) });
                rows.Add(new[] { "Ending AR", FormatCurrency(collection.EndingAR) });
                rows.Add(new[] { "Overdue AR", FormatCurrency(collection.OverdueAR) });
                rows.Add(new[] { "Collection Rate", FormatPercentage(collection.CollectionRate) });
                rows.Add(new[] { "DSO", collection.DaysSalesOutstanding?.ToString("0.00", CultureInfo.InvariantCulture) ?? string.Empty });
                rows.Add(Array.Empty<string>());
                rows.Add(new[] { "Top Collected Accounts", "Amount", "Last Payment Date" });
                rows.AddRange(collection.TopCollectedAccounts.Select(row => new[]
                {
                    row.AccountName,
                    FormatCurrency(row.Amount),
                    row.LastPaymentDate is null ? string.Empty : FormatDate(row.LastPaymentDate.Value)
                }));
                rows.Add(Array.Empty<string>());
                rows.Add(new[] { "Top Overdue Accounts", "Amount", "Last Payment Date" });
                rows.AddRange(collection.TopOverdueAccounts.Select(row => new[]
                {
                    row.AccountName,
                    FormatCurrency(row.Amount),
                    row.LastPaymentDate is null ? string.Empty : FormatDate(row.LastPaymentDate.Value)
                }));
                break;
            }
            case "ap-aging":
            {
                rows.Add(["Supplier", "Current", "1-30", "31-60", "61-90", "Over 90", "Total"]);
                var today = endDate;
                var invoices = await _context.APInvoices
                    .AsNoTracking()
                    .Where(invoice => invoice.Balance > 0 && invoice.Status != APInvoiceStatus.Cancelled && invoice.Status != APInvoiceStatus.Voided)
                    .Select(invoice => new
                    {
                        Supplier = invoice.Supplier != null ? invoice.Supplier.SupplierName : "Unassigned",
                        invoice.DueDate,
                        invoice.Balance
                    })
                    .ToListAsync();
                foreach (var group in invoices.GroupBy(invoice => invoice.Supplier).OrderBy(group => group.Key))
                {
                    decimal Bucket(int from, int? to)
                    {
                        return group.Where(invoice =>
                        {
                            var days = (today - invoice.DueDate.Date).Days;
                            return days >= from && (to is null || days <= to.Value);
                        }).Sum(invoice => invoice.Balance);
                    }

                    var current = group.Where(invoice => invoice.DueDate.Date >= today).Sum(invoice => invoice.Balance);
                    var oneToThirty = Bucket(1, 30);
                    var thirtyOneToSixty = Bucket(31, 60);
                    var sixtyOneToNinety = Bucket(61, 90);
                    var overNinety = Bucket(91, null);
                    rows.Add([group.Key, FormatCurrency(current), FormatCurrency(oneToThirty), FormatCurrency(thirtyOneToSixty), FormatCurrency(sixtyOneToNinety), FormatCurrency(overNinety), FormatCurrency(group.Sum(invoice => invoice.Balance))]);
                }
                break;
            }
            case "supplier-ledger":
            {
                rows.Add(["Supplier", "Invoice Date", "Invoice Number", "Status", "Original Amount", "Paid", "Balance"]);
                var invoices = await _context.APInvoices
                    .AsNoTracking()
                    .Where(invoice => invoice.InvoiceDate >= startDate && invoice.InvoiceDate <= endDate)
                    .OrderBy(invoice => invoice.Supplier!.SupplierName)
                    .ThenBy(invoice => invoice.InvoiceDate)
                    .Select(invoice => new
                    {
                        Supplier = invoice.Supplier != null ? invoice.Supplier.SupplierName : "Unassigned",
                        invoice.InvoiceDate,
                        invoice.InvoiceNumber,
                        invoice.Status,
                        invoice.TotalAmount,
                        invoice.AmountPaid,
                        invoice.Balance
                    })
                    .ToListAsync();
                rows.AddRange(invoices.Select(invoice => new[]
                {
                    invoice.Supplier,
                    FormatDate(invoice.InvoiceDate),
                    invoice.InvoiceNumber,
                    invoice.Status.ToString(),
                    FormatCurrency(invoice.TotalAmount),
                    FormatCurrency(invoice.AmountPaid),
                    FormatCurrency(invoice.Balance)
                }));
                break;
            }
            case "payment-voucher-register":
            {
                rows.Add(["Voucher Date", "Voucher Number", "Supplier", "Status", "Method", "Amount", "Net Payment"]);
                var vouchers = await _context.PaymentVouchers
                    .AsNoTracking()
                    .Where(voucher => voucher.VoucherDate >= startDate && voucher.VoucherDate <= endDate)
                    .OrderBy(voucher => voucher.VoucherDate)
                    .Select(voucher => new
                    {
                        voucher.VoucherDate,
                        voucher.VoucherNumber,
                        Supplier = voucher.Supplier != null ? voucher.Supplier.SupplierName : "Unassigned",
                        voucher.Status,
                        voucher.PaymentMethod,
                        voucher.Amount,
                        voucher.NetPaymentAmount
                    })
                    .ToListAsync();
                rows.AddRange(vouchers.Select(voucher => new[]
                {
                    FormatDate(voucher.VoucherDate),
                    voucher.VoucherNumber,
                    voucher.Supplier,
                    voucher.Status.ToString(),
                    voucher.PaymentMethod.ToString(),
                    FormatCurrency(voucher.Amount),
                    FormatCurrency(voucher.NetPaymentAmount)
                }));
                break;
            }
            case "stock-on-hand":
            case "low-stock":
            {
                rows.Add(["Item Code", "Item Name", "Category", "Unit", "Current Stock", "Reorder Level", "Standard Cost", "Stock Value"]);
                var query = _context.InventoryItems.AsNoTracking().Where(item => item.IsActive);
                if (catalogEntry.ReportKey == "low-stock")
                {
                    query = query.Where(item => item.CurrentStock <= item.ReorderLevel);
                }

                var items = await query
                    .OrderBy(item => item.ItemName)
                    .Select(item => new
                    {
                        item.ItemCode,
                        item.ItemName,
                        Category = item.InventoryCategory != null ? item.InventoryCategory.Name : "Uncategorized",
                        item.UnitOfMeasure,
                        item.CurrentStock,
                        item.ReorderLevel,
                        item.StandardCost
                    })
                    .ToListAsync();
                rows.AddRange(items.Select(item => new[]
                {
                    item.ItemCode,
                    item.ItemName,
                    item.Category,
                    item.UnitOfMeasure,
                    item.CurrentStock.ToString("N2", CultureInfo.CurrentCulture),
                    item.ReorderLevel.ToString("N2", CultureInfo.CurrentCulture),
                    FormatCurrency(item.StandardCost),
                    FormatCurrency(item.CurrentStock * item.StandardCost)
                }));
                break;
            }
            case "stock-movement":
            {
                rows.Add(["Movement Date", "Item", "Movement Type", "Quantity", "Unit Cost", "Reference", "Department", "Remarks"]);
                var movements = await _context.StockMovements
                    .AsNoTracking()
                    .Where(movement => movement.MovementDate >= startDate && movement.MovementDate <= endDate.AddDays(1))
                    .OrderByDescending(movement => movement.MovementDate)
                    .Select(movement => new
                    {
                        movement.MovementDate,
                        Item = movement.InventoryItem != null ? movement.InventoryItem.ItemName : "Unassigned",
                        movement.MovementType,
                        movement.Quantity,
                        movement.UnitCost,
                        movement.ReferenceType,
                        movement.ReferenceId,
                        Department = movement.Department != null ? movement.Department.Name : string.Empty,
                        movement.Remarks
                    })
                    .ToListAsync();
                rows.AddRange(movements.Select(movement => new[]
                {
                    FormatDate(movement.MovementDate),
                    movement.Item,
                    movement.MovementType.ToString(),
                    movement.Quantity.ToString("N2", CultureInfo.CurrentCulture),
                    FormatCurrency(movement.UnitCost),
                    $"{movement.ReferenceType} {movement.ReferenceId}".Trim(),
                    movement.Department,
                    movement.Remarks ?? string.Empty
                }));
                break;
            }
            case "kpi-scorecard":
            {
                rows.Add(["KPI", "Category", "Actual", "Target", "Variance", "Variance %", "Status", "Formula"]);
                var kpis = await _executiveKPIService.GetScorecardAsync(startDate, endDate);
                rows.AddRange(kpis.Select(kpi => new[]
                {
                    kpi.KPIName,
                    kpi.Category.ToString(),
                    kpi.ActualValue.ToString("N2", CultureInfo.CurrentCulture),
                    kpi.TargetValue?.ToString("N2", CultureInfo.CurrentCulture) ?? string.Empty,
                    kpi.Variance?.ToString("N2", CultureInfo.CurrentCulture) ?? string.Empty,
                    FormatPercentage(kpi.VariancePercentage),
                    kpi.Status.ToString(),
                    kpi.FormulaDescription
                }));
                break;
            }
            case "department-performance":
            {
                rows.Add(["Department", "Revenue", "Cost of Sales", "Payroll Cost", "Other Expenses", "Profit", "Profit Margin", "Labor Cost %", "Budget", "Variance"]);
                var departments = await _departmentPerformanceService.GetDepartmentPerformanceAsync(startDate, endDate);
                rows.AddRange(departments.Select(row => new[]
                {
                    row.DepartmentName,
                    FormatCurrency(row.Revenue),
                    FormatCurrency(row.CostOfSales),
                    FormatCurrency(row.PayrollCost),
                    FormatCurrency(row.OtherExpenses),
                    FormatCurrency(row.DepartmentProfit),
                    FormatPercentage(row.DepartmentProfitMargin),
                    FormatPercentage(row.LaborCostPercentage),
                    row.BudgetAmount is null ? string.Empty : FormatCurrency(row.BudgetAmount.Value),
                    row.VarianceAmount is null ? string.Empty : FormatCurrency(row.VarianceAmount.Value)
                }));
                break;
            }
            case "executive-alerts":
            {
                rows.Add(["Alert Date", "Severity", "Module", "Title", "Message", "Recommended Action", "Resolved"]);
                var alerts = await _context.ExecutiveAlerts
                    .AsNoTracking()
                    .Where(alert => alert.AlertDate >= startDate && alert.AlertDate <= endDate)
                    .OrderByDescending(alert => alert.Severity)
                    .ThenByDescending(alert => alert.AlertDate)
                    .ToListAsync();
                rows.AddRange(alerts.Select(alert => new[]
                {
                    FormatDate(alert.AlertDate),
                    alert.Severity.ToString(),
                    alert.Module,
                    alert.Title,
                    alert.Message,
                    alert.RecommendedAction ?? string.Empty,
                    alert.IsResolved ? "Yes" : "No"
                }));
                break;
            }
            default:
                return null;
        }

        if (rows.Count == 1)
        {
            rows.Add(["No records found for the selected date range."]);
        }

        var fileName = BuildSafeFileName(catalogEntry.ReportName, startDate, endDate, "csv");
        return new ReportCsvResult(fileName, "text/csv; charset=utf-8", ExportToCsv(catalogEntry.ReportName, startDate, endDate, rows));
    }

    public byte[] ExportToCsv(string reportTitle, DateTime? dateRangeStart, DateTime? dateRangeEnd, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Escape(reportTitle));
        if (dateRangeStart is not null || dateRangeEnd is not null)
        {
            builder.AppendLine(Escape($"Date Range: {dateRangeStart?.ToString("yyyy-MM-dd") ?? "-"} to {dateRangeEnd?.ToString("yyyy-MM-dd") ?? "-"}"));
        }

        builder.AppendLine();
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(Escape)));
        }

        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetBytes(builder.ToString());
    }

    public async Task LogExportAsync(
        ReportCatalogEntry catalogEntry,
        ReportExportType exportType,
        string? exportedBy,
        DateTime? dateRangeStart,
        DateTime? dateRangeEnd,
        string? fileName,
        string? notes = null)
    {
        _context.ReportExportLogs.Add(new ReportExportLog
        {
            ReportKey = catalogEntry.ReportKey,
            ReportName = catalogEntry.ReportName,
            ReportCategory = catalogEntry.ReportCategory,
            ExportType = exportType,
            ExportedBy = exportedBy,
            ExportedAt = DateTime.Now,
            DateRangeStart = dateRangeStart,
            DateRangeEnd = dateRangeEnd,
            FileName = fileName,
            Notes = notes
        });

        _context.SavedReportRuns.Add(new SavedReportRun
        {
            ReportKey = catalogEntry.ReportKey,
            ReportName = catalogEntry.ReportName,
            ReportCategory = catalogEntry.ReportCategory,
            DateRangeStart = dateRangeStart,
            DateRangeEnd = dateRangeEnd,
            ParametersJson = $$"""{"exportType":"{{exportType}}"}""",
            RunBy = exportedBy,
            RunAt = DateTime.Now,
            Notes = notes ?? "Report export run."
        });

        await _context.SaveChangesAsync();

        if (IsHighValueExport(catalogEntry.ReportKey))
        {
            await _auditLogService.LogAsync(
                AuditActionType.Export,
                "Reports",
                "ReportExport",
                catalogEntry.ReportKey,
                null,
                new
                {
                    catalogEntry.ReportName,
                    ExportType = exportType.ToString(),
                    DateRangeStart = dateRangeStart,
                    DateRangeEnd = dateRangeEnd,
                    FileName = fileName
                },
                exportedBy);
        }
    }

    public string BuildSafeFileName(string reportName, DateTime? startDate, DateTime? endDate, string extension)
    {
        var safeName = new string(reportName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());
        while (safeName.Contains("--", StringComparison.Ordinal))
        {
            safeName = safeName.Replace("--", "-", StringComparison.Ordinal);
        }

        var range = startDate is null && endDate is null
            ? DateTime.Now.ToString("yyyyMMdd-HHmm")
            : $"{startDate?.ToString("yyyyMMdd") ?? "start"}-{endDate?.ToString("yyyyMMdd") ?? "end"}";
        return $"{safeName.Trim('-').ToLowerInvariant()}-{range}.{extension.TrimStart('.')}";
    }

    public string FormatCurrency(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    public string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public string FormatPercentage(decimal? percentage) => percentage is null ? string.Empty : percentage.Value.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r')
            ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\""
            : value;
    }

    private static bool IsHighValueExport(string reportKey)
    {
        return reportKey is
            "profit-loss" or
            "trial-balance" or
            "balance-sheet" or
            "statement-of-cash-flows" or
            "usali-operating-statement" or
            "vat-output-summary" or
            "vat-input-summary" or
            "vat-payable-summary" or
            "ar-aging" or
            "ap-aging" or
            "owner-report-package";
    }
}

public record ReportCsvResult(string FileName, string ContentType, byte[] Content);
