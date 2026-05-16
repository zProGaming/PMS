namespace Vantage.PMS.ViewModels.Booking;

public class BookingStayInput
{
    public DateTime CheckInDate { get; set; } = DateTime.Today.AddDays(1);

    public DateTime CheckOutDate { get; set; } = DateTime.Today.AddDays(2);

    public int AdultCount { get; set; } = 1;

    public int ChildCount { get; set; }

    public string? PromoCode { get; set; }

    public int RoomTypeId { get; set; }

    public int? RatePlanId { get; set; }
}
