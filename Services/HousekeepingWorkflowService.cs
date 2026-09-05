using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Models.GuestPortal;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class HousekeepingWorkflowService(ApplicationDbContext context)
{
    public static bool CanSupervise(ClaimsPrincipal user) => user.IsInRole(PmsRoles.SystemAdmin) ||
        user.IsInRole(PmsRoles.GeneralManager) || user.IsInRole(PmsRoles.HousekeepingSupervisor);
    private static bool CanWork(ClaimsPrincipal user) => !string.IsNullOrWhiteSpace(user.Identity?.Name) && PmsRoles.Housekeeping.Any(user.IsInRole);

    public static IEnumerable<RoomStatus> AllowedStatuses(RoomStatus current, ClaimsPrincipal user)
    {
        if (!CanWork(user) || current == RoomStatus.Occupied) return [];
        if (!CanSupervise(user)) return current == RoomStatus.Dirty ? [RoomStatus.Clean] : [];
        RoomStatus[] next = current switch
        {
            RoomStatus.Dirty => [RoomStatus.Clean],
            RoomStatus.Clean => [RoomStatus.Inspected, RoomStatus.Dirty],
            RoomStatus.Inspected => [RoomStatus.Available, RoomStatus.Dirty],
            RoomStatus.Available => [RoomStatus.Dirty, RoomStatus.Maintenance],
            RoomStatus.Maintenance => [RoomStatus.Dirty],
            RoomStatus.OutOfOrder => [RoomStatus.Maintenance],
            _ => []
        };
        return current == RoomStatus.OutOfOrder ? next : next.Append(RoomStatus.OutOfOrder);
    }

    public Task<IList<string>> CreateTaskAsync(HousekeepingTask input, ClaimsPrincipal user) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync<IList<string>>(async () =>
        {
            if (!CanWork(user)) return ["Housekeeping access is required."];
            if (string.IsNullOrWhiteSpace(input.AssignedTo) || !Enum.IsDefined(input.Priority)) return ["Select a valid priority and enter the task assignee."];
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var room = await LockRoomAsync(input.RoomId);
            if (room is null || !room.IsActive) return ["Select an active room."];
            var before = room.Status;
            // Opening work for a vacant ready room removes it from the ready inventory.
            if (room.Status is RoomStatus.Available or RoomStatus.Clean or RoomStatus.Inspected) room.Status = RoomStatus.Dirty;
            var task = new HousekeepingTask
            {
                RoomId = room.Id, AssignedTo = input.AssignedTo.Trim(), Priority = input.Priority,
                Notes = input.Notes, TaskStatus = HousekeepingTaskStatus.Open
            };
            context.HousekeepingTasks.Add(task);
            Audit(room.Id, "Create Task", before, room.Status, user, null);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return [];
        });

    public Task<IList<string>> CompleteTaskAsync(int id, ClaimsPrincipal user) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync<IList<string>>(async () =>
        {
            if (!CanWork(user)) return ["Housekeeping access is required."];
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var roomId = await context.HousekeepingTasks.AsNoTracking().Where(t => t.Id == id).Select(t => (int?)t.RoomId).FirstOrDefaultAsync();
            if (roomId is null) return ["Housekeeping task was not found."];
            var room = await LockRoomAsync(roomId.Value);
            if (room is null) return ["Room was not found."];
            var task = await context.HousekeepingTasks.FirstAsync(t => t.Id == id);
            if (task.TaskStatus == HousekeepingTaskStatus.Completed) return [];
            if (task.TaskStatus == HousekeepingTaskStatus.Cancelled) return ["A cancelled task cannot be completed."];
            var before = room.Status;
            task.TaskStatus = HousekeepingTaskStatus.Completed; task.CompletedAt = DateTime.Now; task.StartedAt ??= task.CompletedAt;
            var otherWork = await context.HousekeepingTasks.AnyAsync(t => t.RoomId == room.Id && t.Id != id &&
                (t.TaskStatus == HousekeepingTaskStatus.Open || t.TaskStatus == HousekeepingTaskStatus.InProgress));
            var occupied = await context.Reservations.AnyAsync(r => r.RoomId == room.Id && r.Status == ReservationStatus.CheckedIn);
            if (room.Status == RoomStatus.Dirty && !otherWork && !occupied) room.Status = RoomStatus.Clean;
            Audit(room.Id, "Complete Task", before, room.Status, user, id);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return [];
        });

    public Task<IList<string>> CreateGuestTaskAsync(int id, ClaimsPrincipal user) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync<IList<string>>(async () =>
        {
            if (!PmsRoles.GuestPortalManagement.Any(user.IsInRole) || string.IsNullOrWhiteSpace(user.Identity?.Name)) return ["Guest service management access is required."];
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var roomId = await context.GuestServiceRequests.AsNoTracking().Where(r => r.Id == id).Select(r => r.RoomId).FirstOrDefaultAsync();
            if (roomId is null) return ["The guest request needs an assigned room."];
            var room = await LockRoomAsync(roomId.Value);
            if (room is null || !room.IsActive) return ["The guest request needs an active room."];
            var request = await context.GuestServiceRequests.FirstAsync(r => r.Id == id);
            if (request.RequestType is not (GuestServiceRequestType.Housekeeping or GuestServiceRequestType.Amenities or GuestServiceRequestType.ExtraTowels or GuestServiceRequestType.ExtraPillows))
                return ["Only housekeeping or amenity requests can create a housekeeping task."];
            var prefix = $"Guest request #{id}:";
            if (await context.HousekeepingTasks.AnyAsync(t => t.RoomId == room.Id && t.Notes != null && t.Notes.StartsWith(prefix))) return [];
            if (request.Status is GuestServiceRequestStatus.Completed or GuestServiceRequestStatus.Cancelled) return ["A completed or cancelled guest request cannot create new work."];
            var before = room.Status;
            if (room.Status is RoomStatus.Available or RoomStatus.Clean or RoomStatus.Inspected) room.Status = RoomStatus.Dirty;
            request.Status = GuestServiceRequestStatus.Assigned;
            if (string.IsNullOrWhiteSpace(request.AssignedTo)) request.AssignedTo = "Housekeeping Queue";
            context.HousekeepingTasks.Add(new HousekeepingTask
            {
                RoomId = room.Id, AssignedTo = request.AssignedTo, Notes = $"{prefix} {request.Description}",
                Priority = request.Priority switch
                {
                    GuestServiceRequestPriority.Low => HousekeepingTaskPriority.Low,
                    GuestServiceRequestPriority.High => HousekeepingTaskPriority.High,
                    GuestServiceRequestPriority.Urgent => HousekeepingTaskPriority.Urgent,
                    _ => HousekeepingTaskPriority.Normal
                }
            });
            Audit(room.Id, "Create Guest Task", before, room.Status, user, null, prefix);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return [];
        });

    public Task<IList<string>> UpdateRoomAsync(int id, RoomStatus? expected, RoomStatus target, string? notes, ClaimsPrincipal user) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync<IList<string>>(async () =>
        {
            if (!CanWork(user)) return ["Housekeeping access is required."];
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var room = await LockRoomAsync(id);
            if (room is null || !room.IsActive) return ["An active room was not found."];
            if (expected is null || expected != room.Status) return ["Room status has changed. Refresh and review the current condition before submitting again."];
            if (!AllowedStatuses(room.Status, user).Contains(target)) return ["This status change is not permitted. Inspection and release require a housekeeping supervisor."];
            if (await context.Reservations.AnyAsync(r => r.RoomId == id && r.Status == ReservationStatus.CheckedIn)) return ["This room still has a checked-in guest. Front Desk must resolve occupancy first."];
            if (target is RoomStatus.Clean or RoomStatus.Inspected or RoomStatus.Available &&
                await context.HousekeepingTasks.AnyAsync(t => t.RoomId == id && (t.TaskStatus == HousekeepingTaskStatus.Open || t.TaskStatus == HousekeepingTaskStatus.InProgress)))
                return ["Complete all open housekeeping tasks before advancing room readiness."];
            if ((target is RoomStatus.OutOfOrder or RoomStatus.Maintenance || room.Status is RoomStatus.OutOfOrder or RoomStatus.Maintenance) &&
                (string.IsNullOrWhiteSpace(notes) || notes.Trim().Length < 10)) return ["Enter a reason of at least 10 characters for a maintenance or out-of-order change."];
            if (notes?.Length > 500) return ["Keep status notes within 500 characters."];
            var before = room.Status;
            room.Status = target; room.StatusNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            Audit(id, "Update Room", before, target, user, null, notes);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return [];
        });

    private async Task<Room?> LockRoomAsync(int id) => (await context.Rooms.FromSqlInterpolated(
        $"SELECT * FROM [Rooms] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {id}").ToListAsync()).SingleOrDefault();

    private void Audit(int roomId, string action, RoomStatus before, RoomStatus after, ClaimsPrincipal user, int? taskId, string? notes = null) =>
        context.AuditLogs.Add(new AuditLog
        {
            Module = "Housekeeping", EntityName = "HousekeepingDecision", EntityId = roomId.ToString(), Action = AuditActionType.Update,
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier), UserName = user.Identity?.Name,
            NewValues = JsonSerializer.Serialize(new { Action = action, RoomId = roomId, TaskId = taskId, Before = before, After = after, Notes = notes, OccurredAtUtc = DateTime.UtcNow })
        });
}
