namespace Vantage.PMS.Models.Sales;

public class SalesAccount
{
    public int Id { get; set; }

    public string AccountName { get; set; } = string.Empty;

    public SalesAccountType AccountType { get; set; } = SalesAccountType.Corporate;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public decimal CreditLimit { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<ContactPerson> ContactPersons { get; set; } = new List<ContactPerson>();

    public ICollection<SalesLead> SalesLeads { get; set; } = new List<SalesLead>();

    public ICollection<SalesActivity> SalesActivities { get; set; } = new List<SalesActivity>();
}
