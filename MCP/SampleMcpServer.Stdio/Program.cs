using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleMcpServer.Tools;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer(options =>
    {
        McpServerConfiguration.Configure(options);
    })
    .WithStdioServerTransport()
    .WithResources<ServerResourceCatalog>()
    .WithPrompts<ServerPromptCatalog>()
    .WithTools<EchoTool>()
    .WithTools<CalculatorTool>()
    .WithTools<SystemInfoTool>();

await builder.Build().RunAsync();
