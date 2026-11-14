using System.Net;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>
/// Stands in for Foundry Local's HTTP endpoint: answers every request with
/// <see cref="StatusCode"/>, or throws <see cref="ExceptionToThrow"/>, and records the URLs requested.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    /// <summary>Response body; defaults to an OpenAI model list containing a variant of the default model.</summary>
    public string Body { get; set; } = """{"object":"list","data":[{"id":"qwen2.5-0.5b-instruct-generic-cpu:4"}]}""";

    public Exception? ExceptionToThrow { get; set; }

    public List<Uri?> RequestedUris { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUris.Add(request.RequestUri);

        return ExceptionToThrow is null
            ? Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent(Body, System.Text.Encoding.UTF8, "application/json") })
            : Task.FromException<HttpResponseMessage>(ExceptionToThrow);
    }
}
