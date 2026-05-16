namespace Vantage.PMS.Models.SystemAdministration;

public enum AuditActionType
{
    Create = 0,
    Update = 1,
    Delete = 2,
    SoftDelete = 3,
    Login = 4,
    Logout = 5,
    Approve = 6,
    Reject = 7,
    Void = 8,
    Cancel = 9,
    Post = 10,
    Generate = 11,
    Export = 12,
    Other = 13
}

public enum SystemNotificationType
{
    Info = 0,
    Approval = 1,
    Warning = 2,
    Error = 3,
    Task = 4,
    Reminder = 5
}

public enum SystemSeverity
{
    Info = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum DataValidationIssueType
{
    MissingRequiredData = 0,
    InvalidStatus = 1,
    InconsistentBalance = 2,
    OrphanRecord = 3,
    DuplicateRecord = 4,
    DateConflict = 5,
    SecurityConfiguration = 6,
    Other = 7
}

public enum QATestChecklistStatus
{
    NotTested = 0,
    Passed = 1,
    Failed = 2,
    Blocked = 3,
    NeedsFix = 4
}

public enum DemoPackageStatus
{
    Draft = 0,
    Ready = 1,
    Presented = 2,
    Archived = 3
}

public class AuditLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public AuditActionType Action { get; set; } = AuditActionType.Other;

    public string EntityName { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Module { get; set; } = string.Empty;
}

public class SystemErrorLog
{
    public int Id { get; set; }

    public DateTime ErrorDate { get; set; } = DateTime.Now;

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Path { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public string? StackTrace { get; set; }

    public string? Source { get; set; }

    public bool IsResolved { get; set; }

    public string? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? Notes { get; set; }
}

public class SystemNotification
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public SystemNotificationType NotificationType { get; set; } = SystemNotificationType.Info;

    public SystemSeverity Severity { get; set; } = SystemSeverity.Info;

    public string? TargetRole { get; set; }

    public string? TargetUserId { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ReadAt { get; set; }

    public string? RelatedModule { get; set; }

    public string? RelatedReferenceType { get; set; }

    public int? RelatedReferenceId { get; set; }
}

public class SystemSetting
{
    public int Id { get; set; }

    public string SettingKey { get; set; } = string.Empty;

    public string SettingValue { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Module { get; set; } = string.Empty;

    public bool IsEditable { get; set; } = true;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string? UpdatedBy { get; set; }
}

public class DataValidationIssue
{
    public int Id { get; set; }

    public DateTime IssueDate { get; set; } = DateTime.Now;

    public string Module { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public int? EntityId { get; set; }

    public DataValidationIssueType IssueType { get; set; } = DataValidationIssueType.Other;

    public SystemSeverity Severity { get; set; } = SystemSeverity.Info;

    public string Description { get; set; } = string.Empty;

    public string RecommendedFix { get; set; } = string.Empty;

    public bool IsResolved { get; set; }

    public string? ResolvedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }
}

public class QATestChecklistItem
{
    public int Id { get; set; }

    public string Module { get; set; } = string.Empty;

    public string TestName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public QATestChecklistStatus Status { get; set; } = QATestChecklistStatus.NotTested;

    public string? TestedBy { get; set; }

    public DateTime? TestedAt { get; set; }

    public string? Notes { get; set; }
}

public class DocumentTemplateSetting
{
    public int Id { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public string DocumentType { get; set; } = string.Empty;

    public string HeaderTitle { get; set; } = string.Empty;

    public string? FooterText { get; set; }

    public bool ShowLogo { get; set; } = true;

    public bool ShowPreparedBy { get; set; } = true;

    public bool ShowApprovedBy { get; set; } = true;

    public bool ShowPrintedDate { get; set; } = true;

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

public class ClientDemoPackage
{
    public int Id { get; set; }

    public string PackageName { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string HotelName { get; set; } = string.Empty;

    public string PreparedBy { get; set; } = string.Empty;

    public DateTime PreparedDate { get; set; } = DateTime.Today;

    public DemoPackageStatus Status { get; set; } = DemoPackageStatus.Draft;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<ClientDemoPackageItem> Items { get; set; } = new List<ClientDemoPackageItem>();
}

public class ClientDemoPackageItem
{
    public int Id { get; set; }

    public int ClientDemoPackageId { get; set; }

    public ClientDemoPackage? ClientDemoPackage { get; set; }

    public string ItemTitle { get; set; } = string.Empty;

    public string ModuleName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsIncluded { get; set; } = true;
}
