namespace Vantage.PMS.Models.Banquet;

public class BanquetEventOrder
{
    public int Id { get; set; }

    public int BanquetEventId { get; set; }

    public BanquetEvent? BanquetEvent { get; set; }

    public DateTime BEODate { get; set; } = DateTime.Today;

    public string? MenuDetails { get; set; }

    public string? SetupInstructions { get; set; }

    public string? EquipmentRequirements { get; set; }

    public string? ServiceInstructions { get; set; }

    public string? KitchenInstructions { get; set; }

    public string? BillingInstructions { get; set; }

    public string? SpecialInstructions { get; set; }

    public string? PreparedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public BanquetEventOrderStatus Status { get; set; } = BanquetEventOrderStatus.Draft;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
