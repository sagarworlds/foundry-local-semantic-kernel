using System.Text.Json;

namespace LocalFoundry.Api.Endpoints;

/// <summary>
/// Writes Server-Sent Events (<c>text/event-stream</c>) to an HTTP response, flushing after each
/// event so clients receive it immediately.
/// </summary>
internal sealed class ServerSentEventWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpResponse _response;

    public ServerSentEventWriter(HttpResponse response)
    {
        _response = response;
    }

    /// <summary>Sets the SSE response headers. Must be called before the first event.</summary>
    public void Start()
    {
        _response.ContentType = "text/event-stream";
        _response.Headers.CacheControl = "no-cache";
        // Stops nginx-style reverse proxies from buffering the stream into one chunk.
        _response.Headers["X-Accel-Buffering"] = "no";
    }

    /// <summary>Writes one event whose data is <paramref name="payload"/> serialized as JSON.</summary>
    public async Task WriteAsync<T>(string eventName, T payload, CancellationToken cancellationToken)
    {
        // Serialized JSON never contains raw newlines, so it always fits the single
        // "data:" line that the SSE framing requires.
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await _response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", cancellationToken);
        await _response.Body.FlushAsync(cancellationToken);
    }
}
