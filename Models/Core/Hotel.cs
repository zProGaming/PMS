namespace Vantage.PMS.Models.Core;

public class Hotel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? LegalName { get; set; }

    public ICollection<Property> Properties { get; set; } = new List<Property>();
}
