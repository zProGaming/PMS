using Vantage.PMS.Models.Sales;

namespace Vantage.PMS.Models.Banquet;

public class BanquetEvent
{
    public int Id { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string? ContactNumber { get; set; }

    public string? Email { get; set; }

    public int? SalesAccountId { get; set; }

    public SalesAccount? SalesAccount { get; set; }

    public int? SalesLeadId { get; set; }

    public SalesLead? SalesLead { get; set; }

    public int FunctionRoomId { get; set; }

    public FunctionRoom? FunctionRoom { get; set; }

    public int? BanquetPackageId { get; set; }

    public BanquetPackage? BanquetPackage { get; set; }

    public DateTime EventDate { get; set; } = DateTime.Today;

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public int ExpectedPax { get; set; }

    public int GuaranteedPax { get; set; }

    public int? ActualPax { get; set; }

    public BanquetEventStatus EventStatus { get; set; } = BanquetEventStatus.Tentative;

    public BanquetEventType EventType { get; set; } = BanquetEventType.Other;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string CreatedBy { get; set; } = string.Empty;

    public BanquetEventOrder? BanquetEventOrder { get; set; }

    public ICollection<BanquetCharge> Charges { get; set; } = new List<BanquetCharge>();
}
