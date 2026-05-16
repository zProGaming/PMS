namespace Vantage.PMS.Models.Inventory;

public enum PurchaseRequestStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    ConvertedToPO = 4,
    Cancelled = 5
}
