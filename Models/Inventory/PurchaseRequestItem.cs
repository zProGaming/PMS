namespace Vantage.PMS.Models.Inventory;

public class PurchaseRequestItem
{
    public int Id { get; set; }

    public int PurchaseRequestId { get; set; }

    public PurchaseRequest? PurchaseRequest { get; set; }

    public int InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    public decimal Quantity { get; set; }

    public decimal EstimatedUnitCost { get; set; }

    public decimal EstimatedAmount { get; set; }

    public string? Notes { get; set; }
}
