namespace LocalFoundry.Api.Options;

/// <summary>
/// Connection settings for the Foundry Local inference endpoint, bound from the
/// <c>FoundryLocal</c> configuration section.
/// </summary>
public sealed class FoundryLocalOptions
{
    /// <summary>
    /// Base URL of Foundry Local's OpenAI-compatible API. The port must match the one passed to
    /// <c>foundry server start --port &lt;port&gt;</c>.
    /// </summary>
    public string Endpoint { get; set; } = "http://localhost:5273/v1";

    /// <summary>
    /// Model alias to send requests to. Must be loaded with <c>foundry model load &lt;alias&gt;</c>.
    /// </summary>
    public string ModelId { get; set; } = "qwen2.5-0.5b";

    /// <summary>
    /// API key sent with each request. Required by the OpenAI connector's API shape but ignored by Foundry Local.
    /// </summary>
    public string ApiKey { get; set; } = "not-needed";
}
