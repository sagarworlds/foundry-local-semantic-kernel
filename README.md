# LocalFoundry.Api

[![CI](https://github.com/sagarworlds/foundry-local-semantic-kernel/actions/workflows/ci.yml/badge.svg)](https://github.com/sagarworlds/foundry-local-semantic-kernel/actions/workflows/ci.yml)

A minimal ASP.NET Core Web API that runs a local LLM entirely on-device, combining:

- **[Foundry Local](https://learn.microsoft.com/azure/ai-foundry/foundry-local/)** — the on-device model runtime. It serves an OpenAI-compatible REST endpoint (`http://localhost:5273/v1`) and is also queried directly via its native .NET SDK to list the local model catalog.
- **[Semantic Kernel](https://learn.microsoft.com/semantic-kernel/)** — the orchestration layer. It talks to Foundry Local's OpenAI-compatible endpoint and provides plugins / function-calling (see `Plugins/TimePlugin.cs`).
- **[Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)** — the framework-neutral `IChatClient` abstraction that the API endpoints code against. It's obtained by adapting Semantic Kernel's `IChatCompletionService` via `.AsChatClient()`, so swapping the underlying provider later doesn't require touching endpoint code.

## How the pieces fit together

```
HTTP request → Minimal API endpoint → IChatClient (Microsoft.Extensions.AI)
                                            │
                              ModelRoutingChatClient (picks the kernel for the
                              requested model, then .AsChatClient() adapter)
                                            │
                              Semantic Kernel IChatCompletionService
                                            │
                         OpenAI-compatible connector (SK) ──► Foundry Local
                                                               (localhost:5273/v1)
```

`/api/agent` goes through `IAgentService` (implemented with Semantic Kernel function-calling), and `/api/foundry/models` goes through `IModelCatalogService`, which talks to Foundry Local's native SDK directly (`Microsoft.AI.Foundry.Local`) to list the on-device model catalog. Endpoints depend only on these abstractions, so each backend can be swapped (or faked in tests) without touching endpoint code.

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
  "ApiKey": "not-needed",
  "RequestTimeoutSeconds": 120
},
"Agent": {
  "SystemPrompt": "You are a helpful assistant. Use tools when they help answer the question."
}
```

- `Endpoint` / `ModelId` must match the port `foundry server start --port <port>` is bound to and the alias `foundry model load <alias>` has loaded.
- `ApiKey` is required by the OpenAI connector's API shape but ignored by Foundry Local.
- `RequestTimeoutSeconds` is how long to wait for Foundry Local before returning `504`. For `/api/chat/stream` it only covers the wait for the first token.

- `Agent:SystemPrompt` is the instruction placed ahead of every `/api/agent` conversation.

The settings are validated at startup: the app refuses to start, naming the bad setting, if `Endpoint` isn't an http(s) URL, `ModelId`/`ApiKey`/`SystemPrompt` is empty or the timeout isn't positive.

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
| GET | `/openapi/v1.json` | OpenAPI description of the endpoints below |
| GET | `/health` | `200` if Foundry Local's endpoint is reachable, `503` if not, with a JSON reason |
| POST | `/api/chat` | Plain chat via `IChatClient` (Microsoft.Extensions.AI) |
| POST | `/api/chat/stream` | Streaming variant of the above, as Server-Sent Events |
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

### Multi-turn conversations

The API is stateless. To ask a follow-up question, send the earlier turns back in `history` (oldest first); the chat, streaming and agent endpoints all accept it:

```bash
curl -X POST http://localhost:5189/api/chat \
  -H "Content-Type: application/json" \
  -d '{
        "message": "And of Italy?",
        "history": [
          { "role": "user", "content": "What is the capital of France?" },
          { "role": "assistant", "content": "Paris." }
        ]
      }'
```

### Choosing a model per request

Add `"model": "<alias>"` to any chat, streaming or agent request to use a model other than the configured `ModelId` for that request. The alias must be one that `GET /api/foundry/models` lists and that is loaded (`foundry model load <alias>`); if it isn't, Foundry Local's error comes back as a `502`.

```bash
curl -X POST http://localhost:5189/api/chat \
  -H "Content-Type: application/json" \
  -d '{"message":"What is the capital of France?","model":"phi-4-mini"}'
```

The browser test console has a **Model** dropdown filled from the catalog, and keeps conversations for you: each panel keeps its transcript until you click **New conversation**.

## Validation and error responses

Chat and agent requests are validated before reaching the model, otherwise the API returns `400` with a `ValidationProblem` body listing the errors:

- `message` must be present, non-blank and at most 8000 characters.
- `model` is optional; if given it must be 1-100 characters of letters, digits, `.`, `-`, `_` or `:`.
- `history` is optional, at most 50 turns. Each turn needs a `role` of `user` or `assistant` (`system` is rejected so callers can't override the agent's instructions) and non-blank `content` of at most 8000 characters.

Failures are handled centrally by `FoundryLocalExceptionHandler`, which logs them and returns [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457):

| Status | When |
|---|---|
| `400` | Invalid request body (see above) |
| `502` | Foundry Local responded with an error — typically the configured `ModelId` isn't loaded |
| `503` | Foundry Local couldn't be reached — daemon not running, or on a different port than `Endpoint` |
| `504` | Foundry Local didn't answer within `RequestTimeoutSeconds` |
| `500` | Any other, unexpected error (a bug in this app — check the logs) |

`/api/chat/stream` responds with `text/event-stream`. Errors that occur before the first token get the status codes above. After that the status is already `200`, so the outcome is reported in the stream itself:

```
event: token
data: {"text":"Hello"}

event: done
data: {}
```

A stream that fails part-way ends with `event: error` and `data: {"status":503,"title":"...","detail":"..."}` instead of `done`, so a client can always tell a finished reply from a broken one.

## Running the tests

```powershell
dotnet test LocalFoundry.Api.Tests
```

The tests don't need Foundry Local: they host the app in-memory with `WebApplicationFactory` and replace `IChatClient`, `IAgentService` and `IModelCatalogService` with fakes.

## Continuous integration

`.github/workflows/ci.yml` runs on every pull request and push to `main`: restore, build with warnings as errors (including missing XML docs), and run the tests.

## Project layout

```
LocalFoundry.Api/
  Program.cs                                 Host setup: DI, exception handling, static files, endpoints
  Extensions/ServiceCollectionExtensions.cs  AddLocalFoundryAi(): Semantic Kernel, IChatClient and service registration
  Endpoints/                                 One class per endpoint group (info, health, chat, agent, models)
  Health/FoundryLocalHealthCheck.cs          Probes Foundry Local's /models endpoint for GET /health
  Timeouts/ModelRequestTimeout.cs            504 timeout policy for endpoints that call the model
  ErrorHandling/                             Maps Foundry Local failures to 502/503 ProblemDetails
  Validation/                                ChatRequest validator + endpoint filter
  Options/                                   FoundryLocal (endpoint, model, timeout) and Agent (system prompt) settings + validation
  Plugins/TimePlugin.cs                      Sample Semantic Kernel function
  Models/ChatDtos.cs                         Request/response records
  Services/IKernelProvider.cs                Builds the Semantic Kernel for a given model (+ FoundryLocalKernelProvider)
  Services/ModelRoutingChatClient.cs         IChatClient that routes each call to the requested model's kernel
  Services/IAgentService.cs                  Tool-calling agent abstraction (+ SemanticKernelAgentService)
  Services/IModelCatalogService.cs           Model catalog abstraction (+ FoundryLocalCatalogService, lazy SDK wrapper)
  wwwroot/index.html                         Browser test console for all endpoints
LocalFoundry.Api.Tests/                      xUnit unit and in-memory HTTP tests
.github/workflows/ci.yml                     Build and test on pull requests and pushes to main
```
