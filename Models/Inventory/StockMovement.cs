using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Models.Inventory;

public class StockMovement
{
    public int Id { get; set; }

    public int InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    public DateTime MovementDate { get; set; } = DateTime.Now;

    public StockMovementType MovementType { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }

    public string? ReferenceType { get; set; }

    public int? ReferenceId { get; set; }

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string? Remarks { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}
