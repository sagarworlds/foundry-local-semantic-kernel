using LocalFoundry.Api.Services;

namespace LocalFoundry.Api.Endpoints;

/// <summary>Maps the Foundry Local model catalog endpoint.</summary>
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
        try
        {
            var models = await catalogService.ListModelsAsync(ct);
            return Results.Ok(models);
        }
        catch (Exception ex)
        {
            return Results.Problem(
                detail: $"Could not reach Foundry Local: {ex.Message}. Install it and run 'foundry service start' first.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
