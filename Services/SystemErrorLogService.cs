using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class SystemErrorLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
{
    private readonly ApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task LogExceptionAsync(Exception exception, CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        _context.SystemErrorLogs.Add(new SystemErrorLog
        {
            ErrorDate = DateTime.Now,
            UserId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = httpContext?.User.Identity?.Name,
            Path = httpContext?.Request.Path.Value,
            ErrorMessage = exception.Message,
            StackTrace = exception.ToString(),
            Source = exception.Source,
            IsResolved = false
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkResolvedAsync(int id, string resolvedBy, string? notes, CancellationToken cancellationToken = default)
    {
        var error = await _context.SystemErrorLogs.FindAsync([id], cancellationToken);
        if (error is null)
        {
            return;
        }

        error.IsResolved = true;
        error.ResolvedBy = resolvedBy;
        error.ResolvedAt = DateTime.Now;
        error.Notes = notes;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
