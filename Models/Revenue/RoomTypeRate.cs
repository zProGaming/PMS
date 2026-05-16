using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Revenue;

public class RoomTypeRate
{
    public int Id { get; set; }

    public int RatePlanId { get; set; }

    public RatePlan? RatePlan { get; set; }

    public int RoomTypeId { get; set; }

    public RoomType? RoomType { get; set; }

    public decimal BaseRate { get; set; }

    public decimal ExtraAdultRate { get; set; }

    public decimal ExtraChildRate { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.Today;

    public DateTime EffectiveTo { get; set; } = DateTime.Today.AddYears(1);

    public bool IsActive { get; set; } = true;
}
