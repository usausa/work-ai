using System.ComponentModel;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace SampleMcpServer.Tools;

[McpServerPromptType]
public sealed class ServerPromptCatalog
{
    [McpServerPrompt(Name = "server-overview", Title = "Server Overview"), Description("Builds a short server overview prompt for the specified audience.")]
    public static GetPromptResult GetServerOverview(
        [Description("The audience for the overview, such as developer, operator, or user.")] string audience = "developer")
        => new()
        {
            Description = "A concise overview of the sample MCP server.",
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock
                    {
                        Text = $"Provide a concise overview of the sample MCP server for a {audience}. Include available tools, the server info resource, and typical use cases."
                    }
                }
            ]
        };
}
