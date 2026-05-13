namespace Vantage.PMS.Models.Finance;

public class Payment
{
    public int Id { get; set; }

    public int FolioId { get; set; }

    public Folio? Folio { get; set; }

    public DateTime PostedAtUtc { get; set; } = DateTime.UtcNow;

    public string Method { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
}
