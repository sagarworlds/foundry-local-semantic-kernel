using LocalFoundry.Api.Options;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LocalFoundry.Api.Timeouts;

/// <summary>
/// Configures the request-timeout policy applied to endpoints that call Foundry Local, so a
/// hung model can't hold a request open indefinitely.
/// </summary>
public sealed class ModelRequestTimeout : IConfigureOptions<RequestTimeoutOptions>
{
    /// <summary>Name of the timeout policy; apply it to an endpoint with <c>WithRequestTimeout</c>.</summary>
    public const string PolicyName = "model-call";

    private readonly FoundryLocalOptions _options;

    /// <summary>Creates the configurator.</summary>
    /// <param name="options">Supplies <see cref="FoundryLocalOptions.RequestTimeoutSeconds"/>.</param>
    public ModelRequestTimeout(IOptions<FoundryLocalOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public void Configure(RequestTimeoutOptions options)
    {
        var seconds = _options.RequestTimeoutSeconds;

        options.AddPolicy(PolicyName, new RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(seconds),
            TimeoutStatusCode = StatusCodes.Status504GatewayTimeout,
            WriteTimeoutResponse = context => WriteProblemAsync(context, seconds),
        });
    }

    private static async Task WriteProblemAsync(HttpContext context, int seconds)
    {
        var problemDetails = context.RequestServices.GetRequiredService<IProblemDetailsService>();

        await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "Foundry Local timed out",
                Detail =
                    $"Foundry Local did not respond within {seconds} seconds. The model may still be loading or the " +
                    "prompt may be too large for this hardware. Retry, or raise FoundryLocal:RequestTimeoutSeconds in appsettings.json.",
            },
        });
    }
}
