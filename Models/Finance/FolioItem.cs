namespace Vantage.PMS.Models.Finance;

public class FolioItem
{
    public int Id { get; set; }

    public int FolioId { get; set; }

    public Folio? Folio { get; set; }

    public int? ChargeCodeId { get; set; }

    public ChargeCode? ChargeCodeDefinition { get; set; }

    public string Description { get; set; } = string.Empty;

    public string ChargeCode { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public DateTime PostingDate { get; set; } = DateTime.Now;

    // Set only for automated night-audit charges. It provides a durable,
    // database-enforced idempotency key without restricting normal same-day
    // restaurant, banquet, or manual folio charges.
    public DateTime? NightAuditBusinessDate { get; set; }

    public string? NightAuditChargeCode { get; set; }

    public string PostedBy { get; set; } = string.Empty;

    public bool IsVoided { get; set; }

    public bool IsLocked { get; set; }
}
