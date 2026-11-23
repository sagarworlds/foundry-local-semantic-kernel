namespace LocalFoundry.Api.Models;

/// <summary>Request body for the chat and agent endpoints.</summary>
/// <param name="Message">The user's new message to the model.</param>
/// <param name="History">
/// Earlier turns of the conversation, oldest first, so the model can answer follow-up questions.
/// The API is stateless: the client keeps the transcript and sends it back with each request.
/// </param>
/// <param name="Model">
/// Alias of the Foundry Local model to use for this request (one of the aliases returned by
/// <c>GET /api/foundry/models</c>, loaded with <c>foundry model load</c>). Omit to use the
/// configured <c>FoundryLocal:ModelId</c>.
/// </param>
/// <param name="ConversationId">
/// Id of a server-side conversation (from <c>POST /api/conversations</c>). The server then
/// supplies the earlier turns and records this exchange, so <paramref name="History"/> must be omitted.
/// </param>
public sealed record ChatRequest(
    string Message, IReadOnlyList<ChatTurn>? History = null, string? Model = null, string? ConversationId = null);

/// <summary>One earlier turn of a conversation.</summary>
/// <param name="Role">Who said it: <c>user</c> or <c>assistant</c>.</param>
/// <param name="Content">The text of the turn.</param>
public sealed record ChatTurn(string Role, string Content);

/// <summary>Response body for the non-streaming chat and agent endpoints.</summary>
/// <param name="Reply">The model's reply text.</param>
/// <param name="ConversationId">The server-side conversation the exchange was recorded in, if the request named one.</param>
public sealed record ChatReply(string Reply, string? ConversationId = null);

/// <summary>Response body of <c>POST /api/conversations</c>.</summary>
/// <param name="Id">The new conversation's id, to send as <c>conversationId</c> in chat and agent requests.</param>
public sealed record ConversationCreated(string Id);

/// <summary>Response body of <c>GET /api/conversations/{id}</c>.</summary>
/// <param name="Id">The conversation's id.</param>
/// <param name="Turns">The recorded turns, oldest first.</param>
public sealed record ConversationTranscript(string Id, IReadOnlyList<ChatTurn> Turns);

/// <summary>A model available in the local Foundry catalog.</summary>
/// <param name="Alias">Short alias used with the <c>foundry model</c> CLI commands and as <c>FoundryLocal:ModelId</c>.</param>
/// <param name="Id">Full, hardware-specific model variant identifier.</param>
public sealed record FoundryModelSummary(string Alias, string Id);

/// <summary>Payload of a <c>token</c> event on the streaming chat endpoint.</summary>
/// <param name="Text">The next fragment of the model's reply.</param>
public sealed record ChatStreamToken(string Text);

/// <summary>Payload of an <c>error</c> event on the streaming chat endpoint, sent when the stream fails part-way.</summary>
/// <param name="Status">The HTTP status the failure would have had if it had occurred before streaming began.</param>
/// <param name="Title">Short summary of the problem.</param>
/// <param name="Detail">Actionable explanation of the problem.</param>
public sealed record ChatStreamError(int Status, string Title, string Detail);
