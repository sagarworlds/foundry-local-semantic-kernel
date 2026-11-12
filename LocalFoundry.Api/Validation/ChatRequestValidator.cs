using LocalFoundry.Api.Models;

namespace LocalFoundry.Api.Validation;

/// <summary>
/// Validates incoming <see cref="ChatRequest"/> payloads before they reach the model.
/// </summary>
public static class ChatRequestValidator
{
    /// <summary>
    /// Upper bound on the length of the message and of each history turn. Small local models
    /// have context windows of a few thousand tokens, so anything longer is almost certainly a
    /// mistake and would only waste inference time before failing or being truncated.
    /// </summary>
    public const int MaxMessageLength = 8_000;

    /// <summary>
    /// Upper bound on the number of history turns. Keeps a runaway client from sending an
    /// ever-growing transcript that the model's context window couldn't hold anyway.
    /// </summary>
    public const int MaxHistoryTurns = 50;

    /// <summary>Upper bound on the length of a model alias.</summary>
    public const int MaxModelLength = 100;

    /// <summary>
    /// Validates <paramref name="request"/>.
    /// </summary>
    /// <param name="request">The request to validate; may be <see langword="null"/> if the body was empty.</param>
    /// <returns>
    /// Validation errors keyed by field name (the shape expected by <c>Results.ValidationProblem</c>);
    /// empty when the request is valid.
    /// </returns>
    public static IDictionary<string, string[]> Validate(ChatRequest? request)
    {
        var errors = new Dictionary<string, string[]>();

        // The record's Message is non-nullable, but System.Text.Json still leaves it null when
        // the property is missing from the JSON body, so null must be checked explicitly.
        if (string.IsNullOrWhiteSpace(request?.Message))
        {
            errors[nameof(ChatRequest.Message)] = ["Message is required and must not be empty."];
        }
        else if (request.Message.Length > MaxMessageLength)
        {
            errors[nameof(ChatRequest.Message)] = [$"Message must be at most {MaxMessageLength} characters."];
        }

        var historyErrors = ValidateHistory(request?.History);
        if (historyErrors.Count > 0)
        {
            errors[nameof(ChatRequest.History)] = [.. historyErrors];
        }

        // null means "use the configured default"; an explicit value must look like a model alias.
        if (request?.Model is { } model && !IsValidModel(model))
        {
            errors[nameof(ChatRequest.Model)] =
                [$"Model must be 1-{MaxModelLength} characters of letters, digits, '.', '-', '_' or ':' (for example 'qwen2.5-0.5b')."];
        }

        return errors;
    }

    // Foundry Local aliases and variant ids only use these characters (e.g.
    // "qwen2.5-0.5b", "Phi-4-mini-instruct-generic-cpu:4"); rejecting anything else keeps
    // arbitrary text out of the upstream request.
    private static bool IsValidModel(string model) =>
        model.Length is > 0 and <= MaxModelLength
        && model.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' or ':');

    private static List<string> ValidateHistory(IReadOnlyList<ChatTurn>? history)
    {
        var errors = new List<string>();
        if (history is null)
        {
            return errors;
        }

        if (history.Count > MaxHistoryTurns)
        {
            // Don't also report every individual turn of an oversized transcript.
            errors.Add($"History must contain at most {MaxHistoryTurns} turns.");
            return errors;
        }

        for (var i = 0; i < history.Count; i++)
        {
            var turn = history[i];

            // A JSON null inside the array deserializes to a null element.
            if (turn is null)
            {
                errors.Add($"History[{i}] must not be null.");
                continue;
            }

            // "system" is deliberately rejected: letting callers inject system prompts would
            // let them override the agent's instructions.
            if (!ChatTurnRoles.IsValid(turn.Role))
            {
                errors.Add($"History[{i}].Role must be '{ChatTurnRoles.User}' or '{ChatTurnRoles.Assistant}'.");
            }

            if (string.IsNullOrWhiteSpace(turn.Content))
            {
                errors.Add($"History[{i}].Content is required and must not be empty.");
            }
            else if (turn.Content.Length > MaxMessageLength)
            {
                errors.Add($"History[{i}].Content must be at most {MaxMessageLength} characters.");
            }
        }

        return errors;
    }
}
