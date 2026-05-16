namespace Vantage.PMS.Models.Inventory;

public class PurchaseOrder
{
    public int Id { get; set; }

    public string PONumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }

    public Supplier? Supplier { get; set; }

    public int? PurchaseRequestId { get; set; }

    public PurchaseRequest? PurchaseRequest { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.Today;

    public DateTime? ExpectedDeliveryDate { get; set; }

    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string PreparedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
