namespace Vantage.PMS.Models.Inventory;

public class ReceivingRecordItem
{
    public int Id { get; set; }

    public int ReceivingRecordId { get; set; }

    public ReceivingRecord? ReceivingRecord { get; set; }

    public int InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    public decimal QuantityReceived { get; set; }

    public decimal UnitCost { get; set; }

    public decimal Amount { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public string? Notes { get; set; }
}
