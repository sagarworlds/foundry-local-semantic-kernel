using LocalFoundry.Api.Endpoints;
using LocalFoundry.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLocalFoundryAi(builder.Configuration);

var app = builder.Build();

// Serves wwwroot/index.html (a plain HTML+JS test console) at "/".
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapLocalFoundryApi();

app.Run();
