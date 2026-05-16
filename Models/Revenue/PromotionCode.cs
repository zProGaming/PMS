using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Revenue;

public class PromotionCode
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    public decimal DiscountValue { get; set; }

    public DateTime ValidFrom { get; set; } = DateTime.Today;

    public DateTime ValidTo { get; set; } = DateTime.Today.AddMonths(1);

    public bool IsActive { get; set; } = true;

    public int? UsageLimit { get; set; }

    public int TimesUsed { get; set; }

    public int? AppliesToRatePlanId { get; set; }

    public RatePlan? AppliesToRatePlan { get; set; }

    public int? AppliesToRoomTypeId { get; set; }

    public RoomType? AppliesToRoomType { get; set; }
}
