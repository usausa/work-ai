namespace BasicAzureOpenApi;

public sealed class OpenAIOptions
{
    public string EndPoint { get; set; } = default!;

    public string ApiKey { get; set; } = default!;

    public string DeploymentName { get; set; } = default!;
}
