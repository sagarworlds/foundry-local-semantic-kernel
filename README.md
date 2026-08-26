# LocalFoundry.Api

A minimal ASP.NET Core Web API that runs a local LLM entirely on-device, combining:

- **[Foundry Local](https://learn.microsoft.com/azure/ai-foundry/foundry-local/)** — the on-device model runtime. It serves an OpenAI-compatible REST endpoint (`http://localhost:5273/v1`) and is also queried directly via its native .NET SDK to list the local model catalog.
- **[Semantic Kernel](https://learn.microsoft.com/semantic-kernel/)** — the orchestration layer. It talks to Foundry Local's OpenAI-compatible endpoint and provides plugins / function-calling (see `Plugins/TimePlugin.cs`).
- **[Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)** — the framework-neutral `IChatClient` abstraction that the API endpoints code against. It's obtained by adapting Semantic Kernel's `IChatCompletionService` via `.AsChatClient()`, so swapping the underlying provider later doesn't require touching endpoint code.

## How the pieces fit together

```
HTTP request → Minimal API endpoint → IChatClient (Microsoft.Extensions.AI)
                                            │
                                     .AsChatClient() adapter
                                            │
                              Semantic Kernel IChatCompletionService
                                            │
                         OpenAI-compatible connector (SK) ──► Foundry Local
                                                               (localhost:5273/v1)
```

`/api/foundry/models` bypasses all of that and talks to Foundry Local's native SDK directly (`Microsoft.AI.Foundry.Local`) to list the on-device model catalog.

## Prerequisites

- .NET 9 SDK or later
- [Foundry Local](https://learn.microsoft.com/azure/ai-foundry/foundry-local/get-started) installed:
  ```powershell
  winget install Microsoft.FoundryLocal
  ```
  If winget's install fails with a deployment/timeout error (`0x80072ee2`) — seen on this network because the MSIX download is a bit slow and winget's deployment step times out before it finishes — download the `.msix` directly from the [Foundry Local releases page](https://github.com/microsoft/Foundry-Local/releases) and install it with `Add-AppxPackage -Path <file>.msix` instead.

- Start the daemon **pinned to port 5273** (recent CLI versions default to a random port, which won't match this project's config):
  ```powershell
  foundry server start --port 5273
  ```
- Download and load a model:
  ```powershell
  foundry model download qwen2.5-0.5b
  foundry model load qwen2.5-0.5b
  ```
  Run `foundry model list` to see every alias available for your hardware (the app's `GET /api/foundry/models` endpoint also lists these programmatically once the app is running). `foundry server status` shows the daemon's current port/PID/uptime if you need to double check.

  > Older Foundry Local docs describe a single `foundry model run <alias>` / `foundry service start` command surface. As of CLI v0.10.3 these have been split into the `foundry server` and `foundry model download`/`load` commands shown above — run `foundry --help` if your version differs.

## Configuration

`appsettings.json`:

```json
"FoundryLocal": {
  "Endpoint": "http://localhost:5273/v1",
  "ModelId": "qwen2.5-0.5b",
  "ApiKey": "not-needed"
}
```

- `Endpoint` / `ModelId` must match the port `foundry server start --port <port>` is bound to and the alias `foundry model load <alias>` has loaded.
- `ApiKey` is required by the OpenAI connector's API shape but ignored by Foundry Local.

## Running

```powershell
cd LocalFoundry.Api
dotnet run
```

The app starts even if Foundry Local isn't running yet — the chat/agent endpoints will return a `503` with setup instructions until it is.

## Try it in a browser

Open **http://localhost:5189/** — it serves a plain HTML+JS test console (`wwwroot/index.html`) with a panel for each endpoint (chat, streaming chat, agent, model catalog) plus live status badges. It calls the API on the same origin, so no CORS setup is needed. This is the fastest way to check everything is wired correctly without writing any curl commands.

## Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/` | HTML test console (`wwwroot/index.html`) |
| GET | `/api/info` | Service info (JSON) |
| POST | `/api/chat` | Plain chat via `IChatClient` (Microsoft.Extensions.AI) |
| POST | `/api/chat/stream` | Streaming variant of the above |
| POST | `/api/agent` | Semantic Kernel chat with function-calling enabled (`TimePlugin`) |
| GET | `/api/foundry/models` | Lists the local Foundry model catalog via the native SDK |

Example requests:

```bash
curl -X POST http://localhost:5189/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"What is the capital of France?"}'

curl -X POST http://localhost:5189/api/agent \
  -H "Content-Type: application/json" \
  -d '{"message":"What time is it right now?"}'

curl http://localhost:5189/api/foundry/models
```

## Project layout

```
LocalFoundry.Api/
  Program.cs                       DI wiring + minimal API endpoints
  Options/FoundryLocalOptions.cs   Endpoint/ModelId/ApiKey configuration
  Plugins/TimePlugin.cs            Sample Semantic Kernel function
  Models/ChatDtos.cs               Request/response records
  Services/FoundryLocalCatalogService.cs   Lazy wrapper around FoundryLocalManager
  wwwroot/index.html               Browser test console for all endpoints
```
