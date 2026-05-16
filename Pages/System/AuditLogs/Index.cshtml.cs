using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Pages.System.AuditLogs;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private const int PageSize = 50;

    [BindProperty(SupportsGet = true)]
    public DateTime? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DateTo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? UserName { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Module { get; set; }

    [BindProperty(SupportsGet = true)]
    public AuditActionType? Action { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityName { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public int TotalPages { get; set; }

    public IList<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public IList<string> Modules { get; set; } = new List<string>();

    public async Task OnGetAsync()
    {
        CurrentPage = Math.Max(1, CurrentPage);
        Modules = await _context.AuditLogs
            .AsNoTracking()
            .Where(log => log.Module != "")
            .Select(log => log.Module)
            .Distinct()
            .OrderBy(module => module)
            .ToListAsync();

        var query = _context.AuditLogs.AsNoTracking();
        if (DateFrom is not null)
        {
            query = query.Where(log => log.CreatedAt >= DateFrom.Value.Date);
        }

        if (DateTo is not null)
        {
            query = query.Where(log => log.CreatedAt < DateTo.Value.Date.AddDays(1));
        }

        if (!string.IsNullOrWhiteSpace(UserName))
        {
            query = query.Where(log => log.UserName != null && log.UserName.Contains(UserName));
        }

        if (!string.IsNullOrWhiteSpace(Module))
        {
            query = query.Where(log => log.Module == Module);
        }

        if (Action is not null)
        {
            query = query.Where(log => log.Action == Action.Value);
        }

        if (!string.IsNullOrWhiteSpace(EntityName))
        {
            query = query.Where(log => log.EntityName == EntityName);
        }

        var totalRecords = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)PageSize));
        CurrentPage = Math.Min(CurrentPage, TotalPages);
        AuditLogs = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
