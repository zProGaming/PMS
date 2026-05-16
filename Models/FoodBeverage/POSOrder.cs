using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.FoodBeverage;

public class POSOrder
{
    public int Id { get; set; }

    public int OutletId { get; set; }

    public Outlet? Outlet { get; set; }

    public int? DiningTableId { get; set; }

    public DiningTable? DiningTable { get; set; }

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public int? GuestId { get; set; }

    public Guest? Guest { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public POSOrderType OrderType { get; set; }

    public POSOrderStatus OrderStatus { get; set; } = POSOrderStatus.Open;

    public DateTime OrderDate { get; set; } = DateTime.Now;

    public decimal SubTotal { get; set; }

    public decimal ServiceCharge { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public POSPaymentStatus PaymentStatus { get; set; } = POSPaymentStatus.Unpaid;

    public string? Notes { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? ClosedAt { get; set; }

    public ICollection<POSOrderItem> Items { get; set; } = new List<POSOrderItem>();
}
