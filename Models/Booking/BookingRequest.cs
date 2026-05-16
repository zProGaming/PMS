using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Models.Booking;

public class BookingRequest
{
    public int Id { get; set; }

    public string BookingReference { get; set; } = string.Empty;

    public string GuestFirstName { get; set; } = string.Empty;

    public string GuestLastName { get; set; } = string.Empty;

    public string GuestEmail { get; set; } = string.Empty;

    public string? GuestPhone { get; set; }

    public string? GuestAddress { get; set; }

    public DateTime CheckInDate { get; set; }

    public DateTime CheckOutDate { get; set; }

    public int AdultCount { get; set; }

    public int ChildCount { get; set; }

    public int RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public int? RatePlanId { get; set; }

    public RatePlan? RatePlan { get; set; }

    public int? PromotionCodeId { get; set; }

    public PromotionCode? PromotionCode { get; set; }

    public decimal RoomRate { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TotalRoomAmount { get; set; }

    public bool DepositRequired { get; set; }

    public decimal DepositAmount { get; set; }

    public string? SpecialRequests { get; set; }

    public BookingRequestStatus BookingStatus { get; set; } = BookingRequestStatus.Pending;

    public int? ReservationId { get; set; }

    public Reservation? Reservation { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<BookingRequestAddOn> AddOns { get; set; } = new List<BookingRequestAddOn>();
}
