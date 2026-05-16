using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Accounting;
using Vantage.PMS.Models.Inventory;
using Vantage.PMS.Models.Labor;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Executive;

public class CostControlModel(ApplicationDbContext context, ExecutiveKPIService kpiService) : PageModel
{
    public ExecutiveSummaryMetrics Summary { get; private set; } = new();
    public int DepartmentsOverBudget { get; private set; }
    public int LowStockItems { get; private set; }
    public int OutOfStockItems { get; private set; }
    public int ExpiringItems { get; private set; }
    public int PurchaseOrdersPendingApproval { get; private set; }
    public int APInvoicesPendingApproval { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        Summary = await kpiService.GetSummaryAsync(new DateTime(today.Year, today.Month, 1), today);
        LowStockItems = await context.InventoryItems.AsNoTracking().CountAsync(item => item.IsActive && item.CurrentStock <= item.ReorderLevel);
        OutOfStockItems = await context.InventoryItems.AsNoTracking().CountAsync(item => item.IsActive && item.CurrentStock <= 0);
        ExpiringItems = await context.InventoryItems.AsNoTracking().CountAsync(item => item.IsActive && item.IsPerishable && item.ExpiryDate != null && item.ExpiryDate >= today && item.ExpiryDate <= today.AddDays(14));
        PurchaseOrdersPendingApproval = await context.PurchaseOrders.AsNoTracking().CountAsync(order => order.Status == PurchaseOrderStatus.ForApproval || order.Status == PurchaseOrderStatus.Draft);
        APInvoicesPendingApproval = await context.APInvoices.AsNoTracking().CountAsync(invoice => invoice.Status == APInvoiceStatus.ForApproval || invoice.Status == APInvoiceStatus.Draft);

        var laborActuals = await context.PayrollCostEntries
            .AsNoTracking()
            .Where(entry => entry.DepartmentId != null && entry.PayrollPeriod != null && entry.PayrollPeriod.StartDate.Month == today.Month && entry.PayrollPeriod.StartDate.Year == today.Year && entry.PayrollPeriod.Status != PayrollPeriodStatus.Cancelled)
            .GroupBy(entry => entry.DepartmentId)
            .Select(group => new { DepartmentId = group.Key, Cost = group.Sum(entry => entry.EmployerCost > 0 ? entry.EmployerCost : entry.GrossPay) })
            .ToListAsync();
        var budgets = await context.DepartmentLaborBudgets.AsNoTracking().Where(budget => budget.Month == today.Month && budget.Year == today.Year && budget.BudgetedLaborCost > 0).ToListAsync();
        DepartmentsOverBudget = budgets.Count(budget => laborActuals.Where(actual => actual.DepartmentId == budget.DepartmentId).Sum(actual => actual.Cost) > budget.BudgetedLaborCost);
    }
}
