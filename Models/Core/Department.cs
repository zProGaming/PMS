namespace Vantage.PMS.Models.Core;

public class Department
{
    public int Id { get; set; }

    public int PropertyId { get; set; }

    public Property? Property { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
