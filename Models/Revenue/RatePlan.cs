using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Revenue;

public class RatePlan
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IncludesBreakfast { get; set; }

    public bool IsCorporateRate { get; set; }

    public string? CancellationPolicy { get; set; }

    public string? DepositPolicy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<RoomTypeRate> RoomTypeRates { get; set; } = new List<RoomTypeRate>();

    public ICollection<SeasonalRate> SeasonalRates { get; set; } = new List<SeasonalRate>();

    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
