using LocalFoundry.Api.Services;

namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Maps the Foundry Local model catalog endpoint.
/// Failures are translated to HTTP responses by the global exception handler.
/// </summary>
public static class FoundryModelEndpoints
{
    /// <summary>Maps <c>GET /api/foundry/models</c>.</summary>
    /// <param name="app">The route builder to add the endpoint to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapFoundryModelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/foundry/models", ListModelsAsync);
        return app;
    }

    private static async Task<IResult> ListModelsAsync(IModelCatalogService catalogService, CancellationToken ct)
    {
        var models = await catalogService.ListModelsAsync(ct);
        return Results.Ok(models);
    }
}
