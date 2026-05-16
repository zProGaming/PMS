using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Booking;

public class BookingEngineRoomContent
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string? LongDescription { get; set; }

    public string? ImageUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsVisible { get; set; } = true;
}
