namespace LocalFoundry.Api.Models;

public sealed record ChatRequest(string Message);

public sealed record ChatReply(string Reply);

public sealed record FoundryModelSummary(string Alias, string Id);
