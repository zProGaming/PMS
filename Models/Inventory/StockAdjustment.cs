namespace Vantage.PMS.Models.Inventory;

public class StockAdjustment
{
    public int Id { get; set; }

    public string AdjustmentNumber { get; set; } = string.Empty;

    public DateTime AdjustmentDate { get; set; } = DateTime.Today;

    public StockAdjustmentStatus Status { get; set; } = StockAdjustmentStatus.Draft;

    public string? Reason { get; set; }

    public string PreparedBy { get; set; } = string.Empty;

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<StockAdjustmentItem> Items { get; set; } = new List<StockAdjustmentItem>();
}
