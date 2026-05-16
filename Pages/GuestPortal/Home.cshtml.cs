using Microsoft.AspNetCore.Mvc.RazorPages;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.GuestPortal;

public class HomeModel(GuestPortalService guestPortalService) : PageModel
{
    private readonly GuestPortalService _guestPortalService = guestPortalService;

    public GuestPortalAccess? Access { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string CheckInDate { get; set; } = "-";
    public string CheckOutDate { get; set; } = "-";
    public string RoomType { get; set; } = "-";
    public string RoomNumber { get; set; } = "-";
    public string Status { get; set; } = "-";

    public async Task OnGetAsync(string token)
    {
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
        CheckInDate = (reservation?.ArrivalDate ?? booking?.CheckInDate)?.ToString("yyyy-MM-dd") ?? "-";
        CheckOutDate = (reservation?.DepartureDate ?? booking?.CheckOutDate)?.ToString("yyyy-MM-dd") ?? "-";
        RoomType = reservation?.RoomType?.Name ?? booking?.RoomType?.Name ?? "-";
        RoomNumber = reservation?.Room?.RoomNumber ?? "Not assigned";
        Status = reservation?.Status.ToString() ?? booking?.BookingStatus.ToString() ?? "-";
    }
}
