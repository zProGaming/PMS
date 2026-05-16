namespace Vantage.PMS.Models.Booking;

public class BookingRequestAddOn
{
    public int Id { get; set; }

    public int BookingRequestId { get; set; }

    public BookingRequest? BookingRequest { get; set; }

    public int BookingAddOnId { get; set; }

    public BookingAddOn? BookingAddOn { get; set; }

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }
}
