namespace Vantage.PMS.Models.Sales;

public class ContactPerson
{
    public int Id { get; set; }

    public int SalesAccountId { get; set; }

    public SalesAccount? SalesAccount { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string? Position { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    public bool IsPrimary { get; set; }

    public string? Notes { get; set; }
}
