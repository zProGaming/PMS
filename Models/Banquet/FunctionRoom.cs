namespace Vantage.PMS.Models.Banquet;

public class FunctionRoom
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }

    public int Capacity { get; set; }

    public decimal Rate { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public ICollection<BanquetEvent> BanquetEvents { get; set; } = new List<BanquetEvent>();
}
