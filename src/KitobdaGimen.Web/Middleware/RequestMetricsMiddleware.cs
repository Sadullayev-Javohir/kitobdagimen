using System.Diagnostics;
using KitobdaGimen.Web.Monitoring;
using Microsoft.AspNetCore.Routing;

namespace KitobdaGimen.Web.Middleware;

/// <summary>
/// Measures every routed request (latency + status code) and feeds it to <see cref="HttpMetrics"/>
/// for the admin monitoring dashboard. Placed after <c>UseRouting</c> so the matched route
/// template (e.g. <c>/posts/{id}</c>) is available for path normalization, keeping the
/// "busiest endpoints" list bounded instead of exploding per unique id.
/// </summary>
public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpMetrics _metrics;

    public RequestMetricsMiddleware(RequestDelegate next, HttpMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _metrics.Record(NormalizePath(context), context.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private static string NormalizePath(HttpContext context)
    {
        // Prefer the matched route template so "/posts/123" and "/posts/456" fold into "/posts/{id}".
        var endpoint = context.GetEndpoint();
        var routePattern = (endpoint as RouteEndpoint)?.RoutePattern.RawText;
        if (!string.IsNullOrEmpty(routePattern))
        {
            return "/" + routePattern.TrimStart('/');
        }

        var path = context.Request.Path.Value;
        return string.IsNullOrEmpty(path) ? "/" : path;
    }
}
