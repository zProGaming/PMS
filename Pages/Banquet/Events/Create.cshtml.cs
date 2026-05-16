using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Banquet;

namespace Vantage.PMS.Pages.Banquet.Events;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BanquetEvent BanquetEvent { get; set; } = new()
    {
        EventDate = DateTime.Today,
        StartTime = new TimeSpan(9, 0, 0),
        EndTime = new TimeSpan(17, 0, 0),
        EventStatus = BanquetEventStatus.Tentative,
        EventType = BanquetEventType.Other
    };

    public SelectList FunctionRoomOptions { get; set; } = default!;

    public SelectList BanquetPackageOptions { get; set; } = default!;

    public SelectList SalesAccountOptions { get; set; } = default!;

    public SelectList SalesLeadOptions { get; set; } = default!;

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public IEnumerable<SelectListItem> EventTypeOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadOptionsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateEvent();
        await ValidateRoomConflictAsync();

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

        BanquetEvent.CreatedAt = DateTime.Now;
        BanquetEvent.CreatedBy = User.Identity?.Name ?? Environment.UserName;

        _context.BanquetEvents.Add(BanquetEvent);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = BanquetEvent.Id });
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

    private async Task ValidateRoomConflictAsync()
    {
        if (BanquetEvent.EventStatus is not (BanquetEventStatus.Tentative or BanquetEventStatus.Confirmed))
        {
            return;
        }

        var hasConflict = await _context.BanquetEvents.AnyAsync(existing =>
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
            .Where(room => room.IsActive)
            .OrderBy(room => room.Name)
            .Select(room => new { room.Id, room.Name })
            .ToListAsync();

        var packages = await _context.BanquetPackages
            .AsNoTracking()
            .Where(package => package.IsActive)
            .OrderBy(package => package.PackageName)
            .Select(package => new { package.Id, package.PackageName })
            .ToListAsync();

        var accounts = await _context.SalesAccounts
            .AsNoTracking()
            .Where(account => account.IsActive)
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
