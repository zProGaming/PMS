namespace Vantage.PMS.Models.Core;

public class BusinessDateSetting
{
    public int Id { get; set; }

    public DateTime CurrentBusinessDate { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
