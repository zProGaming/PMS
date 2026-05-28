using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;

namespace Vantage.PMS.Pages.GuestPortalManagement.Accesses;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public IList<GuestPortalAccessRow> Accesses { get; set; } = new List<GuestPortalAccessRow>();

    public async Task OnGetAsync()
    {
        Accesses = await _context.GuestPortalAccesses
            .Include(access => access.Reservation)
            .Include(access => access.BookingRequest)
            .AsNoTracking()
            .OrderByDescending(access => access.CreatedAt)
            .Select(access => new GuestPortalAccessRow
            {
                Id = access.Id,
                MaskedToken = access.AccessToken.Length <= 8
                    ? "Suppressed"
                    : access.AccessToken.Substring(0, 4) + "..." + access.AccessToken.Substring(access.AccessToken.Length - 4),
                ReservationNumber = access.Reservation == null ? null : access.Reservation.ConfirmationNumber,
                BookingReference = access.BookingRequest == null ? null : access.BookingRequest.BookingReference,
                GuestEmail = access.GuestEmail,
                GuestPhone = access.GuestPhone,
                IsActive = access.IsActive,
                ExpiresAt = access.ExpiresAt,
                LastAccessedAt = access.LastAccessedAt
            })
            .ToListAsync();
    }

    public class GuestPortalAccessRow
    {
        public int Id { get; set; }

        public string MaskedToken { get; set; } = string.Empty;

        public string? ReservationNumber { get; set; }

        public string? BookingReference { get; set; }

        public string? GuestEmail { get; set; }

        public string? GuestPhone { get; set; }

        public bool IsActive { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public DateTime? LastAccessedAt { get; set; }
    }
}
