namespace LocalFoundry.Api.Tests.Integration;

/// <summary>
/// Opt-in switches and overrides for the tests that talk to a real Foundry Local, read from
/// environment variables so a plain <c>dotnet test</c> (and CI) never needs Foundry Local.
/// </summary>
public static class FoundryLocalIntegrationSettings
{
    /// <summary>Set to <c>1</c> or <c>true</c> to run the integration tests.</summary>
    public const string EnableVariable = "FOUNDRY_LOCAL_TESTS";

    /// <summary>
    /// Also set to <c>1</c> or <c>true</c> to run the tests whose outcome depends on how capable the
    /// loaded model is (remembering an earlier turn, calling a tool). Small models fail these
    /// unpredictably, so they are a separate opt-in.
    /// </summary>
    public const string BehaviourVariable = "FOUNDRY_LOCAL_TESTS_BEHAVIOUR";

    /// <summary>Overrides the Foundry Local endpoint (default: the app's configured one, <c>http://localhost:5273/v1</c>).</summary>
    public const string EndpointVariable = "FOUNDRY_LOCAL_TESTS_ENDPOINT";

    /// <summary>Overrides the model alias to test with (default: the app's configured one, <c>qwen2.5-0.5b</c>).</summary>
    public const string ModelVariable = "FOUNDRY_LOCAL_TESTS_MODEL";

    /// <summary>Whether the integration tests were switched on.</summary>
    public static bool Enabled => IsOn(EnableVariable);

    /// <summary>Whether the model-capability-dependent tests were switched on as well.</summary>
    public static bool BehaviourEnabled => Enabled && IsOn(BehaviourVariable);

    /// <summary>The endpoint override, or <see langword="null"/> to use the app's configured endpoint.</summary>
    public static string? EndpointOverride => Environment.GetEnvironmentVariable(EndpointVariable) is { Length: > 0 } value ? value : null;

    /// <summary>The model override, or <see langword="null"/> to use the app's configured model.</summary>
    public static string? ModelOverride => Environment.GetEnvironmentVariable(ModelVariable) is { Length: > 0 } value ? value : null;

    private static bool IsOn(string variable) =>
        Environment.GetEnvironmentVariable(variable) is { } value
        && (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase));
}
