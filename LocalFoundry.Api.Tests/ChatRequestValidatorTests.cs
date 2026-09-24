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
}
