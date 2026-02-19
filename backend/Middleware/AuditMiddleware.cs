using System.Security.Claims;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.Extensions.Options;

namespace IkOtomasyon.Api.Middleware;

public class AuditMiddleware
{
    private static readonly HashSet<string> MutatingMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST", "PUT", "PATCH", "DELETE"
    };

    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AuditOptions _options;

    public AuditMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, IOptions<AuditOptions> options)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!MutatingMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var actorId = GetUserId(context.User);
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var ip = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers.UserAgent.ToString();

        var statusCode = StatusCodes.Status500InternalServerError;
        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch
        {
            statusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var shouldLog = _options.LogUnauthorized || statusCode is not (StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden);
            if (shouldLog)
            {
                var auditEvents = context.GetAuditEvents();
                if (auditEvents.Count == 0)
                {
                    auditEvents = [new AuditContextExtensions.AuditEvent($"{method} {path}", null, null)];
                }

                foreach (var auditEvent in auditEvents)
                {
                    var log = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = actorId,
                        Method = method,
                        Path = path,
                        Action = auditEvent.Action,
                        Entity = auditEvent.Entity,
                        EntityId = auditEvent.EntityId,
                        StatusCode = statusCode,
                        Ip = ip,
                        UserAgent = userAgent,
                        CreatedAt = DateTime.UtcNow
                    };

                    db.AuditLogs.Add(log);
                }

                await db.SaveChangesAsync();
            }
        }
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
