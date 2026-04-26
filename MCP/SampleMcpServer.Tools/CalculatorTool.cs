using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace SampleMcpServer.Tools;

[McpServerToolType]
public sealed class CalculatorTool
{
    [McpServerTool, Description("Adds two numbers.")]
    public static double Add(
        [Description("Left operand.")] double a,
        [Description("Right operand.")] double b)
        => a + b;

    [McpServerTool, Description("Subtracts b from a.")]
    public static double Subtract(
        [Description("Left operand.")] double a,
        [Description("Right operand.")] double b)
        => a - b;

    [McpServerTool, Description("Multiplies two numbers.")]
    public static double Multiply(
        [Description("Left operand.")] double a,
        [Description("Right operand.")] double b)
        => a * b;

    [McpServerTool, Description("Divides a by b. Throws if b is zero.")]
    public static double Divide(
        [Description("Numerator.")] double a,
        [Description("Denominator (must be non-zero).")] double b)
    {
        if (b == 0)
        {
            throw new McpException("Division by zero is not allowed.");
        }
        return a / b;
    }
}
