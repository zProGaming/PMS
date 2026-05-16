using Vantage.PMS.Models.Core;
using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Finance;

public class Folio
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public Property? Property { get; set; }

    public int ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int GuestId { get; set; }

    public Guest? Guest { get; set; }

    public string FolioNumber { get; set; } = string.Empty;

    public FolioStatus Status { get; set; } = FolioStatus.Open;

    public DateTime OpenedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAtUtc { get; set; }

    public ICollection<FolioItem> Items { get; set; } = new List<FolioItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public decimal TotalCharges => Items
        .Where(item => !item.IsVoided)
        .Sum(item => item.Amount);

    public decimal TotalPayments => Payments
        .Where(payment => payment.Status == PaymentStatus.Completed)
        .Sum(payment => payment.Amount);

    public decimal Balance => TotalCharges - TotalPayments;
}
