using System.Net;
using System.Net.Http.Json;
using LocalFoundry.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Tests;

/// <summary>End-to-end tests of server-side conversations through the real HTTP pipeline.</summary>
public sealed class ConversationEndpointTests : IDisposable
{
    private readonly LocalFoundryApiFactory _factory = new();
    private readonly HttpClient _client;

    public ConversationEndpointTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<string> CreateConversationAsync()
    {
        var response = await _client.PostAsync("/api/conversations", content: null);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<ConversationCreated>();
        Assert.Equal($"/api/conversations/{created!.Id}", response.Headers.Location!.OriginalString);
        return created.Id;
    }

    private async Task<IReadOnlyList<ChatTurn>> GetTurnsAsync(string id) =>
        (await _client.GetFromJsonAsync<ConversationTranscript>($"/api/conversations/{id}"))!.Turns;

    [Fact]
    public async Task Chat_WithConversationId_SendsStoredHistoryAndRecordsTheExchange()
    {
        var id = await CreateConversationAsync();
        _factory.ChatClient.Chunks = ["Paris"];
        var first = await _client.PostAsJsonAsync("/api/chat", new ChatRequest("Capital of France?", ConversationId: id));
        _factory.ChatClient.Chunks = ["Rome"];

        var second = await _client.PostAsJsonAsync("/api/chat", new ChatRequest("And of Italy?", ConversationId: id));

        Assert.Equal(id, (await first.Content.ReadFromJsonAsync<ChatReply>())!.ConversationId);
        Assert.Equal("Rome", (await second.Content.ReadFromJsonAsync<ChatReply>())!.Reply);
        // The second request carried only the new message; the server supplied the rest.
        Assert.Equal(
            [(ChatRole.User, "Capital of France?"), (ChatRole.Assistant, "Paris"), (ChatRole.User, "And of Italy?")],
            _factory.ChatClient.LastMessages.Select(m => (m.Role, m.Text)));
        Assert.Equal(
            [new ChatTurn("user", "Capital of France?"), new ChatTurn("assistant", "Paris"), new ChatTurn("user", "And of Italy?"), new ChatTurn("assistant", "Rome")],
            await GetTurnsAsync(id));
    }

    [Theory]
    [InlineData("/api/chat/stream", "Hello there!")]
    [InlineData("/api/agent", "It is noon.")]
    [InlineData("/api/agent/stream", "It is noon. ")]
    public async Task OtherEndpoints_WithConversationId_RecordTheCompletedExchange(string path, string expectedReply)
    {
        var id = await CreateConversationAsync();
        _factory.ChatClient.Chunks = ["Hello", " there", "!"];

        var response = await _client.PostAsJsonAsync(path, new ChatRequest("hi", ConversationId: id));
        await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal([new ChatTurn("user", "hi"), new ChatTurn("assistant", expectedReply)], await GetTurnsAsync(id));
    }

    [Fact]
    public async Task ConversationsAreSharedAcrossChatAndAgentEndpoints()
    {
        var id = await CreateConversationAsync();
        await _client.PostAsJsonAsync("/api/chat", new ChatRequest("first", ConversationId: id));

        await _client.PostAsJsonAsync("/api/agent", new ChatRequest("second", ConversationId: id));

        Assert.Equal(3, _factory.Agent.LastMessages.Count);
    }

    [Fact]
    public async Task ChatStream_FailingMidStream_DoesNotRecordThePartialReply()
    {
        var id = await CreateConversationAsync();
        _factory.ChatClient.ExceptionAfterChunks = new HttpRequestException("Connection reset");

        var response = await _client.PostAsJsonAsync("/api/chat/stream", new ChatRequest("hi", ConversationId: id));
        await response.Content.ReadAsStringAsync();

        Assert.Empty(await GetTurnsAsync(id));
    }

    [Fact]
    public async Task Chat_FailingBeforeAReply_DoesNotRecordTheMessage()
    {
        var id = await CreateConversationAsync();
        _factory.ChatClient.ExceptionToThrow = new HttpRequestException("Connection refused");

        var response = await _client.PostAsJsonAsync("/api/chat", new ChatRequest("hi", ConversationId: id));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Empty(await GetTurnsAsync(id));
    }

    [Theory]
    [InlineData("/api/chat")]
    [InlineData("/api/chat/stream")]
    [InlineData("/api/agent")]
    [InlineData("/api/agent/stream")]
    public async Task Endpoints_WithUnknownConversationId_Return404WithoutCallingTheModel(string path)
    {
        var response = await _client.PostAsJsonAsync(path, new ChatRequest("hi", ConversationId: "0123456789abcdef0123456789abcdef"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Conversation not found", problem!.Title);
        Assert.Equal(0, _factory.ChatClient.CallCount);
        Assert.Empty(_factory.Agent.LastMessages);
    }

    [Fact]
    public async Task Chat_WithConversationIdAndHistory_Returns400()
    {
        var id = await CreateConversationAsync();
        var request = new ChatRequest("hi", [new ChatTurn("user", "earlier")], ConversationId: id);

        var response = await _client.PostAsJsonAsync("/api/chat", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(ChatRequest.ConversationId), problem!.Errors.Keys);
    }

    [Fact]
    public async Task Chat_WithMalformedConversationId_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/chat", new ChatRequest("hi", ConversationId: "../etc"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RemovesTheConversation()
    {
        var id = await CreateConversationAsync();

        var deleted = await _client.DeleteAsync($"/api/conversations/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/conversations/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/conversations/{id}")).StatusCode);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("bad%20id")]
    public async Task Get_UnknownOrMalformedId_Returns404Problem(string id)
    {
        var response = await _client.GetAsync($"/api/conversations/{id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }
}
