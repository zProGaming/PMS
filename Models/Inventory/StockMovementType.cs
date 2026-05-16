namespace Vantage.PMS.Models.Inventory;

public enum StockMovementType
{
    OpeningBalance = 0,
    PurchaseReceiving = 1,
    StockIssue = 2,
    StockReturn = 3,
    AdjustmentIncrease = 4,
    AdjustmentDecrease = 5,
    Wastage = 6,
    TransferIn = 7,
    TransferOut = 8
}
