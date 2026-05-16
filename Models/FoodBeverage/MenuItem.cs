using Vantage.PMS.Models.Inventory;

namespace Vantage.PMS.Models.FoodBeverage;

public class MenuItem
{
    public int Id { get; set; }

    public int MenuCategoryId { get; set; }

    public MenuCategory? MenuCategory { get; set; }

    public int? KitchenStationId { get; set; }

    public KitchenStation? KitchenStation { get; set; }

    public int? InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsAvailable { get; set; } = true;

    public bool IsTaxable { get; set; } = true;

    public bool IsServiceChargeable { get; set; } = true;

    public ICollection<POSOrderItem> OrderItems { get; set; } = new List<POSOrderItem>();
}
