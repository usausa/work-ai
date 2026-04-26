using SampleMcpServer.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithResources<ServerResourceCatalog>()
    .WithPrompts<ServerPromptCatalog>()
    .WithTools<EchoTool>()
    .WithTools<CalculatorTool>()
    .WithTools<SystemInfoTool>();

var app = builder.Build();

app.MapMcp("/mcp");

app.MapGet("/", () => Results.Text(
    "Sample MCP Server (HTTP).\nMCP endpoint: POST /mcp (Streamable HTTP).\n",
    "text/plain"));

app.Run();
