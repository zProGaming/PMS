namespace Vantage.PMS.Models.Inventory;

public class StockAdjustmentItem
{
    public int Id { get; set; }

    public int StockAdjustmentId { get; set; }

    public StockAdjustment? StockAdjustment { get; set; }

    public int InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    public decimal SystemQuantity { get; set; }

    public decimal ActualQuantity { get; set; }

    public decimal VarianceQuantity { get; set; }

    public decimal UnitCost { get; set; }

    public decimal VarianceAmount { get; set; }

    public string? Notes { get; set; }
}
