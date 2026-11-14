using LocalFoundry.Api.Plugins;

namespace LocalFoundry.Api.Tests;

public class MathPluginTests
{
    private readonly MathPlugin _math = new();

    [Fact]
    public void Add_ReturnsSum() => Assert.Equal(5.5, _math.Add(2, 3.5));

    [Fact]
    public void Subtract_ReturnsDifference() => Assert.Equal(-1.5, _math.Subtract(2, 3.5));

    [Fact]
    public void Multiply_ReturnsProduct() => Assert.Equal(391, _math.Multiply(17, 23));

    [Fact]
    public void Divide_ReturnsQuotient() => Assert.Equal(2.5, _math.Divide(10, 4));

    [Fact]
    public void Divide_ByZero_ThrowsWithAMessageTheModelCanRelay()
    {
        var exception = Assert.Throws<DivideByZeroException>(() => _math.Divide(1, 0));

        Assert.Equal("Cannot divide by zero.", exception.Message);
    }
}
