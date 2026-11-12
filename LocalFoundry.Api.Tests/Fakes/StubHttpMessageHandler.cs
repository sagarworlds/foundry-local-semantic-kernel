using System.Net;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>
/// Stands in for Foundry Local's HTTP endpoint: answers every request with
/// <see cref="StatusCode"/>, or throws <see cref="ExceptionToThrow"/>, and records the URLs requested.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public Exception? ExceptionToThrow { get; set; }

    public List<Uri?> RequestedUris { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUris.Add(request.RequestUri);

        return ExceptionToThrow is null
            ? Task.FromResult(new HttpResponseMessage(StatusCode) { Content = new StringContent("{\"data\":[]}") })
            : Task.FromException<HttpResponseMessage>(ExceptionToThrow);
    }
}
