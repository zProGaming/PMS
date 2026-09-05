using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Housekeeping;
using Vantage.PMS.Services;
using Xunit;

namespace Vantage.PMS.Tests;

[Collection("Checkout SQL")]
public class HousekeepingWorkflowTests(CheckoutDatabase database)
{
    private static System.Security.Claims.ClaimsPrincipal Cleaner => FinanceAdjustmentTests.Actor(PmsRoles.Housekeeper, "training.cleaner");
    private static System.Security.Claims.ClaimsPrincipal Supervisor => FinanceAdjustmentTests.Actor(PmsRoles.HousekeepingSupervisor, "training.supervisor");

    private async Task<(int Room, int Task)> SeedAsync(bool occupied = false)
    {
        var id = await database.SeedStayAsync(0);
        await using var context = database.Open();
        var stay = await context.Reservations.Include(r => r.Room).SingleAsync(r => r.Id == id);
        if (!occupied) { stay.Status = ReservationStatus.CheckedOut; stay.Room!.Status = RoomStatus.Dirty; }
        var task = new HousekeepingTask { RoomId = stay.RoomId!.Value, AssignedTo = "Training cleaner", Notes = "Training cleaning task" };
        context.HousekeepingTasks.Add(task); await context.SaveChangesAsync();
        return (stay.RoomId.Value, task.Id);
    }

    [Fact] public async Task CompletionMakesCleanButNeverAvailableAndRetryPreservesTimestamp()
    {
        var seed = await SeedAsync();
        await using var context = database.Open();
        var service = new HousekeepingWorkflowService(context);
        Assert.Empty(await service.CompleteTaskAsync(seed.Task, Cleaner));
        var completedAt = (await context.HousekeepingTasks.FindAsync(seed.Task))!.CompletedAt;
        Assert.Empty(await service.CompleteTaskAsync(seed.Task, Cleaner));
        Assert.Equal(completedAt, (await context.HousekeepingTasks.FindAsync(seed.Task))!.CompletedAt);
        Assert.Equal(RoomStatus.Clean, (await context.Rooms.FindAsync(seed.Room))!.Status);
        Assert.NotEmpty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Clean, RoomStatus.Available, null, Supervisor));
        Assert.NotEmpty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Clean, RoomStatus.Inspected, null, Cleaner));
        Assert.Empty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Clean, RoomStatus.Inspected, null, Supervisor));
        Assert.NotEmpty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Clean, RoomStatus.Available, null, Supervisor));
        Assert.Empty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Inspected, RoomStatus.Available, null, Supervisor));
        Assert.Equal(3, await context.AuditLogs.CountAsync(a => a.EntityName == "HousekeepingDecision" && a.EntityId == seed.Room.ToString()));
    }

    [Fact] public async Task OutstandingTasksBlockReadinessAndCancelledTasksCannotComplete()
    {
        var seed = await SeedAsync();
        await using var context = database.Open();
        var other = new HousekeepingTask { RoomId = seed.Room, AssignedTo = "Second cleaner" };
        context.HousekeepingTasks.Add(other); await context.SaveChangesAsync();
        var service = new HousekeepingWorkflowService(context);
        Assert.Empty(await service.CompleteTaskAsync(seed.Task, Cleaner));
        Assert.Equal(RoomStatus.Dirty, (await context.Rooms.FindAsync(seed.Room))!.Status);
        Assert.NotEmpty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Dirty, RoomStatus.Clean, null, Cleaner));
        (await context.HousekeepingTasks.FindAsync(other.Id))!.TaskStatus = HousekeepingTaskStatus.Cancelled;
        await context.SaveChangesAsync();
        Assert.NotEmpty(await service.CompleteTaskAsync(other.Id, Cleaner));
    }

    [Fact] public async Task OccupiedRoomIsNotReleasedByTaskCompletion()
    {
        var seed = await SeedAsync(true);
        await using var context = database.Open();
        var service = new HousekeepingWorkflowService(context);
        Assert.Empty(await service.CompleteTaskAsync(seed.Task, Cleaner));
        Assert.Equal(RoomStatus.Occupied, (await context.Rooms.FindAsync(seed.Room))!.Status);
        Assert.NotEmpty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Occupied, RoomStatus.OutOfOrder, "Training reason.", Supervisor));
    }

    [Fact] public async Task MaintenanceRecoveryRequiresCleaningAndReason()
    {
        var seed = await SeedAsync();
        await using var context = database.Open();
        var service = new HousekeepingWorkflowService(context);
        Assert.NotEmpty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Dirty, RoomStatus.OutOfOrder, null, Supervisor));
        Assert.Empty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Dirty, RoomStatus.OutOfOrder, "Air conditioning repair.", Supervisor));
        Assert.Empty(await service.UpdateRoomAsync(seed.Room, RoomStatus.OutOfOrder, RoomStatus.Maintenance, "Technician assigned to repair.", Supervisor));
        Assert.NotEmpty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Maintenance, RoomStatus.Available, "Repair completed.", Supervisor));
        Assert.Empty(await service.UpdateRoomAsync(seed.Room, RoomStatus.Maintenance, RoomStatus.Dirty, "Repair completed; clean before inspection.", Supervisor));
    }

    [Fact] public async Task GuestRequestCreatesOneTaskAndRemovesVacantRoomFromReadyInventory()
    {
        var seed = await SeedAsync();
        await using var context = database.Open();
        var room = (await context.Rooms.FindAsync(seed.Room))!;
        room.Status = RoomStatus.Available;
        var request = new Models.GuestPortal.GuestServiceRequest
        {
            RoomId = seed.Room, RequestType = Models.GuestPortal.GuestServiceRequestType.ExtraTowels, Description = "Training request for extra towels."
        };
        context.GuestServiceRequests.Add(request); await context.SaveChangesAsync();
        var user = FinanceAdjustmentTests.Actor(PmsRoles.FrontDesk, "training.frontdesk");
        var service = new HousekeepingWorkflowService(context);
        Assert.Empty(await service.CreateGuestTaskAsync(request.Id, user));
        Assert.Empty(await service.CreateGuestTaskAsync(request.Id, user));
        Assert.Equal(RoomStatus.Dirty, (await context.Rooms.FindAsync(seed.Room))!.Status);
        Assert.Single(await context.HousekeepingTasks.Where(t => t.Notes != null && t.Notes.StartsWith($"Guest request #{request.Id}:")).ToListAsync());
    }
}
