using LocalFoundry.Api.Models;
using LocalFoundry.Api.Options;
using LocalFoundry.Api.Plugins;
using LocalFoundry.Api.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FoundryLocalOptions>(builder.Configuration.GetSection("FoundryLocal"));

// Semantic Kernel: orchestration layer. Points at Foundry Local's OpenAI-compatible
// endpoint (run `foundry service start` first - see README).
builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<IOptions<FoundryLocalOptions>>().Value;

    var kernelBuilder = Kernel.CreateBuilder();
#pragma warning disable SKEXP0010 // Custom OpenAI-compatible endpoints are experimental in SK.
    kernelBuilder.AddOpenAIChatCompletion(
        modelId: options.ModelId,
        endpoint: new Uri(options.Endpoint),
        apiKey: options.ApiKey);
#pragma warning restore SKEXP0010
    kernelBuilder.Plugins.AddFromType<TimePlugin>();

    return kernelBuilder.Build();
});

// Microsoft.Extensions.AI: adapt the Semantic Kernel chat service into the M.E.AI
// abstraction so app/endpoint code can depend on the framework-neutral IChatClient.
builder.Services.AddSingleton<IChatClient>(sp =>
    sp.GetRequiredService<Kernel>().GetRequiredService<IChatCompletionService>().AsChatClient());

builder.Services.AddSingleton<FoundryLocalCatalogService>();

var app = builder.Build();

// Serves wwwroot/index.html (a plain HTML+JS test console) at "/".
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/info", () => Results.Ok(new
{
    service = "LocalFoundry.Api",
    description = "Sample .NET API combining Microsoft.Extensions.AI, Semantic Kernel, and Foundry Local.",
    endpoints = new[]
    {
        "POST /api/chat",
        "POST /api/chat/stream",
        "POST /api/agent",
        "GET /api/foundry/models",
    },
}));

// Microsoft.Extensions.AI: plain chat via IChatClient (backed by Semantic Kernel + Foundry Local).
app.MapPost("/api/chat", async (ChatRequest request, IChatClient chatClient, CancellationToken ct) =>
{
    try
    {
        var response = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, request.Message)],
            cancellationToken: ct);

        return Results.Ok(new ChatReply(response.Text));
    }
    catch (Exception ex)
    {
        return FoundryLocalUnavailable(ex);
    }
});

// Microsoft.Extensions.AI: streaming variant of the same endpoint.
app.MapPost("/api/chat/stream", async (ChatRequest request, IChatClient chatClient, HttpResponse httpResponse, CancellationToken ct) =>
{
    try
    {
        httpResponse.ContentType = "text/plain";

        await foreach (var update in chatClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, request.Message)],
            cancellationToken: ct))
        {
            await httpResponse.WriteAsync(update.Text, ct);
            await httpResponse.Body.FlushAsync(ct);
        }
    }
    catch (Exception ex) when (!httpResponse.HasStarted)
    {
        await Results.Problem(
            detail: FoundryLocalUnavailableMessage(ex),
            statusCode: StatusCodes.Status503ServiceUnavailable).ExecuteAsync(httpResponse.HttpContext);
    }
});

// Semantic Kernel: agent-style endpoint that lets the model call the TimePlugin function.
app.MapPost("/api/agent", async (ChatRequest request, Kernel kernel, CancellationToken ct) =>
{
    try
    {
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory("You are a helpful assistant. Use tools when they help answer the question.");
        history.AddUserMessage(request.Message);

        var settings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
        };

        var result = await chatCompletionService.GetChatMessageContentAsync(history, settings, kernel, ct);

        return Results.Ok(new ChatReply(result.Content ?? string.Empty));
    }
    catch (Exception ex)
    {
        return FoundryLocalUnavailable(ex);
    }
});

// Foundry Local: list the models currently available in the local model catalog.
app.MapGet("/api/foundry/models", async (FoundryLocalCatalogService catalogService) =>
{
    try
    {
        var models = await catalogService.ListModelsAsync();
        return Results.Ok(models);
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: $"Could not reach Foundry Local: {ex.Message}. Install it and run 'foundry service start' first.",
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.Run();

static IResult FoundryLocalUnavailable(Exception ex) =>
    Results.Problem(
        detail: FoundryLocalUnavailableMessage(ex),
        statusCode: StatusCodes.Status503ServiceUnavailable);

static string FoundryLocalUnavailableMessage(Exception ex) =>
    $"Could not reach the Foundry Local inference endpoint: {ex.Message}. " +
    "Run 'foundry model run <alias>' (or 'foundry service start') and check the FoundryLocal:Endpoint/ModelId " +
    "settings in appsettings.json match what's running. See README.md for setup steps.";
