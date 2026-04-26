using System.ComponentModel;
using System.Runtime.InteropServices;
using ModelContextProtocol.Server;

namespace SampleMcpServer.Tools;

[McpServerToolType]
public sealed class SystemInfoTool
{
    [McpServerTool, Description("Returns the current UTC timestamp in ISO 8601 format.")]
    public static string GetUtcNow()
        => DateTimeOffset.UtcNow.ToString("O");

    [McpServerTool, Description("Returns basic information about the host runtime and OS.")]
    public static string GetRuntimeInfo()
        => $"""
            OS:          {RuntimeInformation.OSDescription}
            Architecture:{RuntimeInformation.OSArchitecture}
            Framework:   {RuntimeInformation.FrameworkDescription}
            ProcessId:   {Environment.ProcessId}
            MachineName: {Environment.MachineName}
            """;
}
