using System.Net;
using System.Net.Http.Json;
using LocalFoundry.Api.Models;

namespace LocalFoundry.Api.Tests.Integration;

/// <summary>
/// Runs the app against a real Foundry Local, to check the assumptions the fake-backed tests can't:
/// the real model list, the real error responses, the real streaming format. Opt-in: see
/// <see cref="FoundryLocalIntegrationSettings"/>.
/// </summary>
/// <remarks>
/// Assertions avoid depending on what the model says, because that varies run to run. The tests
/// that do depend on it use <see cref="FoundryLocalBehaviourFactAttribute"/>.
/// </remarks>
public sealed class FoundryLocalIntegrationTests(FoundryLocalFixture foundry) : IClassFixture<FoundryLocalFixture>
{
    private HttpClient Client => foundry.Client;

    [FoundryLocalFact]
    public async Task Health_ReportsTheConfiguredModelAsLoaded()
    {
        var response = await Client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        // "Degraded" means Foundry Local answered but no loaded model id matched the alias:
        // either the model isn't loaded, or the alias-prefix match in the health check is wrong
        // for this Foundry Local version. The body lists what it did see.
        Assert.True(
            response.StatusCode == HttpStatusCode.OK && body.Contains("\"status\":\"Healthy\""),
            $"Expected Healthy for model '{foundry.Model}', got HTTP {(int)response.StatusCode}: {body}");
    }

    [FoundryLocalFact]
    public async Task Models_ListsTheConfiguredModel()
    {
        var response = await Client.GetAsync("/api/foundry/models");
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)response.StatusCode}: {body}");

        var models = await response.Content.ReadFromJsonAsync<List<FoundryModelSummary>>();

        Assert.Contains(models!, model => string.Equals(model.Alias, foundry.Model, StringComparison.OrdinalIgnoreCase));
    }

    [FoundryLocalFact]
    public async Task Chat_ReturnsAReply()
    {
        var response = await Client.PostAsJsonAsync("/api/chat", new ChatRequest("Reply with the single word: pong"));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)response.StatusCode}: {body}");

        var reply = System.Text.Json.JsonSerializer.Deserialize<ChatReply>(body, JsonOptions);

        Assert.False(string.IsNullOrWhiteSpace(reply!.Reply), "The model returned an empty reply.");
    }

    [FoundryLocalFact]
    public async Task ChatStream_SendsTokensThenDone()
    {
        var response = await Client.PostAsJsonAsync("/api/chat/stream", new ChatRequest("Count from one to five."));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)response.StatusCode}: {body}");
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType!.MediaType);

        var events = SseEvent.Parse(body);

        Assert.True(events.Count > 1, $"Expected token events followed by done, got: {body}");
        Assert.Equal("done", events[^1].Name);

        var tokens = events.SkipLast(1).ToList();
        Assert.All(tokens, e => Assert.Equal("token", e.Name));
        Assert.False(string.IsNullOrWhiteSpace(string.Concat(tokens.Select(e => e.Read<ChatStreamToken>().Text))));
    }

    [FoundryLocalFact]
    public async Task Chat_NamingTheConfiguredModelExplicitly_Succeeds()
    {
        // Checks that Foundry Local accepts the alias in the request's model field.
        var response = await Client.PostAsJsonAsync("/api/chat", new ChatRequest("Say hello.", Model: foundry.Model));
        var body = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)response.StatusCode}: {body}");
    }

    [FoundryLocalFact]
    public async Task Chat_NamingAnUnknownModel_Returns502WithFoundryLocalsMessage()
    {
        var response = await Client.PostAsJsonAsync("/api/chat", new ChatRequest("hi", Model: "no-such-model-for-integration-tests"));
        var body = await response.Content.ReadAsStringAsync();

        // Whatever status Foundry Local uses for an unknown model, the app should report it as a
        // bad gateway rather than hiding it as a 500 or pretending Foundry Local is down.
        Assert.True(response.StatusCode == HttpStatusCode.BadGateway, $"Expected 502, got HTTP {(int)response.StatusCode}: {body}");
        Assert.Contains("Foundry Local returned an error", body);
    }

    [FoundryLocalFact]
    public async Task Conversation_RecordsEachExchange()
    {
        var id = (await (await Client.PostAsync("/api/conversations", content: null)).Content.ReadFromJsonAsync<ConversationCreated>())!.Id;

        await PostChatAsync(new ChatRequest("My favourite colour is purple. Reply with OK.", ConversationId: id));
        await PostChatAsync(new ChatRequest("What is my favourite colour?", ConversationId: id));

        var transcript = await Client.GetFromJsonAsync<ConversationTranscript>($"/api/conversations/{id}");
        Assert.Equal(["user", "assistant", "user", "assistant"], transcript!.Turns.Select(t => t.Role));
    }

    [FoundryLocalBehaviourFact]
    public async Task Conversation_FollowUpRecallsAnEarlierTurn()
    {
        var id = (await (await Client.PostAsync("/api/conversations", content: null)).Content.ReadFromJsonAsync<ConversationCreated>())!.Id;
        await PostChatAsync(new ChatRequest("My favourite colour is purple. Reply with OK.", ConversationId: id));

        var reply = await PostChatAsync(new ChatRequest("What is my favourite colour? Answer in one word.", ConversationId: id));

        Assert.Contains("purple", reply, StringComparison.OrdinalIgnoreCase);
    }

    [FoundryLocalBehaviourFact]
    public async Task Agent_UsesTheCalculatorTool()
    {
        var response = await Client.PostAsJsonAsync("/api/agent", new ChatRequest("What is 17 times 23? Use the calculator tool."));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)response.StatusCode}: {body}");

        var reply = System.Text.Json.JsonSerializer.Deserialize<ChatReply>(body, JsonOptions)!.Reply;

        Assert.Contains("391", reply);
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);

    private async Task<string> PostChatAsync(ChatRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/chat", request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"HTTP {(int)response.StatusCode}: {body}");
        return System.Text.Json.JsonSerializer.Deserialize<ChatReply>(body, JsonOptions)!.Reply;
    }
}
