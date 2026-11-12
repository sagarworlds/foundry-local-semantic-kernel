using LocalFoundry.Api.Options;
using LocalFoundry.Api.Services;
using LocalFoundry.Api.Tests.Fakes;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LocalFoundry.Api.Tests;

public class SemanticKernelAgentServiceTests
{
    private readonly FakeKernelProvider _kernels = new();
    private readonly SemanticKernelAgentService _agent;

    public SemanticKernelAgentServiceTests()
    {
        _agent = new SemanticKernelAgentService(
            _kernels,
            Microsoft.Extensions.Options.Options.Create(new AgentOptions { SystemPrompt = "Be brief." }));
    }

    [Fact]
    public async Task AskAsync_SendsSystemPromptThenTheConversationInOrder()
    {
        ChatMessage[] conversation =
        [
            new(ChatRole.User, "What day is it?"),
            new(ChatRole.Assistant, "Monday"),
            new(ChatRole.User, "And tomorrow?"),
        ];

        await _agent.AskAsync(conversation, modelId: null, CancellationToken.None);

        Assert.Equal(
            [
                (AuthorRole.System, "Be brief."),
                (AuthorRole.User, "What day is it?"),
                (AuthorRole.Assistant, "Monday"),
                (AuthorRole.User, "And tomorrow?"),
            ],
            _kernels.ChatService.LastHistory!.Select(m => (m.Role, m.Content!)));
    }

    [Fact]
    public async Task AskAsync_EnablesAutomaticToolCallingWithTheModelsKernel()
    {
        await _agent.AskAsync([new ChatMessage(ChatRole.User, "What time is it?")], "phi-4-mini", CancellationToken.None);

        Assert.Equal(["phi-4-mini"], _kernels.RequestedModels);
        // The kernel must be passed along: it is what gives the model access to the plugins.
        Assert.Same(_kernels.Kernel, _kernels.ChatService.LastKernel);
        Assert.IsType<AutoFunctionChoiceBehavior>(_kernels.ChatService.LastSettings!.FunctionChoiceBehavior);
    }

    [Fact]
    public async Task AskAsync_ReturnsTheModelsReply()
    {
        _kernels.ChatService.Reply = "It is noon.";

        var reply = await _agent.AskAsync([new ChatMessage(ChatRole.User, "hi")], null, CancellationToken.None);

        Assert.Equal("It is noon.", reply);
    }

    [Fact]
    public async Task AskAsync_ModelReturnsNoText_ReturnsEmptyString()
    {
        _kernels.ChatService.Reply = null;

        var reply = await _agent.AskAsync([new ChatMessage(ChatRole.User, "hi")], null, CancellationToken.None);

        Assert.Equal(string.Empty, reply);
    }
}
