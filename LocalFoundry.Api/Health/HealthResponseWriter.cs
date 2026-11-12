using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LocalFoundry.Api.Health;

/// <summary>
/// Writes health reports as JSON. The default writer only emits the overall status as plain
/// text, which doesn't say <em>what</em> is unhealthy.
/// </summary>
internal static class HealthResponseWriter
{
    public static Task WriteAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
            }),
        });
}
