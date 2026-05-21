using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Authorization;
using Vantage.PMS.Data;
using Vantage.PMS.Models.Core;

namespace Vantage.PMS.Pages.Admin.CompanyAccess;

public class IndexModel(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager) : PageModel
{
    public IList<CompanyAccessRow> AccessRows { get; private set; } = new List<CompanyAccessRow>();

    public SelectList UserOptions { get; private set; } = default!;

    public SelectList HotelOptions { get; private set; } = default!;

    public SelectList RoleOptions { get; private set; } = default!;

    [BindProperty]
    public CompanyAccessInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostGrantAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        var user = await userManager.FindByIdAsync(Input.UserId);
        var hotel = await context.Hotels.FindAsync(Input.HotelId);
        if (user is null || hotel is null)
        {
            ModelState.AddModelError(string.Empty, "Select a valid user and company.");
            await LoadAsync();
            return Page();
        }

        var existing = await context.HotelUserAccesses
            .FirstOrDefaultAsync(access => access.UserId == Input.UserId && access.HotelId == Input.HotelId);

        if (Input.IsDefaultCompany)
        {
            var existingDefaults = await context.HotelUserAccesses
                .Where(access => access.UserId == Input.UserId && access.IsDefaultCompany)
                .ToListAsync();
            foreach (var item in existingDefaults)
            {
                item.IsDefaultCompany = false;
            }
        }

        if (existing is null)
        {
            context.HotelUserAccesses.Add(new HotelUserAccess
            {
                UserId = Input.UserId,
                HotelId = Input.HotelId,
                RoleName = Input.RoleName,
                IsDefaultCompany = Input.IsDefaultCompany,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "SystemAdmin"
            });
        }
        else
        {
            existing.RoleName = Input.RoleName;
            existing.IsDefaultCompany = Input.IsDefaultCompany;
            existing.IsActive = true;
        }

        await context.SaveChangesAsync();
        StatusMessage = $"Company access granted for {user.Email} to {hotel.Code}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var access = await context.HotelUserAccesses.FindAsync(id);
        if (access is null)
        {
            return RedirectToPage();
        }

        access.IsActive = false;
        access.IsDefaultCompany = false;
        await context.SaveChangesAsync();
        StatusMessage = "Company access deactivated.";
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var users = await userManager.Users
            .OrderBy(user => user.Email)
            .Select(user => new { user.Id, Label = user.Email ?? user.UserName ?? user.Id })
            .ToListAsync();

        var hotels = await context.Hotels
            .AsNoTracking()
            .OrderBy(hotel => hotel.Code)
            .Select(hotel => new { hotel.Id, Label = hotel.Code + " - " + hotel.Name })
            .ToListAsync();

        var userLookup = users.ToDictionary(user => user.Id, user => user.Label);

        AccessRows = await context.HotelUserAccesses
            .AsNoTracking()
            .Include(access => access.Hotel)
            .OrderBy(access => access.Hotel!.Code)
            .ThenBy(access => access.RoleName)
            .Select(access => new CompanyAccessRow
            {
                Id = access.Id,
                UserId = access.UserId,
                HotelCode = access.Hotel == null ? "-" : access.Hotel.Code,
                HotelName = access.Hotel == null ? "Company not found" : access.Hotel.Name,
                RoleName = access.RoleName,
                IsDefaultCompany = access.IsDefaultCompany,
                IsActive = access.IsActive,
                CreatedAt = access.CreatedAt
            })
            .ToListAsync();

        foreach (var row in AccessRows)
        {
            row.UserEmail = userLookup.TryGetValue(row.UserId, out var email) ? email : row.UserId;
        }

        UserOptions = new SelectList(users, "Id", "Label", Input.UserId);
        HotelOptions = new SelectList(hotels, "Id", "Label", Input.HotelId);
        RoleOptions = new SelectList(PmsRoles.All, Input.RoleName);
    }

    public class CompanyAccessInput
    {
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int HotelId { get; set; }

        [Required]
        public string RoleName { get; set; } = PmsRoles.FrontDesk;

        public bool IsDefaultCompany { get; set; } = true;
    }

    public class CompanyAccessRow
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public string HotelCode { get; set; } = string.Empty;

        public string HotelName { get; set; } = string.Empty;

        public string? RoleName { get; set; }

        public bool IsDefaultCompany { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
