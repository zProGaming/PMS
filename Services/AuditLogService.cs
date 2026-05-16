using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Vantage.PMS.Data;
using Vantage.PMS.Models.SystemAdministration;

namespace Vantage.PMS.Services;

public class AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
{
    private readonly ApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task LogAsync(
        AuditActionType action,
        string module,
        string entityName,
        string? entityId,
        object? oldValues,
        object? newValues,
        string? overrideUserName = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        _context.AuditLogs.Add(new AuditLog
        {
            UserId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = overrideUserName ?? httpContext?.User.Identity?.Name ?? "System",
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues is null ? null : JsonSerializer.Serialize(Sanitize(oldValues), JsonOptions),
            NewValues = newValues is null ? null : JsonSerializer.Serialize(Sanitize(newValues), JsonOptions),
            IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request.Headers.UserAgent.ToString(),
            CreatedAt = DateTime.Now,
            Module = module
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static object Sanitize(object value)
    {
        if (value is IReadOnlyDictionary<string, object?> dictionary)
        {
            return dictionary.ToDictionary(pair => pair.Key, pair => MaskIfSensitive(pair.Key, pair.Value));
        }

        if (value is IDictionary<string, object?> mutableDictionary)
        {
            return mutableDictionary.ToDictionary(pair => pair.Key, pair => MaskIfSensitive(pair.Key, pair.Value));
        }

        return value;
    }

    private static object? MaskIfSensitive(string key, object? value)
    {
        var normalized = key.ToUpperInvariant();
        return normalized.Contains("PASSWORD") || normalized.Contains("APIKEY") || normalized.Contains("SECRET") || normalized.Contains("TOKEN")
            ? "***"
            : value;
    }
}
