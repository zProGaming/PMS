namespace Vantage.PMS.Services;

public class SystemErrorLoggingMiddleware(RequestDelegate next, ILogger<SystemErrorLoggingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<SystemErrorLoggingMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context, SystemErrorLogService errorLogService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            try
            {
                await errorLogService.LogExceptionAsync(exception);
            }
            catch (Exception loggingException)
            {
                _logger.LogError(loggingException, "Failed to write system error log.");
            }

            throw;
        }
    }
}
