using Vantage.PMS.Models.FoodBeverage;

namespace Vantage.PMS.Services;

public static class POSOrderTotalsCalculator
{
    private const decimal ServiceChargeRate = 0.10m;
    private const decimal TaxRate = 0.12m;

    public static void Recalculate(POSOrder order)
    {
        var activeItems = order.Items
            .Where(item => !item.IsVoided &&
                           item.ItemStatus != POSOrderItemStatus.Cancelled &&
                           item.ItemStatus != POSOrderItemStatus.Voided)
            .ToList();

        order.SubTotal = activeItems.Sum(item => item.Quantity * item.UnitPrice);
        order.DiscountAmount = activeItems.Sum(item => item.DiscountAmount);
        order.ServiceCharge = activeItems
            .Where(item => item.MenuItem?.IsServiceChargeable == true)
            .Sum(item => item.LineTotal) * ServiceChargeRate;
        order.TaxAmount = activeItems
            .Where(item => item.MenuItem?.IsTaxable == true)
            .Sum(item => item.LineTotal) * TaxRate;
        order.TotalAmount = activeItems.Sum(item => item.LineTotal) + order.ServiceCharge + order.TaxAmount;
    }

    public static decimal CalculateLineTotal(decimal quantity, decimal unitPrice, decimal discountAmount)
    {
        return Math.Max(0, quantity * unitPrice - discountAmount);
    }
}
