namespace Vantage.PMS.Models.Inventory;

public class InventoryCategory
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
}
