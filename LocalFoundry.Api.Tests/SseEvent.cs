using System.Text.Json;

namespace LocalFoundry.Api.Tests;

/// <summary>A parsed Server-Sent Event, for asserting on the streaming endpoint's output.</summary>
public sealed record SseEvent(string Name, string Data)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Deserializes the event's JSON data.</summary>
    public T Read<T>() => JsonSerializer.Deserialize<T>(Data, JsonOptions)!;

    /// <summary>Parses a complete <c>text/event-stream</c> body into its events.</summary>
    public static IReadOnlyList<SseEvent> Parse(string body) =>
        body.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(block => block.Split('\n'))
            .Select(lines => new SseEvent(
                Name: lines.Single(l => l.StartsWith("event: ", StringComparison.Ordinal))["event: ".Length..],
                Data: lines.Single(l => l.StartsWith("data: ", StringComparison.Ordinal))["data: ".Length..]))
            .ToList();
}
