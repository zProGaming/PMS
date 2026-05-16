using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Models.Inventory;

public class PurchaseRequest
{
    public int Id { get; set; }

    public string RequestNumber { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string RequestedBy { get; set; } = string.Empty;

    public DateTime RequestDate { get; set; } = DateTime.Today;

    public DateTime? NeededDate { get; set; }

    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Draft;

    public string? Purpose { get; set; }

    public string? Notes { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public ICollection<PurchaseRequestItem> Items { get; set; } = new List<PurchaseRequestItem>();
}
