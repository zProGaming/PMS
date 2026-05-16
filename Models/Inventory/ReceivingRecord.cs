namespace Vantage.PMS.Models.Inventory;

public class ReceivingRecord
{
    public int Id { get; set; }

    public string ReceivingNumber { get; set; } = string.Empty;

    public int? PurchaseOrderId { get; set; }

    public PurchaseOrder? PurchaseOrder { get; set; }

    public int? SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public DateTime ReceivedDate { get; set; } = DateTime.Today;

    public string ReceivedBy { get; set; } = string.Empty;

    public ReceivingStatus Status { get; set; } = ReceivingStatus.Draft;

    public string? Notes { get; set; }

    public ICollection<ReceivingRecordItem> Items { get; set; } = new List<ReceivingRecordItem>();
}
