using System.Net;
using System.Net.Sockets;
using LocalFoundry.Api.ErrorHandling;
using Microsoft.AI.Foundry.Local;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Tests;

public class FoundryLocalErrorMapperTests
{
    [Fact]
    public void TryMap_HttpRequestException_MapsTo503WithSetupHint()
    {
        var mapped = FoundryLocalErrorMapper.TryMap(new HttpRequestException("Connection refused (localhost:5273)"), out var error);

        Assert.True(mapped);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, error!.StatusCode);
        Assert.Contains("Connection refused", error.Detail);
        Assert.Contains("foundry server start", error.Detail);
    }

    [Fact]
    public void TryMap_WrappedConnectionFailureWithoutStatus_MapsTo503()
    {
        // Shape SK produces for transport failures: no StatusCode, socket error underneath.
        var exception = new HttpOperationException(
            "Connection refused",
            new HttpRequestException("Connection refused", new SocketException((int)SocketError.ConnectionRefused)));

        Assert.True(FoundryLocalErrorMapper.TryMap(exception, out var error));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, error!.StatusCode);
    }

    [Fact]
    public void TryMap_UpstreamErrorResponse_MapsTo502WithResponseContent()
    {
        var exception = new HttpOperationException(
            HttpStatusCode.NotFound,
            responseContent: "model qwen2.5-0.5b not loaded",
            message: "Not Found",
            innerException: null);

        Assert.True(FoundryLocalErrorMapper.TryMap(exception, out var error));
        Assert.Equal(StatusCodes.Status502BadGateway, error!.StatusCode);
        Assert.Contains("HTTP 404", error.Detail);
        Assert.Contains("model qwen2.5-0.5b not loaded", error.Detail);
    }

    [Fact]
    public void TryMap_FoundryLocalException_MapsTo503WithoutStackTrace()
    {
        var exception = new FoundryLocalException("Error getting models: connection refused\n   at Some.Frame()\n   at Other.Frame()");

        Assert.True(FoundryLocalErrorMapper.TryMap(exception, out var error));
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, error!.StatusCode);
        Assert.Contains("Error getting models: connection refused", error.Detail);
        Assert.DoesNotContain("Some.Frame", error.Detail);
    }

    [Theory]
    [MemberData(nameof(UnrelatedExceptions))]
    public void TryMap_UnrelatedException_IsNotMapped(Exception exception)
    {
        Assert.False(FoundryLocalErrorMapper.TryMap(exception, out var error));
        Assert.Null(error);
    }

    public static TheoryData<Exception> UnrelatedExceptions() =>
    [
        new InvalidOperationException("bug"),
        new NullReferenceException(),
        new HttpOperationException("no status and no transport failure underneath"),
    ];
}
