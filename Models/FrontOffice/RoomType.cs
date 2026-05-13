using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Models.FrontOffice;

public class RoomType
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public Property? Property { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int MaxOccupancy { get; set; }

    public decimal BaseRate { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
