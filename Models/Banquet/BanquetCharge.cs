namespace Vantage.PMS.Models.Banquet;

public class BanquetCharge
{
    public int Id { get; set; }

    public int BanquetEventId { get; set; }

    public BanquetEvent? BanquetEvent { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal Amount { get; set; }

    public DateTime ChargeDate { get; set; } = DateTime.Today;

    public bool IsVoided { get; set; }
}
