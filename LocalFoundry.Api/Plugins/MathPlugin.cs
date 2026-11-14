using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace LocalFoundry.Api.Plugins;

/// <summary>
/// Arithmetic exposed to the model as callable tools. Language models are unreliable at
/// arithmetic, so the agent is better off delegating it than computing in its head.
/// </summary>
public sealed class MathPlugin
{
    /// <summary>Adds two numbers.</summary>
    /// <param name="a">The first number.</param>
    /// <param name="b">The second number.</param>
    /// <returns>The sum.</returns>
    [KernelFunction, Description("Adds two numbers and returns the sum.")]
    public double Add(
        [Description("The first number")] double a,
        [Description("The second number")] double b) => a + b;

    /// <summary>Subtracts one number from another.</summary>
    /// <param name="a">The number to subtract from.</param>
    /// <param name="b">The number to subtract.</param>
    /// <returns>The difference.</returns>
    [KernelFunction, Description("Subtracts the second number from the first and returns the difference.")]
    public double Subtract(
        [Description("The number to subtract from")] double a,
        [Description("The number to subtract")] double b) => a - b;

    /// <summary>Multiplies two numbers.</summary>
    /// <param name="a">The first number.</param>
    /// <param name="b">The second number.</param>
    /// <returns>The product.</returns>
    [KernelFunction, Description("Multiplies two numbers and returns the product.")]
    public double Multiply(
        [Description("The first number")] double a,
        [Description("The second number")] double b) => a * b;

    /// <summary>Divides one number by another.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <returns>The quotient.</returns>
    /// <exception cref="DivideByZeroException">
    /// <paramref name="b"/> is zero. Semantic Kernel passes the message back to the model, so it
    /// can explain the problem instead of reporting "Infinity".
    /// </exception>
    [KernelFunction, Description("Divides the first number by the second and returns the quotient.")]
    public double Divide(
        [Description("The number to divide (dividend)")] double a,
        [Description("The number to divide by (divisor); must not be zero")] double b) =>
        b == 0 ? throw new DivideByZeroException("Cannot divide by zero.") : a / b;
}
