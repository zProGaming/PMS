using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Models.FrontOffice;

public class Room
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public Property? Property { get; set; }

    public int RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public string? Floor { get; set; }

    public RoomStatus Status { get; set; } = RoomStatus.VacantClean;

    public bool IsActive { get; set; } = true;
}
