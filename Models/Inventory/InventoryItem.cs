namespace Vantage.PMS.Models.Inventory;

public class InventoryItem
{
    public int Id { get; set; }

    public int InventoryCategoryId { get; set; }

    public InventoryCategory? InventoryCategory { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal ReorderLevel { get; set; }

    public decimal ParStockLevel { get; set; }

    public decimal StandardCost { get; set; }

    public decimal CurrentStock { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsPerishable { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
