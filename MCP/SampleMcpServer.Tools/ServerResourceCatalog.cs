using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SampleMcpServer.Tools;

[McpServerResourceType]
public sealed class ServerResourceCatalog
{
    [McpServerResource(UriTemplate = "resource://server/info", MimeType = "text/plain", Name = "server-info", Title = "Server Info"), Description("Returns a plain text summary of the sample MCP server capabilities.")]
    public static string GetServerInfo()
        => """
            Sample MCP Server
            - tools: Echo, Reverse, Add, Subtract, Multiply, Divide, GetUtcNow, GetRuntimeInfo
            - resources: resource://server/info
            - prompts: server-overview
            """;
}
