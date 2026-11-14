using System.ComponentModel.DataAnnotations;

namespace LocalFoundry.Api.Options;

/// <summary>
/// Limits for server-side conversation storage, bound from the <c>Conversations</c> configuration section.
/// </summary>
public sealed class ConversationOptions
{
    /// <summary>Name of the configuration section these options bind to.</summary>
    public const string SectionName = "Conversations";

    /// <summary>
    /// Maximum number of conversations kept at once. When a new one would exceed it, the
    /// least recently used conversation is discarded, which bounds memory use.
    /// </summary>
    [Range(1, 100_000, ErrorMessage = "Conversations:MaxConversations must be between 1 and 100000.")]
    public int MaxConversations { get; set; } = 200;

    /// <summary>Minutes a conversation may go unused before it is discarded.</summary>
    [Range(1, 525_600, ErrorMessage = "Conversations:IdleTimeoutMinutes must be between 1 and 525600 (one year).")]
    public int IdleTimeoutMinutes { get; set; } = 120;
}
