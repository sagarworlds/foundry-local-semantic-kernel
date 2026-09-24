namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Builds the 503 problem responses returned when Foundry Local can't be reached.
/// </summary>
internal static class FoundryLocalProblems
{
    public static IResult Unavailable(Exception ex) =>
        Results.Problem(
            detail: UnavailableMessage(ex),
            statusCode: StatusCodes.Status503ServiceUnavailable);

    public static string UnavailableMessage(Exception ex) =>
        $"Could not reach the Foundry Local inference endpoint: {ex.Message}. " +
        "Run 'foundry model run <alias>' (or 'foundry service start') and check the FoundryLocal:Endpoint/ModelId " +
        "settings in appsettings.json match what's running. See README.md for setup steps.";
}
