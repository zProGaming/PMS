using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Revenue;

public class RateRestriction
{
    public int Id { get; set; }

    public int? RatePlanId { get; set; }

    public RatePlan? RatePlan { get; set; }

    public int? RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public DateTime RestrictionDate { get; set; } = DateTime.Today;

    public int MinimumLengthOfStay { get; set; } = 1;

    public int? MaximumLengthOfStay { get; set; }

    public bool ClosedToArrival { get; set; }

    public bool ClosedToDeparture { get; set; }

    public bool StopSell { get; set; }

    public string? Notes { get; set; }
}
