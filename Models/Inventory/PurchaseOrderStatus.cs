namespace Vantage.PMS.Models.Inventory;

public enum PurchaseOrderStatus
{
    Draft = 0,
    ForApproval = 1,
    Approved = 2,
    PartiallyReceived = 3,
    FullyReceived = 4,
    Closed = 5,
    Cancelled = 6
}
