using LocalFoundry.Api.Models;
using LocalFoundry.Api.Validation;
using LocalFoundry.Api.Services;
using LocalFoundry.Api.Timeouts;

namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Maps the tool-calling agent endpoint.
/// Failures are translated to HTTP responses by the global exception handler.
/// </summary>
public static class AgentEndpoints
{
    /// <summary>Maps <c>POST /api/agent</c>.</summary>
    /// <param name="app">The route builder to add the endpoint to.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/agent", AskAgentAsync)
            .AddEndpointFilter<ChatRequestValidationFilter>()
            .WithRequestTimeout(ModelRequestTimeout.PolicyName);
        return app;
    }

    private static async Task<IResult> AskAgentAsync(ChatRequest request, IAgentService agent, CancellationToken ct)
    {
        var reply = await agent.AskAsync(request.ToChatMessages(), request.Model, ct);
        return Results.Ok(new ChatReply(reply));
    }
}
