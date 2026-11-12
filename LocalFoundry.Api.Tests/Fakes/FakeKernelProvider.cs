using LocalFoundry.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LocalFoundry.Api.Tests.Fakes;

/// <summary>
/// <see cref="IKernelProvider"/> that always returns one kernel backed by a
/// <see cref="FakeChatCompletionService"/>, recording which model was asked for.
/// </summary>
public sealed class FakeKernelProvider : IKernelProvider
{
    public FakeKernelProvider()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton<IChatCompletionService>(ChatService);
        Kernel = builder.Build();
    }

    public FakeChatCompletionService ChatService { get; } = new();

    public Kernel Kernel { get; }

    public List<string?> RequestedModels { get; } = [];

    public Kernel GetKernel(string? modelId)
    {
        RequestedModels.Add(modelId);
        return Kernel;
    }
}
