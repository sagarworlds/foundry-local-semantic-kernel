namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Shared OpenAPI metadata for endpoints that call Foundry Local, so every such endpoint
/// documents the same failure responses the global exception handler and timeout policy produce.
/// </summary>
internal static class OpenApiConventions
{
    /// <summary>Documents the 502/503/504 ProblemDetails responses of a model-calling endpoint.</summary>
    public static TBuilder ProducesFoundryLocalProblems<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(
            new ProducesResponseTypeMetadata(StatusCodes.Status502BadGateway, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), ["application/problem+json"]),
            new ProducesResponseTypeMetadata(StatusCodes.Status503ServiceUnavailable, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), ["application/problem+json"]),
            new ProducesResponseTypeMetadata(StatusCodes.Status504GatewayTimeout, typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), ["application/problem+json"]));
        return builder;
    }
}
