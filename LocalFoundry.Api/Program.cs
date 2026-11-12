using LocalFoundry.Api.Endpoints;
using LocalFoundry.Api.ErrorHandling;
using LocalFoundry.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalFoundryAi(builder.Configuration);

// Known Foundry Local failures become 502/503 ProblemDetails; anything else falls through
// to the default handler as a logged 500.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<FoundryLocalExceptionHandler>();

// Machine-readable description of the API, served at /openapi/v1.json.
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRequestTimeouts();

// Serves wwwroot/index.html (a plain HTML+JS test console) at "/".
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapOpenApi();
app.MapLocalFoundryApi();

app.Run();

/// <summary>Entry point; declared partial and public so integration tests can host the app via WebApplicationFactory.</summary>
public partial class Program;
