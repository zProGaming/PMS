using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Core;

public class Property
{
    public int Id { get; set; }

    public int HotelId { get; set; }

    public Hotel? Hotel { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? StateProvince { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public ICollection<Department> Departments { get; set; } = new List<Department>();

    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
