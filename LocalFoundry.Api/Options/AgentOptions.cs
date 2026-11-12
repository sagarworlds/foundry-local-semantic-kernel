using System.ComponentModel.DataAnnotations;

namespace LocalFoundry.Api.Options;

/// <summary>
/// Settings for the tool-calling agent endpoint, bound from the <c>Agent</c> configuration section.
/// </summary>
public sealed class AgentOptions
{
    /// <summary>Name of the configuration section these options bind to.</summary>
    public const string SectionName = "Agent";

    /// <summary>
    /// Instructions placed at the start of every agent conversation, ahead of the caller's messages.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "Agent:SystemPrompt is required and must not be empty.")]
    public string SystemPrompt { get; set; } = "You are a helpful assistant. Use tools when they help answer the question.";
}
