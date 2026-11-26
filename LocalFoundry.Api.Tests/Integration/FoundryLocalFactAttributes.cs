namespace LocalFoundry.Api.Tests.Integration;

/// <summary>
/// A test that needs a running Foundry Local with the model loaded. Skipped unless
/// <see cref="FoundryLocalIntegrationSettings.EnableVariable"/> is set.
/// </summary>
public sealed class FoundryLocalFactAttribute : FactAttribute
{
    public FoundryLocalFactAttribute()
    {
        if (!FoundryLocalIntegrationSettings.Enabled)
        {
            Skip = FoundryLocalFactSkipReasons.NotEnabled;
        }
    }
}

/// <summary>
/// A test whose outcome depends on the loaded model's capability, not only on this app working.
/// Skipped unless both <see cref="FoundryLocalIntegrationSettings.EnableVariable"/> and
/// <see cref="FoundryLocalIntegrationSettings.BehaviourVariable"/> are set.
/// </summary>
public sealed class FoundryLocalBehaviourFactAttribute : FactAttribute
{
    public FoundryLocalBehaviourFactAttribute()
    {
        if (!FoundryLocalIntegrationSettings.Enabled)
        {
            Skip = FoundryLocalFactSkipReasons.NotEnabled;
        }
        else if (!FoundryLocalIntegrationSettings.BehaviourEnabled)
        {
            Skip = $"Depends on the model's capability (small models often fail it); also set {FoundryLocalIntegrationSettings.BehaviourVariable}=1 to run.";
        }
    }
}

internal static class FoundryLocalFactSkipReasons
{
    public static readonly string NotEnabled =
        $"Needs a running Foundry Local with a model loaded; set {FoundryLocalIntegrationSettings.EnableVariable}=1 to run (see README).";
}
