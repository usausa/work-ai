using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SampleMcpServer.Tools;

[McpServerToolType]
public sealed class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the caller.")]
    public static string Echo(
        [Description("The message to echo.")] string message)
        => $"Echo: {message}";

    [McpServerTool, Description("Reverses the supplied message.")]
    public static string Reverse(
        [Description("The message to reverse.")] string message)
        => new(message.Reverse().ToArray());
}
