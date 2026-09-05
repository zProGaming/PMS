using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Finance;
using Vantage.PMS.Models.FrontOffice;
using Vantage.PMS.Models.Housekeeping;

namespace Vantage.PMS.Pages;

[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class IndexModel(ApplicationDbContext context) : PageModel
{
    public sealed record WorkspaceOption(string Id, string Label);
    public sealed record WorkItem(string Reference, string Detail, string Status, string Action, string Page,
        Dictionary<string, string>? Route = null, decimal? Amount = null, string? Fragment = null);
    public sealed record WorkQueue(string Title, string Description, int Total, string AllPage, IReadOnlyList<WorkItem> Items);
    public IReadOnlyList<WorkspaceOption> Workspaces { get; private set; } = [];
    public string Workspace { get; private set; } = string.Empty;
    public string WorkspaceLabel => Workspaces.FirstOrDefault(w => w.Id == Workspace)?.Label ?? "Your workspace";
    public DateTime BusinessDate { get; private set; }
    public IList<WorkQueue> Queues { get; } = new List<WorkQueue>();
    public const int QueueLimit = 8;

    public static IReadOnlyList<WorkspaceOption> AllowedWorkspaces(ClaimsPrincipal user)
    {
        var result = new List<WorkspaceOption>();
        if (PmsRoles.ExecutiveManagement.Any(user.IsInRole)) result.Add(new("manager", "Manager"));
        if (PmsRoles.FrontOffice.Any(user.IsInRole)) result.Add(new("front-desk", "Front desk"));
        if (PmsRoles.Finance.Any(user.IsInRole)) result.Add(new("cashier", "Cashier"));
        if (PmsRoles.Housekeeping.Any(user.IsInRole)) result.Add(new("housekeeping", "Housekeeping"));
        return result;
    }

    public async Task<IActionResult> OnGetAsync(string? workspace)
    {
        Workspaces = AllowedWorkspaces(User);
        if (workspace is not null && !Workspaces.Any(w => w.Id == workspace)) return Forbid();
        Workspace = workspace ?? Workspaces.FirstOrDefault()?.Id ?? string.Empty;
        // No other department's queries are executed for this request.
        if (Workspace.Length == 0) return Page();
        BusinessDate = (await context.BusinessDateSettings.AsNoTracking().OrderBy(s => s.Id)
            .Select(s => (DateTime?)s.CurrentBusinessDate).FirstOrDefaultAsync() ?? DateTime.Today).Date;
        switch (Workspace)
        {
            case "front-desk": await LoadFrontDeskAsync(); break;
            case "cashier": await LoadCashierAsync(); break;
            case "housekeeping": await LoadHousekeepingAsync(); break;
            case "manager": await LoadManagerAsync(); break;
        }
        return Page();
    }

    private static Dictionary<string, string> Id(int id, string key = "id") => new() { [key] = id.ToString() };

    private async Task LoadFrontDeskAsync()
    {
        var tomorrow = BusinessDate.AddDays(1);
        var arrivals = context.Reservations.AsNoTracking().Where(r => r.Status == ReservationStatus.Reserved && r.ArrivalDate < tomorrow);
        var rows = await arrivals.Include(r => r.Guest).Include(r => r.Room).OrderBy(r => r.ArrivalDate).ThenBy(r => r.Id).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Arrivals to check in", "Today's arrivals and earlier reservations still awaiting a decision.", await arrivals.CountAsync(), "/FrontOffice/Reservations/Index",
            rows.Select(r => new WorkItem(r.ConfirmationNumber, $"{r.Guest?.FirstName} {r.Guest?.LastName} · Room {r.Room?.RoomNumber ?? "unassigned"}",
                r.ArrivalDate < BusinessDate ? "Overdue arrival" : r.RoomId == null ? "Assign room" : "Review room readiness", "Review arrival", "/FrontOffice/Reservations/Details", Id(r.Id))).ToList()));
        var departures = context.Reservations.AsNoTracking().Where(r => r.Status == ReservationStatus.CheckedIn && r.DepartureDate < tomorrow);
        rows = await departures.Include(r => r.Guest).Include(r => r.Room).OrderBy(r => r.DepartureDate).ThenBy(r => r.Id).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Departures to settle", "Review all folios before checkout. Late departures appear first.", await departures.CountAsync(), "/FrontOffice/Reservations/Index",
            rows.Select(r => new WorkItem(r.ConfirmationNumber, $"{r.Guest?.FirstName} {r.Guest?.LastName} · Room {r.Room?.RoomNumber ?? "unassigned"}",
                r.DepartureDate < BusinessDate ? "Overdue departure" : "Due today", "Review checkout", "/FrontOffice/Reservations/CheckOut", Id(r.Id))).ToList()));
    }

    private async Task LoadCashierAsync()
    {
        var shifts = context.CashierShifts.AsNoTracking().Where(s => s.OpenedBy == User.Identity!.Name && s.Status == CashierShiftStatus.Open);
        var shiftRows = await shifts.OrderBy(s => s.OpenedAt).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Your open shifts", "Check your shift before collecting payments and reconcile it at handover.", await shifts.CountAsync(), "/Finance/CashierShifts/Index",
            shiftRows.Select(s => new WorkItem(s.ShiftNumber, $"Opened {s.OpenedAt:dd MMM, HH:mm}", "Open", "Review shift", "/Finance/CashierShifts/Index")).ToList()));
        var folios = context.Folios.AsNoTracking().Where(f => f.Status == FolioStatus.Open)
            .Select(f => new
            {
                f.Id, f.FolioNumber, GuestName = f.Guest!.FirstName + " " + f.Guest.LastName,
                Balance = f.Items.Where(i => !i.IsVoided).Sum(i => (decimal?)i.Amount) ?? 0,
                Paid = f.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => (decimal?)p.Amount) ?? 0,
                Departed = f.Reservation!.Status == ReservationStatus.CheckedOut
            }).Where(f => f.Balance != f.Paid);
        var folioRows = await folios.OrderByDescending(f => f.Departed).ThenBy(f => f.Id).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Collection and guest-credit follow-up", "Departed guests appear first. Collect payments here or review source receipts for guest-credit refunds.", await folios.CountAsync(), "/Finance/Settlement/Index",
            folioRows.Select(f => new WorkItem(f.FolioNumber, f.GuestName,
                f.Balance < f.Paid ? "Guest credit" : f.Departed ? "Departed · unpaid" : "In-house · unpaid",
                f.Balance > f.Paid ? "Collect Payment" : "Review Receipts", f.Balance > f.Paid ? "/Finance/Settlement/PostPayment" : "/Finance/Payments/Index", Id(f.Id, "folioId"), f.Balance - f.Paid)).ToList()));
    }

    private async Task LoadHousekeepingAsync()
    {
        var tasks = context.HousekeepingTasks.AsNoTracking().Where(t => t.TaskStatus == HousekeepingTaskStatus.Open || t.TaskStatus == HousekeepingTaskStatus.InProgress);
        var rows = await tasks.Include(t => t.Room).OrderByDescending(t => t.Priority).ThenBy(t => t.Id).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Team turnover tasks", "Highest priority first. Completing the final cleaning task marks a vacant dirty room Clean. A supervisor inspects and releases it.", await tasks.CountAsync(), "/Housekeeping/Tasks/Index",
            rows.Select(t => new WorkItem($"Room {t.Room?.RoomNumber}", string.IsNullOrWhiteSpace(t.AssignedTo) ? "Awaiting assignment" : t.AssignedTo,
                $"{t.Priority} · {(t.TaskStatus == HousekeepingTaskStatus.InProgress ? "In progress" : "To do")}", "Review task", "/Housekeeping/Tasks/Index", Id(t.Id, "taskId"))).ToList()));
        var rooms = context.Rooms.AsNoTracking().Where(r => r.IsActive && (r.Status == RoomStatus.Dirty || r.Status == RoomStatus.Clean));
        var roomRows = await rooms.OrderBy(r => r.RoomNumber).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Rooms awaiting readiness", "Cleaning and inspection are separate checks. Confirm the physical room condition before changing status.", await rooms.CountAsync(), "/Housekeeping/Index",
            roomRows.Select(r => new WorkItem($"Room {r.RoomNumber}", r.Status == RoomStatus.Dirty ? "Cleaning required" : "Inspection required", r.Status.ToString(), "Review room", "/Housekeeping/Rooms/UpdateStatus", Id(r.Id))).ToList()));
    }

    private async Task LoadManagerAsync()
    {
        var voids = context.VoidRequests.AsNoTracking().Where(v => v.Status == ApprovalStatus.Pending);
        var voidRows = await voids.OrderBy(v => v.RequestedAt).ThenBy(v => v.Id).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Voids awaiting approval", "Oldest requests first. Review the source transaction and reason before approving.", await voids.CountAsync(), "/Finance/VoidRequests/Index",
            voidRows.Select(v => new WorkItem($"Void #{v.Id}", $"{v.ReferenceType} #{v.ReferenceId} · {v.RequestedBy}", "Pending approval", "Review request", "/Finance/VoidRequests/Index", Fragment: $"request-{v.Id}")).ToList()));
        var refunds = context.RefundTransactions.AsNoTracking().Where(r => r.Status == RefundStatus.Requested || r.Status == RefundStatus.ForApproval || r.Status == RefundStatus.Approved);
        var refundRows = await refunds.OrderBy(r => r.Id).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Refunds requiring action", "Separate approval from payout. Approved requests still need processing.", await refunds.CountAsync(), "/Finance/Refunds/Index",
            refundRows.Select(r => new WorkItem(r.RefundNumber, "Guest refund", r.Status == RefundStatus.Approved ? "Awaiting payout" : "Awaiting approval", "Review refund", "/Finance/Refunds/Index", Amount: r.Amount, Fragment: $"request-{r.Id}")).ToList()));
        var discounts = context.DiscountApprovals.AsNoTracking().Where(d => d.Status == ApprovalStatus.Pending);
        var discountRows = await discounts.OrderBy(d => d.Id).Take(QueueLimit).ToListAsync();
        Queues.Add(new("Discounts awaiting approval", "Confirm the policy, amount and approver before applying a discount.", await discounts.CountAsync(), "/Finance/DiscountApprovals/Index",
            discountRows.Select(d => new WorkItem($"Discount #{d.Id}", d.RequestedBy, "Pending approval", "Review discount", "/Finance/DiscountApprovals/Index", Amount: d.DiscountAmount, Fragment: $"request-{d.Id}")).ToList()));
    }
}
