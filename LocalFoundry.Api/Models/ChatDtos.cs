namespace LocalFoundry.Api.Models;

/// <summary>Request body for the chat and agent endpoints.</summary>
/// <param name="Message">The user's message to the model.</param>
public sealed record ChatRequest(string Message);

/// <summary>Response body for the non-streaming chat and agent endpoints.</summary>
/// <param name="Reply">The model's reply text.</param>
public sealed record ChatReply(string Reply);

/// <summary>A model available in the local Foundry catalog.</summary>
/// <param name="Alias">Short alias used with the <c>foundry model</c> CLI commands and as <c>FoundryLocal:ModelId</c>.</param>
/// <param name="Id">Full, hardware-specific model variant identifier.</param>
public sealed record FoundryModelSummary(string Alias, string Id);
