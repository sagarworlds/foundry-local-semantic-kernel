namespace LocalFoundry.Api.Endpoints;

/// <summary>Maps the service information endpoint.</summary>
public static class InfoEndpoints
{
    /// <summary>Maps <c>GET /api/info</c>.</summary>
    /// <param name="app">The route builder to add the endpoint to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapInfoEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/info", () => Results.Ok(new
        {
            service = "LocalFoundry.Api",
            description = "Sample .NET API combining Microsoft.Extensions.AI, Semantic Kernel, and Foundry Local.",
            endpoints = new[]
            {
                "POST /api/chat",
                "POST /api/chat/stream",
                "POST /api/agent",
                "GET /api/foundry/models",
            },
        }));

        return app;
    }
}
