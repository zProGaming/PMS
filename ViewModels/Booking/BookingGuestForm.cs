namespace Vantage.PMS.ViewModels.Booking;

public class BookingGuestForm
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public string? SpecialRequests { get; set; }
}
