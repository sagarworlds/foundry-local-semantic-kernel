using LocalFoundry.Api.Models;

namespace LocalFoundry.Api.Validation;

/// <summary>
/// Validates incoming <see cref="ChatRequest"/> payloads before they reach the model.
/// </summary>
public static class ChatRequestValidator
{
    /// <summary>
    /// Upper bound on message length. Small local models have context windows of a few
    /// thousand tokens, so anything longer is almost certainly a mistake and would only
    /// waste inference time before failing or being truncated.
    /// </summary>
    public const int MaxMessageLength = 8_000;

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

        return errors;
    }
}
