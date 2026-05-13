namespace Vantage.PMS.Models.Finance;

public class FolioItem
{
    public int Id { get; set; }

    public int FolioId { get; set; }

    public Folio? Folio { get; set; }

    public DateTime PostedAtUtc { get; set; } = DateTime.UtcNow;

    public string Description { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }

    public decimal Amount { get; set; }

    public bool IsVoided { get; set; }
}
