using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.Events;

public class EditModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BanquetEvent BanquetEvent { get; set; } = default!;

    public SelectList FunctionRoomOptions { get; set; } = default!;

    public SelectList BanquetPackageOptions { get; set; } = default!;

    public SelectList SalesAccountOptions { get; set; } = default!;

    public SelectList SalesLeadOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> EventTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var banquetEvent = await _context.BanquetEvents.FindAsync(id);
        if (banquetEvent is null)
        {
            return NotFound();
        }

        BanquetEvent = banquetEvent;
        await LoadOptionsAsync(
            BanquetEvent.FunctionRoomId,
            BanquetEvent.BanquetPackageId,
            BanquetEvent.SalesAccountId,
            BanquetEvent.SalesLeadId,
            BanquetEvent.EventStatus,
            BanquetEvent.EventType);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateEvent();
        await ValidateRoomConflictAsync(BanquetEvent.Id);

        if (!ModelState.IsValid)
        {
            await LoadOptionsAsync(
                BanquetEvent.FunctionRoomId,
                BanquetEvent.BanquetPackageId,
                BanquetEvent.SalesAccountId,
                BanquetEvent.SalesLeadId,
                BanquetEvent.EventStatus,
                BanquetEvent.EventType);
            return Page();
        }

        var banquetEvent = await _context.BanquetEvents.FindAsync(BanquetEvent.Id);
        if (banquetEvent is null)
        {
            return NotFound();
        }

        banquetEvent.EventName = BanquetEvent.EventName;
        banquetEvent.ClientName = BanquetEvent.ClientName;
        banquetEvent.ContactNumber = BanquetEvent.ContactNumber;
        banquetEvent.Email = BanquetEvent.Email;
        banquetEvent.SalesAccountId = BanquetEvent.SalesAccountId;
        banquetEvent.SalesLeadId = BanquetEvent.SalesLeadId;
        banquetEvent.FunctionRoomId = BanquetEvent.FunctionRoomId;
        banquetEvent.BanquetPackageId = BanquetEvent.BanquetPackageId;
        banquetEvent.EventDate = BanquetEvent.EventDate;
        banquetEvent.StartTime = BanquetEvent.StartTime;
        banquetEvent.EndTime = BanquetEvent.EndTime;
        banquetEvent.ExpectedPax = BanquetEvent.ExpectedPax;
        banquetEvent.GuaranteedPax = BanquetEvent.GuaranteedPax;
        banquetEvent.ActualPax = BanquetEvent.ActualPax;
        banquetEvent.EventStatus = BanquetEvent.EventStatus;
        banquetEvent.EventType = BanquetEvent.EventType;
        banquetEvent.Notes = BanquetEvent.Notes;

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = banquetEvent.Id });
    }

    private void ValidateEvent()
    {
        if (BanquetEvent.EndTime <= BanquetEvent.StartTime)
        {
            ModelState.AddModelError("BanquetEvent.EndTime", "End time must be later than start time.");
        }

        if (BanquetEvent.ExpectedPax < 0 || BanquetEvent.GuaranteedPax < 0 || BanquetEvent.ActualPax < 0)
        {
            ModelState.AddModelError(string.Empty, "Pax values cannot be negative.");
        }
    }

    private async Task ValidateRoomConflictAsync(int currentEventId)
    {
        if (BanquetEvent.EventStatus is not (BanquetEventStatus.Tentative or BanquetEventStatus.Confirmed))
        {
            return;
        }

        var hasConflict = await _context.BanquetEvents.AnyAsync(existing =>
            existing.Id != currentEventId &&
            existing.FunctionRoomId == BanquetEvent.FunctionRoomId &&
            existing.EventDate.Date == BanquetEvent.EventDate.Date &&
            (existing.EventStatus == BanquetEventStatus.Tentative ||
             existing.EventStatus == BanquetEventStatus.Confirmed) &&
            BanquetEvent.StartTime < existing.EndTime &&
            BanquetEvent.EndTime > existing.StartTime);

        if (hasConflict)
        {
            ModelState.AddModelError(string.Empty, "Function room is already blocked by a tentative or confirmed event during this date and time.");
        }
    }

    private async Task LoadOptionsAsync(
        object? selectedRoom = null,
        object? selectedPackage = null,
        object? selectedAccount = null,
        object? selectedLead = null,
        BanquetEventStatus selectedStatus = BanquetEventStatus.Tentative,
        BanquetEventType selectedType = BanquetEventType.Other)
    {
        var rooms = await _context.FunctionRooms
            .AsNoTracking()
            .OrderBy(room => room.Name)
            .Select(room => new { room.Id, room.Name })
            .ToListAsync();

        var packages = await _context.BanquetPackages
            .AsNoTracking()
            .OrderBy(package => package.PackageName)
            .Select(package => new { package.Id, package.PackageName })
            .ToListAsync();

        var accounts = await _context.SalesAccounts
            .AsNoTracking()
            .OrderBy(account => account.AccountName)
            .Select(account => new { account.Id, account.AccountName })
            .ToListAsync();

        var leads = await _context.SalesLeads
            .Include(lead => lead.SalesAccount)
            .AsNoTracking()
            .OrderBy(lead => lead.LeadName)
            .ToListAsync();

        FunctionRoomOptions = new SelectList(rooms, "Id", "Name", selectedRoom);
        BanquetPackageOptions = new SelectList(packages, "Id", "PackageName", selectedPackage);
        SalesAccountOptions = new SelectList(accounts, "Id", "AccountName", selectedAccount);
        SalesLeadOptions = new SelectList(
            leads.Select(lead => new { lead.Id, Name = $"{lead.LeadName} ({lead.SalesAccount?.AccountName ?? "No account"})" }),
            "Id",
            "Name",
            selectedLead);
        StatusOptions = Enum.GetValues<BanquetEventStatus>().Select(status => new SelectListItem
        {
            Value = status.ToString(),
            Text = status.ToString(),
            Selected = status == selectedStatus
        });
        EventTypeOptions = Enum.GetValues<BanquetEventType>().Select(type => new SelectListItem
        {
            Value = type.ToString(),
            Text = type.ToString(),
            Selected = type == selectedType
        });
    }
}
