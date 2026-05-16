namespace Vantage.PMS.Models.FoodBeverage;

public class Outlet
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public OutletType OutletType { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<DiningTable> DiningTables { get; set; } = new List<DiningTable>();

    public ICollection<POSOrder> Orders { get; set; } = new List<POSOrder>();
}
