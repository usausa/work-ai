using System.Diagnostics.CodeAnalysis;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace SampleMcpServer.Tools;

public static class McpServerConfiguration
{
    [SuppressMessage("Usage", "MCPEXP001", Justification = "The sample intentionally shows the MCP options surface, including experimental members.")]
    public static void Configure(McpServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ProtocolVersion = "2024-11-05";
        options.ServerInstructions = "Use tools for actions, the server-info resource for static capability details, and the server-overview prompt when a short usage summary is needed.";
        options.InitializationTimeout = TimeSpan.FromSeconds(30);
        options.SendTaskStatusNotifications = true;
        options.ScopeRequests = true;
        options.MaxSamplingOutputTokens = 2048;
        options.ServerInfo = new Implementation
        {
            Name = "sample-mcp-server",
            Title = "Sample MCP Server",
            Version = "1.0.0",
            Description = "Sample MCP server exposing tools, resources, and prompts over stdio and HTTP.",
            WebsiteUrl = "https://github.com/usausa/work-ai"
        };
        options.Capabilities = new ServerCapabilities
        {
            Logging = new LoggingCapability(),
            Prompts = new PromptsCapability
            {
                ListChanged = false
            },
            Resources = new ResourcesCapability
            {
                ListChanged = false,
                Subscribe = false
            },
            Tools = new ToolsCapability
            {
                ListChanged = false
            },
            Completions = new CompletionsCapability(),
            Extensions = new Dictionary<string, object>(),
            Experimental = new Dictionary<string, object>()
        };
        options.Handlers.SetLoggingLevelHandler = static (requestContext, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = requestContext.Params.Level;
            return ValueTask.FromResult(new EmptyResult());
        };
        options.Handlers.CompleteHandler = static (requestContext, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var values = new List<string>();
            var argumentName = requestContext.Params.Argument?.Name;
            if (!string.IsNullOrWhiteSpace(argumentName))
            {
                switch (argumentName)
                {
                    case "audience":
                        values.AddRange(["developer", "operator", "user"]);
                        break;
                    case "message":
                        values.AddRange(["hello", "sample", "mcp"]);
                        break;
                }
            }

            return ValueTask.FromResult(new CompleteResult
            {
                Completion = new Completion
                {
                    Values = values,
                    Total = values.Count,
                    HasMore = false
                }
            });
        };
        options.Handlers.SubscribeToResourcesHandler = static (requestContext, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromException<EmptyResult>(new NotSupportedException("Resource subscriptions are not enabled by this sample server."));
        };
        options.Handlers.UnsubscribeFromResourcesHandler = static (requestContext, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromException<EmptyResult>(new NotSupportedException("Resource subscriptions are not enabled by this sample server."));
        };
    }

}
