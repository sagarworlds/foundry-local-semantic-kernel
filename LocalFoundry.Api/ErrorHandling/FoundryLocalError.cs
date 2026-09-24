namespace LocalFoundry.Api.ErrorHandling;

/// <summary>
/// An exception from the Foundry Local stack, translated into what the HTTP client should see.
/// </summary>
/// <param name="StatusCode">HTTP status code to return.</param>
/// <param name="Title">Short, human-readable summary of the problem.</param>
/// <param name="Detail">Actionable explanation, including how to fix the setup where applicable.</param>
public sealed record FoundryLocalError(int StatusCode, string Title, string Detail);
