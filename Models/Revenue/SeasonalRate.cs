using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Revenue;

public class SeasonalRate
{
    public int Id { get; set; }

    public int RatePlanId { get; set; }

    public RatePlan? RatePlan { get; set; }

    public int RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public string SeasonName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(1);

    public decimal Rate { get; set; }

    public decimal ExtraAdultRate { get; set; }

    public decimal ExtraChildRate { get; set; }

    public bool IsActive { get; set; } = true;
}
