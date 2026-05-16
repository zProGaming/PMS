namespace Vantage.PMS.Models.ManagementAI;

public enum ManagementInsightType
{
    Operational = 0,
    Financial = 1,
    GuestExperience = 2,
    Revenue = 3,
    Housekeeping = 4,
    Maintenance = 5,
    Inventory = 6,
    Sales = 7,
    Banquet = 8,
    Risk = 9,
    Opportunity = 10
}

public enum ManagementInsightSeverity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum AIActionType
{
    InsightGenerated = 0,
    InsightResolved = 1,
    SummaryGenerated = 2,
    RecommendationViewed = 3,
    RecommendationDismissed = 4,
    Exported = 5,
    Other = 6
}

public class ManagementInsight
{
    public int Id { get; set; }

    public DateTime InsightDate { get; set; } = DateTime.Today;

    public ManagementInsightType InsightType { get; set; } = ManagementInsightType.Operational;

    public ManagementInsightSeverity Severity { get; set; } = ManagementInsightSeverity.Info;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public string RelatedModule { get; set; } = string.Empty;

    public string RelatedReferenceType { get; set; } = string.Empty;

    public int? RelatedReferenceId { get; set; }

    public bool IsResolved { get; set; }

    public string? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<AIActionLog> ActionLogs { get; set; } = new List<AIActionLog>();
}

public class ManagementDailySummary
{
    public int Id { get; set; }

    public DateTime BusinessDate { get; set; } = DateTime.Today;

    public decimal OccupancyPercentage { get; set; }

    public int TotalRooms { get; set; }

    public int OccupiedRooms { get; set; }

    public int AvailableRooms { get; set; }

    public int DirtyRooms { get; set; }

    public int OutOfOrderRooms { get; set; }

    public int ArrivalsToday { get; set; }

    public int DeparturesToday { get; set; }

    public int InHouseGuests { get; set; }

    public decimal RoomRevenue { get; set; }

    public decimal FBRevenue { get; set; }

    public decimal BanquetRevenue { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal TotalPayments { get; set; }

    public decimal OutstandingGuestBalances { get; set; }

    public decimal ARBalance { get; set; }

    public int OpenServiceRequests { get; set; }

    public int PendingHousekeepingTasks { get; set; }

    public int PendingMaintenanceTickets { get; set; }

    public int LowStockItems { get; set; }

    public int PendingPurchaseRequests { get; set; }

    public int PendingApprovals { get; set; }

    public string SummaryText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string GeneratedBy { get; set; } = string.Empty;
}

public class AIRecommendationRule
{
    public int Id { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string ConditionDescription { get; set; } = string.Empty;

    public string RecommendationText { get; set; } = string.Empty;

    public ManagementInsightSeverity Severity { get; set; } = ManagementInsightSeverity.Info;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class AIActionLog
{
    public int Id { get; set; }

    public DateTime ActionDate { get; set; } = DateTime.Now;

    public AIActionType ActionType { get; set; } = AIActionType.Other;

    public string Module { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PerformedBy { get; set; } = string.Empty;

    public int? RelatedInsightId { get; set; }

    public ManagementInsight? RelatedInsight { get; set; }

    public string? Notes { get; set; }
}

public class AIIntegrationSetting
{
    public int Id { get; set; }

    public string ProviderName { get; set; } = "Rule-Based MVP";

    public bool IsEnabled { get; set; }

    public string? ModelName { get; set; }

    public bool ApiKeyConfigured { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
