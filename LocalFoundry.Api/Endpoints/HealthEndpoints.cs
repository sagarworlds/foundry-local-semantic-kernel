using LocalFoundry.Api.Health;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace LocalFoundry.Api.Endpoints;

/// <summary>Maps the health endpoint.</summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps <c>GET /health</c>, which returns <c>200</c> when Foundry Local is reachable and
    /// <c>503</c> otherwise, with a JSON body describing each check.
    /// </summary>
    /// <param name="app">The route builder to add the endpoint to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = HealthResponseWriter.WriteAsync });
        return app;
    }
}
