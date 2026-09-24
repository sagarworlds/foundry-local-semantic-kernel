using LocalFoundry.Api.Models;

namespace LocalFoundry.Api.Validation;

/// <summary>
/// Endpoint filter that rejects invalid <see cref="ChatRequest"/> bodies with a 400
/// ValidationProblem response instead of forwarding them to the model.
/// </summary>
public sealed class ChatRequestValidationFilter : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<ChatRequest>().FirstOrDefault();
        var errors = ChatRequestValidator.Validate(request);

        return errors.Count > 0
            ? Results.ValidationProblem(errors)
            : await next(context);
    }
}
