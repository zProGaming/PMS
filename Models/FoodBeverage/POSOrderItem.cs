namespace Vantage.PMS.Models.FoodBeverage;

public class POSOrderItem
{
    public int Id { get; set; }

    public int POSOrderId { get; set; }

    public POSOrder? POSOrder { get; set; }

    public int MenuItemId { get; set; }

    public MenuItem? MenuItem { get; set; }

    public decimal Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal LineTotal { get; set; }

    public string? Notes { get; set; }

    public POSOrderItemStatus ItemStatus { get; set; } = POSOrderItemStatus.New;

    public bool IsVoided { get; set; }

    public DateTime? SentToKitchenAt { get; set; }

    public DateTime? PreparingAt { get; set; }

    public DateTime? ReadyAt { get; set; }

    public DateTime? ServedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
}
