using System.Diagnostics;
using System.Security.Claims;
using IkOtomasyon.Api.Services;

namespace IkOtomasyon.Api.Middleware;

public class RequestObservabilityMiddleware
{
    private const string CorrelationHeader = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestObservabilityMiddleware> _logger;
    private readonly AppMetrics _metrics;

    public RequestObservabilityMiddleware(
        RequestDelegate next,
        ILogger<RequestObservabilityMiddleware> logger,
        AppMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(CorrelationHeader, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString().Trim()
                : Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationHeader] = correlationId;
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
            var route = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? string.Empty;
            var statusCode = context.Response.StatusCode;
            var elapsedMs = sw.Elapsed.TotalMilliseconds;

            using (_logger.BeginScope(new Dictionary<string, object?>
            {
                ["correlationId"] = correlationId,
                ["userId"] = userId
            }))
            {
                _logger.LogInformation(
                    "HTTP request completed method={Method} route={Route} path={Path} status={StatusCode} latencyMs={LatencyMs}",
                    context.Request.Method,
                    route,
                    context.Request.Path.Value ?? string.Empty,
                    statusCode,
                    elapsedMs);
            }

            _metrics.RecordRequest(
                context.Request.Method,
                route,
                statusCode,
                elapsedMs);
        }
    }
}
