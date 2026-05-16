namespace Vantage.PMS.Models.FoodBeverage;

public enum POSOrderStatus
{
    Open = 0,
    SentToKitchen = 1,
    Preparing = 2,
    Ready = 3,
    Served = 4,
    Closed = 5,
    Cancelled = 6
}
