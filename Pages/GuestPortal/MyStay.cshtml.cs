using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortal;

public class MyStayModel(GuestPortalService guestPortalService) : PageModel
{
    private readonly GuestPortalService _guestPortalService = guestPortalService;

    public string Token { get; set; } = string.Empty;
    public GuestPortalAccess? Access { get; set; }
    public string GuestName { get; set; } = "-";
    public string Reference { get; set; } = "-";
    public string StayDates { get; set; } = "-";
    public string RoomType { get; set; } = "-";
    public string RoomNumber { get; set; } = "-";
    public string Status { get; set; } = "-";
    public string? SpecialRequests { get; set; }

    public async Task OnGetAsync(string token)
    {
        Token = token;
        Access = await _guestPortalService.GetAccessAsync(token);
        if (Access is null)
        {
            return;
        }

        var reservation = Access.Reservation;
        var booking = Access.BookingRequest;
        GuestName = reservation?.Guest is not null
            ? $"{reservation.Guest.FirstName} {reservation.Guest.LastName}"
            : $"{booking?.GuestFirstName} {booking?.GuestLastName}".Trim();
        Reference = reservation?.ConfirmationNumber ?? booking?.BookingReference ?? "-";
        StayDates = reservation is not null
            ? $"{reservation.ArrivalDate:yyyy-MM-dd} - {reservation.DepartureDate:yyyy-MM-dd}"
            : booking is not null ? $"{booking.CheckInDate:yyyy-MM-dd} - {booking.CheckOutDate:yyyy-MM-dd}" : "-";
        RoomType = reservation?.RoomType?.Name ?? booking?.RoomType?.Name ?? "-";
        RoomNumber = reservation?.Room?.RoomNumber ?? "Not assigned";
        Status = reservation?.Status.ToString() ?? booking?.BookingStatus.ToString() ?? "-";
        SpecialRequests = booking?.SpecialRequests;
    }
}
