using Vantage.PMS.Models.Revenue;

namespace Vantage.PMS.Models.Booking;

public class BookingEngineSetting
{
    public int Id { get; set; }

    public string HotelName { get; set; } = string.Empty;

    public string BookingEngineTitle { get; set; } = "Book Your Stay";

    public string? WelcomeMessage { get; set; }

    public string? ContactEmail { get; set; }

    public string? ContactNumber { get; set; }

    public int? DefaultRatePlanId { get; set; }

    public RatePlan? DefaultRatePlan { get; set; }

    public bool RequireDeposit { get; set; }

    public decimal DepositPercentage { get; set; }

    public bool AllowPromoCodes { get; set; } = true;

    public bool AllowSpecialRequests { get; set; } = true;

    public bool IsBookingEngineEnabled { get; set; } = true;

    public string? TermsAndConditions { get; set; }

    public string? PrivacyPolicy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
