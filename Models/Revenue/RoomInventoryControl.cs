using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Revenue;

public class RoomInventoryControl
{
    public int Id { get; set; }

    public int RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public DateTime InventoryDate { get; set; } = DateTime.Today;

    public int TotalRooms { get; set; }

    public int RoomsToSell { get; set; }

    public int OverbookingLimit { get; set; }

    public bool StopSell { get; set; }

    public string? Notes { get; set; }
}
