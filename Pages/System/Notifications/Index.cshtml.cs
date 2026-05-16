using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;
using Vantage.PMS.Services;

namespace Vantage.PMS.Pages.System.Notifications;

public class IndexModel(ApplicationDbContext context, SystemNotificationService notificationService) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly SystemNotificationService _notificationService = notificationService;

    public IList<SystemNotification> Notifications { get; set; } = new List<SystemNotification>();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        var count = await _notificationService.GenerateOperationalNotificationsAsync();
        StatusMessage = $"Notification scan completed. New notifications created: {count}.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int id)
    {
        await _notificationService.MarkReadAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), UserRoles());
        return RedirectToPage();
    }

    private async Task LoadAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = UserRoles().ToList();
        var notifications = await _context.SystemNotifications
            .AsNoTracking()
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(250)
            .ToListAsync();

        Notifications = notifications
            .Where(notification => SystemNotificationService.CanView(notification, userId, roles))
            .ToList();
    }

    private IEnumerable<string> UserRoles()
    {
        return User.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value);
    }
}
