using LocalFoundry.Api.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace LocalFoundry.Api.Tests;

public class FoundryLocalOptionsValidatorTests
{
    private readonly FoundryLocalOptionsValidator _validator = new();

    [Fact]
    public void Validate_Defaults_Succeeds()
    {
        Assert.True(_validator.Validate(null, new FoundryLocalOptions()).Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost:5273")]
    [InlineData("/v1")]
    [InlineData("ftp://localhost:5273/v1")]
    public void Validate_InvalidEndpoint_Fails(string endpoint)
    {
        var result = _validator.Validate(null, new FoundryLocalOptions { Endpoint = endpoint });

        Assert.True(result.Failed);
        Assert.Contains("FoundryLocal:Endpoint", result.FailureMessage);
    }

    [Fact]
    public void Validate_BlankModelIdAndApiKey_ReportsBoth()
    {
        var result = _validator.Validate(null, new FoundryLocalOptions { ModelId = " ", ApiKey = "" });

        Assert.True(result.Failed);
        Assert.Contains("FoundryLocal:ModelId", result.FailureMessage);
        Assert.Contains("FoundryLocal:ApiKey", result.FailureMessage);
    }

    [Fact]
    public void App_WithInvalidConfiguration_FailsAtStartup()
    {
        using var factory = new LocalFoundryApiFactory()
            .WithWebHostBuilder(builder => builder.UseSetting("FoundryLocal:Endpoint", "not-a-url"));

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("FoundryLocal:Endpoint", exception.Message);
    }
}
