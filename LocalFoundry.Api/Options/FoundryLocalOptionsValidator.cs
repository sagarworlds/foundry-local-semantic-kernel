using Microsoft.Extensions.Options;

namespace LocalFoundry.Api.Options;

/// <summary>
/// Validates <see cref="FoundryLocalOptions"/> so a misconfigured app fails at startup with a
/// clear message instead of on the first chat request.
/// </summary>
public sealed class FoundryLocalOptionsValidator : IValidateOptions<FoundryLocalOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, FoundryLocalOptions options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint)
            || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
        {
            failures.Add(
                $"FoundryLocal:Endpoint must be an absolute http(s) URL such as http://localhost:5273/v1, but was '{options.Endpoint}'.");
        }

        if (string.IsNullOrWhiteSpace(options.ModelId))
        {
            failures.Add("FoundryLocal:ModelId is required (the alias passed to 'foundry model load <alias>').");
        }

        // The OpenAI connector rejects an empty key even though Foundry Local ignores its value.
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            failures.Add("FoundryLocal:ApiKey must not be empty; any placeholder value works because Foundry Local ignores it.");
        }

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}
