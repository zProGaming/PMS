namespace Vantage.PMS.Models.FoodBeverage;

public class DiningTable
{
    public int Id { get; set; }

    public int OutletId { get; set; }

    public Outlet? Outlet { get; set; }

    public string TableName { get; set; } = string.Empty;

    public int SeatingCapacity { get; set; }

    public DiningTableStatus Status { get; set; } = DiningTableStatus.Available;
}
