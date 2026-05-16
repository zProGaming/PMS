namespace Vantage.PMS.Models.Sales;

public class SalesActivity
{
    public int Id { get; set; }

    public int? SalesLeadId { get; set; }

    public SalesLead? SalesLead { get; set; }

    public int? SalesAccountId { get; set; }

    public SalesAccount? SalesAccount { get; set; }

    public SalesActivityType ActivityType { get; set; } = SalesActivityType.Call;

    public DateTime ActivityDate { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    public DateTime? NextFollowUpDate { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
