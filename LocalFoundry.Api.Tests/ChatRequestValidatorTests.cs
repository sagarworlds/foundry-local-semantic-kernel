using LocalFoundry.Api.Models;
using LocalFoundry.Api.Validation;

namespace LocalFoundry.Api.Tests;

public class ChatRequestValidatorTests
{
    [Fact]
    public void Validate_ValidMessage_ReturnsNoErrors()
    {
        Assert.Empty(ChatRequestValidator.Validate(new ChatRequest("What is the capital of France?")));
    }

    [Fact]
    public void Validate_MessageAtMaxLength_ReturnsNoErrors()
    {
        var message = new string('a', ChatRequestValidator.MaxMessageLength);

        Assert.Empty(ChatRequestValidator.Validate(new ChatRequest(message)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Validate_MissingOrBlankMessage_ReturnsMessageError(string? message)
    {
        var errors = ChatRequestValidator.Validate(new ChatRequest(message!));

        Assert.Contains(nameof(ChatRequest.Message), errors.Keys);
    }

    [Fact]
    public void Validate_NullRequest_ReturnsMessageError()
    {
        var errors = ChatRequestValidator.Validate(null);

        Assert.Contains(nameof(ChatRequest.Message), errors.Keys);
    }

    [Fact]
    public void Validate_MessageOverMaxLength_ReturnsLengthError()
    {
        var message = new string('a', ChatRequestValidator.MaxMessageLength + 1);

        var errors = ChatRequestValidator.Validate(new ChatRequest(message));

        var error = Assert.Single(errors[nameof(ChatRequest.Message)]);
        Assert.Contains(ChatRequestValidator.MaxMessageLength.ToString(), error);
    }

    [Fact]
    public void Validate_ValidHistory_ReturnsNoErrors()
    {
        var request = new ChatRequest("And of Italy?", [new ChatTurn("user", "Capital of France?"), new ChatTurn("Assistant", "Paris")]);

        Assert.Empty(ChatRequestValidator.Validate(request));
    }

    [Theory]
    [InlineData("system")]
    [InlineData("tool")]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_HistoryWithUnsupportedRole_ReturnsHistoryError(string? role)
    {
        var errors = ChatRequestValidator.Validate(new ChatRequest("hi", [new ChatTurn(role!, "text")]));

        var error = Assert.Single(errors[nameof(ChatRequest.History)]);
        Assert.Contains("History[0].Role", error);
    }

    [Fact]
    public void Validate_HistoryWithBlankOversizedAndNullTurns_ReportsEachByIndex()
    {
        var history = new List<ChatTurn>
        {
            new("user", " "),
            new("assistant", new string('a', ChatRequestValidator.MaxMessageLength + 1)),
            null!,
        };

        var errors = ChatRequestValidator.Validate(new ChatRequest("hi", history))[nameof(ChatRequest.History)];

        Assert.Equal(3, errors.Length);
        Assert.Contains("History[0].Content", errors[0]);
        Assert.Contains("History[1].Content", errors[1]);
        Assert.Contains("History[2]", errors[2]);
    }

    [Fact]
    public void Validate_TooManyHistoryTurns_ReturnsSingleError()
    {
        var history = Enumerable.Repeat(new ChatTurn("user", "hi"), ChatRequestValidator.MaxHistoryTurns + 1).ToList();

        var error = Assert.Single(ChatRequestValidator.Validate(new ChatRequest("hi", history))[nameof(ChatRequest.History)]);
        Assert.Contains(ChatRequestValidator.MaxHistoryTurns.ToString(), error);
    }

    [Theory]
    [InlineData("qwen2.5-0.5b")]
    [InlineData("Phi-4-mini-instruct-generic-cpu:4")]
    public void Validate_ValidModel_ReturnsNoErrors(string model)
    {
        Assert.Empty(ChatRequestValidator.Validate(new ChatRequest("hi", Model: model)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("phi 4")]
    [InlineData("../models/other")]
    [InlineData("model\nname")]
    public void Validate_MalformedModel_ReturnsModelError(string model)
    {
        var errors = ChatRequestValidator.Validate(new ChatRequest("hi", Model: model));

        Assert.Contains(nameof(ChatRequest.Model), errors.Keys);
    }

    [Fact]
    public void Validate_ModelOverMaxLength_ReturnsModelError()
    {
        var model = new string('a', ChatRequestValidator.MaxModelLength + 1);

        Assert.Contains(nameof(ChatRequest.Model), ChatRequestValidator.Validate(new ChatRequest("hi", Model: model)).Keys);
    }
}
