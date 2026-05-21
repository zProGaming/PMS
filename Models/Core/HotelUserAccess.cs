using Microsoft.AspNetCore.Identity;

namespace Vantage.PMS.Models.Core;

public class HotelUserAccess
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int HotelId { get; set; }

    public string? RoleName { get; set; }

    public bool IsDefaultCompany { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? CreatedBy { get; set; }

    public IdentityUser? User { get; set; }

    public Hotel? Hotel { get; set; }
}
