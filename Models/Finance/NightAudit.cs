namespace Vantage.PMS.Models.Finance;

public class NightAudit
{
    public int Id { get; set; }

    public DateTime BusinessDate { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public NightAuditStatus Status { get; set; } = NightAuditStatus.Started;

    public string CompletedBy { get; set; } = string.Empty;

    public string? Notes { get; set; }
}
