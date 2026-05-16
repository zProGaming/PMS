using Vantage.PMS.Models.FrontOffice;

namespace Vantage.PMS.Models.Housekeeping;

public class HousekeepingTask
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public Room? Room { get; set; }

    public string AssignedTo { get; set; } = string.Empty;

    public HousekeepingTaskStatus TaskStatus { get; set; } = HousekeepingTaskStatus.Open;

    public HousekeepingTaskPriority Priority { get; set; } = HousekeepingTaskPriority.Normal;

    public string? Notes { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
