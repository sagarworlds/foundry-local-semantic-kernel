namespace LocalFoundry.Api.Endpoints;

/// <summary>Maps every API endpoint group in one call.</summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>Maps the info, chat, agent and model catalog endpoints.</summary>
    /// <param name="app">The route builder to add the endpoints to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapLocalFoundryApi(this IEndpointRouteBuilder app) =>
        app.MapInfoEndpoints()
            .MapChatEndpoints()
            .MapAgentEndpoints()
            .MapFoundryModelEndpoints();
}
