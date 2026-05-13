namespace Vantage.PMS.Models.Finance;

public enum PaymentStatus
{
    Pending = 0,
    Authorized = 1,
    Completed = 2,
    Voided = 3,
    Refunded = 4,
    Failed = 5
}
