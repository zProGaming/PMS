using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Groups;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.Groups;

public class IndexModel(ApplicationDbContext context, AuditLogService auditLogService) : PageModel
{
    public IList<GroupBooking> GroupBookings { get; private set; } = [];

    public SelectList SalesAccountOptions { get; private set; } = null!;

    [BindProperty]
    public GroupBooking Input { get; set; } = new()
    {
        ArrivalDate = DateTime.Today,
        DepartureDate = DateTime.Today.AddDays(1),
        BookingStatus = GroupBookingStatus.Inquiry
    };

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        Input.GroupCode = (Input.GroupCode ?? string.Empty).Trim().ToUpperInvariant();
        Input.GroupName = (Input.GroupName ?? string.Empty).Trim();
        Input.CreatedBy = User.Identity?.Name ?? "System";
        Input.CreatedAt = DateTime.Now;

        if (string.IsNullOrWhiteSpace(Input.GroupCode))
        {
            ModelState.AddModelError("Input.GroupCode", "Group code is required.");
        }

        if (string.IsNullOrWhiteSpace(Input.GroupName))
        {
            ModelState.AddModelError("Input.GroupName", "Group name is required.");
        }

        if (Input.DepartureDate.Date <= Input.ArrivalDate.Date)
        {
            ModelState.AddModelError("Input.DepartureDate", "Departure date must be after arrival date.");
        }

        if (Input.CreditLimit < 0 || Input.DepositAmount < 0)
        {
            ModelState.AddModelError(string.Empty, "Credit limit and deposit amount cannot be negative.");
        }

        if (await context.GroupBookings.AnyAsync(item => item.GroupCode == Input.GroupCode))
        {
            ModelState.AddModelError("Input.GroupCode", "Group code must be unique.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        context.GroupBookings.Add(Input);
        await context.SaveChangesAsync();
        await auditLogService.LogAsync(AuditActionType.Create, "Group Management", nameof(GroupBooking), Input.Id.ToString(), null, new { Input.GroupCode, Input.GroupName, Input.BookingStatus });
        return RedirectToPage("./Details", new { id = Input.Id });
    }

    private async Task LoadAsync()
    {
        GroupBookings = await context.GroupBookings
            .Include(item => item.SalesAccount)
            .Include(item => item.RoomBlocks)
            .Include(item => item.GroupFolios)
            .Include(item => item.Deposits)
            .AsNoTracking()
            .OrderByDescending(item => item.ArrivalDate)
            .ThenBy(item => item.GroupCode)
            .ToListAsync();

        var accounts = await context.SalesAccounts
            .AsNoTracking()
            .Where(item => item.IsActive)
            .OrderBy(item => item.AccountName)
            .Select(item => new { item.Id, item.AccountName })
            .ToListAsync();

        SalesAccountOptions = new SelectList(accounts, "Id", "AccountName", Input.SalesAccountId);
    }
}
