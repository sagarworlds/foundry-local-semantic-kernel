namespace LocalFoundry.Api.Options;

public sealed class FoundryLocalOptions
{
    public string Endpoint { get; set; } = "http://localhost:5273/v1";

    public string ModelId { get; set; } = "qwen2.5-0.5b";

    public string ApiKey { get; set; } = "not-needed";
}
