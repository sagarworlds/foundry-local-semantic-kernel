using LocalFoundry.Api.Services;
using LocalFoundry.Api.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace LocalFoundry.Api.Tests;

public class ModelRoutingChatClientTests
{
    private readonly FakeKernelProvider _kernels = new();
    private readonly ModelRoutingChatClient _client;

    public ModelRoutingChatClientTests()
    {
        _client = new ModelRoutingChatClient(_kernels);
    }

    [Fact]
    public async Task GetResponseAsync_WithModel_UsesTheKernelForThatModel()
    {
        var response = await _client.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hi")], new ChatOptions { ModelId = "phi-4-mini" });

        Assert.Equal("fake reply", response.Text);
        Assert.Equal(["phi-4-mini"], _kernels.RequestedModels);
    }

    [Fact]
    public async Task GetResponseAsync_WithoutOptions_UsesTheDefaultModel()
    {
        await _client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        Assert.Equal([null], _kernels.RequestedModels);
    }

    [Fact]
    public async Task GetStreamingResponseAsync_WithModel_UsesTheKernelForThatModel()
    {
        var text = "";
        await foreach (var update in _client.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "hi")], new ChatOptions { ModelId = "phi-4-mini" }))
        {
            text += update.Text;
        }

        Assert.Equal("fake reply", text);
        Assert.Equal(["phi-4-mini"], _kernels.RequestedModels);
    }
}
