namespace Vantage.PMS.Models.Banquet;

public class BanquetPackage
{
    public int Id { get; set; }

    public string PackageName { get; set; } = string.Empty;

    public decimal PricePerPax { get; set; }

    public int MinimumPax { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<BanquetEvent> BanquetEvents { get; set; } = new List<BanquetEvent>();
}
