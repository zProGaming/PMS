namespace Vantage.PMS.Models.Finance;

public class Payment
{
    public int Id { get; set; }

    public int FolioId { get; set; }

    public Folio? Folio { get; set; }

    public decimal Amount { get; set; }

    public string PaymentMethod { get; set; } = string.Empty;

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public bool IsLocked { get; set; }

    public ICollection<CashierTransaction> CashierTransactions { get; set; } = new List<CashierTransaction>();
}
