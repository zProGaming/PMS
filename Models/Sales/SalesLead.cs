namespace Vantage.PMS.Models.Sales;

public class SalesLead
{
    public int Id { get; set; }

    public int? SalesAccountId { get; set; }

    public SalesAccount? SalesAccount { get; set; }

    public string LeadName { get; set; } = string.Empty;

    public string? LeadSource { get; set; }

    public decimal EstimatedValue { get; set; }

    public SalesLeadStatus Status { get; set; } = SalesLeadStatus.New;

    public DateTime? ExpectedCloseDate { get; set; }

    public string? Notes { get; set; }

    public string? AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<SalesActivity> SalesActivities { get; set; } = new List<SalesActivity>();
}
