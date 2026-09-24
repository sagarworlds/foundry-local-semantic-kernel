using System.Net;
using System.Net.Http.Json;
using LocalFoundry.Api.Models;
using Microsoft.AI.Foundry.Local;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Tests;

/// <summary>
/// End-to-end HTTP tests. A fresh factory per test keeps the scriptable fakes isolated.
/// </summary>
public sealed class ApiEndpointTests : IDisposable
{
    private readonly LocalFoundryApiFactory _factory = new();
    private readonly HttpClient _client;

    public ApiEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Info_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Chat_ValidMessage_ReturnsModelReply()
    {
        var response = await _client.PostAsJsonAsync("/api/chat", new ChatRequest("Capital of France?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reply = await response.Content.ReadFromJsonAsync<ChatReply>();
        Assert.Equal("Paris", reply!.Reply);
    }

    [Theory]
    [InlineData("/api/chat")]
    [InlineData("/api/chat/stream")]
    [InlineData("/api/agent")]
    public async Task ChatEndpoints_BlankMessage_Return400WithoutCallingModel(string path)
    {
        var response = await _client.PostAsJsonAsync(path, new ChatRequest("  "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(ChatRequest.Message), problem!.Errors.Keys);
        Assert.Equal(0, _factory.ChatClient.CallCount);
    }

    [Fact]
    public async Task Chat_FoundryLocalUnreachable_Returns503Problem()
    {
        _factory.ChatClient.ExceptionToThrow = new HttpRequestException("Connection refused (localhost:5273)");

        var response = await _client.PostAsJsonAsync("/api/chat", new ChatRequest("hi"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Contains("Connection refused", problem!.Detail);
    }

    [Fact]
    public async Task Chat_UpstreamErrorResponse_Returns502Problem()
    {
        _factory.ChatClient.ExceptionToThrow =
            new HttpOperationException(HttpStatusCode.NotFound, "model not loaded", "Not Found", innerException: null);

        var response = await _client.PostAsJsonAsync("/api/chat", new ChatRequest("hi"));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Chat_UnexpectedException_Returns500NotDisguisedAsOutage()
    {
        _factory.ChatClient.ExceptionToThrow = new InvalidOperationException("bug");

        var response = await _client.PostAsJsonAsync("/api/chat", new ChatRequest("hi"));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task ChatStream_ValidMessage_StreamsAllChunks()
    {
        _factory.ChatClient.Chunks = ["Hello", " there", "!"];

        var response = await _client.PostAsJsonAsync("/api/chat/stream", new ChatRequest("hi"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("Hello there!", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ChatStream_FailureBeforeFirstChunk_Returns503Problem()
    {
        _factory.ChatClient.ExceptionToThrow = new HttpRequestException("Connection refused");

        var response = await _client.PostAsJsonAsync("/api/chat/stream", new ChatRequest("hi"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Agent_ValidMessage_ReturnsAgentReply()
    {
        var response = await _client.PostAsJsonAsync("/api/agent", new ChatRequest("What time is it?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reply = await response.Content.ReadFromJsonAsync<ChatReply>();
        Assert.Equal("It is noon.", reply!.Reply);
    }

    [Fact]
    public async Task Models_ReturnsCatalog()
    {
        var models = await _client.GetFromJsonAsync<List<FoundryModelSummary>>("/api/foundry/models");

        var model = Assert.Single(models!);
        Assert.Equal("qwen2.5-0.5b", model.Alias);
    }

    [Fact]
    public async Task Models_FoundryLocalException_Returns503Problem()
    {
        _factory.Catalog.ExceptionToThrow = new FoundryLocalException("Error getting models\n   at Frame()");

        var response = await _client.GetAsync("/api/foundry/models");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.DoesNotContain("Frame()", problem!.Detail);
    }
}
