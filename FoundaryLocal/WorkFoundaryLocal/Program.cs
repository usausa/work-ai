using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging;

const string DefaultModelAlias = "qwen2.5-0.5b";
const string DefaultSystemPrompt =
    "あなたは Foundry Local の学習用アシスタントです。回答は簡潔で分かりやすく、" +
    "必要に応じて箇条書きを使ってください。分からないことは分からないと伝えてください。";

CancellationToken cancellationToken = CancellationToken.None;
AppOptions options = ParseOptions(args);

if (options.ShowHelp)
{
    PrintCliHelp();
    return;
}

try
{
    Console.OutputEncoding = System.Text.Encoding.UTF8;
    Console.WriteLine("=== WorkFoundaryLocal / Foundry Local 学習サンプル ===");
    Console.WriteLine($"希望モデル: {options.ModelAlias}");
    Console.WriteLine($"実行プロバイダーの自動取得: {!options.SkipEpDownload}");
    Console.WriteLine($"モデルの自動ダウンロード: {!options.SkipModelDownload}");
    Console.WriteLine($"モデル一覧のみ表示: {options.ListModelsOnly}");
    Console.WriteLine();

    string appDataDir = Path.Combine(Environment.CurrentDirectory, ".foundry-local-data");
    string modelCacheDir = Path.Combine(appDataDir, "cache", "models");
    string logsDir = Path.Combine(appDataDir, "logs");

    Environment.SetEnvironmentVariable("HOME", Environment.CurrentDirectory);
    Environment.SetEnvironmentVariable("USERPROFILE", Environment.CurrentDirectory);

    var configuration = new Configuration
    {
        AppName = "work_foundary_local_learning_sample",
        AppDataDir = appDataDir,
        ModelCacheDir = modelCacheDir,
        LogsDir = logsDir,
        LogLevel = Microsoft.AI.Foundry.Local.LogLevel.Information
    };

    using var loggerFactory = LoggerFactory.Create(builder =>
    {
        builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
    });

    ILogger logger = loggerFactory.CreateLogger("WorkFoundaryLocal");

    await FoundryLocalManager.CreateAsync(configuration, logger);
    var manager = FoundryLocalManager.Instance;

    if (options.SkipEpDownload)
    {
        Console.WriteLine("実行プロバイダーのダウンロードをスキップしました。");
        Console.WriteLine();
    }
    else
    {
        await EnsureExecutionProvidersAsync(manager);
    }

    ICatalog catalog = await manager.GetCatalogAsync();
    IModel[] availableModels = (await catalog.ListModelsAsync()).ToArray();
    PrintCatalogOverview(availableModels, options.ModelAlias);

    if (options.ListModelsOnly)
    {
        Console.WriteLine("モデル一覧表示のみで終了します。");
        return;
    }

    var model = await SelectModelAsync(catalog, availableModels, options.ModelAlias);

    try
    {
        if (options.SkipModelDownload)
        {
            Console.WriteLine("モデルのダウンロードをスキップしました。キャッシュ済みでない場合はロードに失敗する可能性があります。");
            Console.WriteLine();
        }
        else
        {
            await DownloadModelAsync(model);
        }

        Console.WriteLine($"モデルをロードしています: {model.Id}");
        await model.LoadAsync();
        Console.WriteLine("モデルのロードが完了しました。");
        Console.WriteLine();

        var chatClient = await model.GetChatClientAsync();
        var messages = CreateInitialMessages(DefaultSystemPrompt);

        PrintHelp(model.Id);

        while (true)
        {
            Console.Write("You> ");
            var userInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            if (IsExitCommand(userInput))
            {
                break;
            }

            if (TryHandleLocalCommand(userInput, messages))
            {
                continue;
            }

            messages.Add(new ChatMessage
            {
                Role = "user",
                Content = userInput
            });

            string response = await StreamAssistantResponseAsync(chatClient, messages, cancellationToken);
            messages.Add(new ChatMessage
            {
                Role = "assistant",
                Content = response
            });

            Console.WriteLine();
        }
    }
    finally
    {
        Console.WriteLine();
        Console.WriteLine("モデルをアンロードしています...");
        await model.UnloadAsync();
        Console.WriteLine("終了しました。");
    }
}
catch (Exception ex)
{
    PrintFriendlyError(ex);
    Environment.ExitCode = 1;
}

AppOptions ParseOptions(string[] args)
{
    string? modelAlias = null;
    bool showHelp = false;
    bool listModelsOnly = false;
    bool skipEpDownload = false;
    bool skipModelDownload = false;

    for (int i = 0; i < args.Length; i++)
    {
        var arg = args[i];
        switch (arg)
        {
            case "--help":
            case "-h":
                showHelp = true;
                break;
            case "--list-models":
                listModelsOnly = true;
                break;
            case "--skip-ep-download":
                skipEpDownload = true;
                break;
            case "--skip-model-download":
                skipModelDownload = true;
                break;
            case "--model":
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException("--model の後ろにモデル別名を指定してください。");
                }

                modelAlias = args[++i].Trim();
                break;
            default:
                if (!arg.StartsWith("-", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(modelAlias))
                {
                    modelAlias = arg.Trim();
                    break;
                }

                throw new ArgumentException($"未対応の引数です: {arg}");
        }
    }

    var environmentValue = Environment.GetEnvironmentVariable("FOUNDRY_MODEL_ALIAS");
    modelAlias = string.IsNullOrWhiteSpace(modelAlias)
        ? (string.IsNullOrWhiteSpace(environmentValue) ? DefaultModelAlias : environmentValue.Trim())
        : modelAlias;

    return new AppOptions(
        ModelAlias: modelAlias,
        ShowHelp: showHelp,
        ListModelsOnly: listModelsOnly,
        SkipEpDownload: skipEpDownload,
        SkipModelDownload: skipModelDownload);
}

async Task EnsureExecutionProvidersAsync(FoundryLocalManager manager)
{
    Console.WriteLine("実行プロバイダーを確認しています...");

    string currentExecutionProvider = string.Empty;
    await manager.DownloadAndRegisterEpsAsync((epName, percent) =>
    {
        if (!string.Equals(currentExecutionProvider, epName, StringComparison.Ordinal))
        {
            if (!string.IsNullOrEmpty(currentExecutionProvider))
            {
                Console.WriteLine();
            }

            currentExecutionProvider = epName;
        }

        Console.Write($"\r  {epName.PadRight(30)} {percent,6:F1}%");
    });

    if (!string.IsNullOrEmpty(currentExecutionProvider))
    {
        Console.WriteLine();
    }

    Console.WriteLine("実行プロバイダーの準備が完了しました。");
    Console.WriteLine();
}

void PrintCatalogOverview(IReadOnlyList<IModel> availableModels, string requestedModelAlias)
{
    Console.WriteLine($"利用可能なモデル数: {availableModels.Count}");
    Console.WriteLine($"要求したモデル別名: {requestedModelAlias}");
    Console.WriteLine("利用可能モデルの先頭 10 件:");

    foreach (var model in availableModels.Take(10))
    {
        Console.WriteLine($"  - {model.Id}");
    }

    Console.WriteLine();
}

async Task<IModel> SelectModelAsync(ICatalog catalog, IReadOnlyList<IModel> availableModels, string requestedModelAlias)
{
    var requestedModel = await catalog.GetModelAsync(requestedModelAlias);
    if (requestedModel is not null)
    {
        return requestedModel;
    }

    var partialMatch = availableModels.FirstOrDefault(model =>
        model.Id.Contains(requestedModelAlias, StringComparison.OrdinalIgnoreCase));
    if (partialMatch is not null)
    {
        Console.WriteLine($"'{requestedModelAlias}' の部分一致として '{partialMatch.Id}' を使用します。");
        Console.WriteLine();
        return partialMatch;
    }

    var fallbackModel = ChoosePreferredFallbackModel(availableModels);
    if (fallbackModel is null)
    {
        throw new InvalidOperationException("この環境で利用できる Foundry Local モデルが見つかりませんでした。");
    }

    Console.WriteLine($"'{requestedModelAlias}' が見つからなかったため、'{fallbackModel.Id}' を使用します。");
    Console.WriteLine();
    return fallbackModel;
}

IModel? ChoosePreferredFallbackModel(IReadOnlyList<IModel> availableModels)
{
    string[] preferredPatterns =
    [
        "phi-4-mini",
        "phi-3.5-mini",
        "phi-3-mini",
        "qwen",
        "mistral-7b",
        "mini"
    ];

    foreach (string pattern in preferredPatterns)
    {
        var match = availableModels
            .Where(model => model.Id.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            .OrderBy(model => model.Id.Length)
            .FirstOrDefault();

        if (match is not null)
        {
            return match;
        }
    }

    return availableModels.FirstOrDefault();
}

async Task DownloadModelAsync(IModel model)
{
    Console.WriteLine($"モデルをダウンロードしています: {model.Id}");

    await model.DownloadAsync((float progress) =>
    {
        Console.Write($"\r  download {progress,6:F2}%");
    });

    Console.WriteLine();
    Console.WriteLine("モデルのダウンロード確認が完了しました。");
}

List<ChatMessage> CreateInitialMessages(string systemPrompt)
{
    return
    [
        new ChatMessage
        {
            Role = "system",
            Content = systemPrompt
        }
    ];
}

bool IsExitCommand(string input)
{
    return input.Equals("quit", StringComparison.OrdinalIgnoreCase)
        || input.Equals("exit", StringComparison.OrdinalIgnoreCase)
        || input.Equals("/exit", StringComparison.OrdinalIgnoreCase);
}

bool TryHandleLocalCommand(string input, List<ChatMessage> messages)
{
    if (input.Equals("/help", StringComparison.OrdinalIgnoreCase))
    {
        PrintHelp(null);
        return true;
    }

    if (input.Equals("/reset", StringComparison.OrdinalIgnoreCase))
    {
        messages.Clear();
        messages.AddRange(CreateInitialMessages(DefaultSystemPrompt));
        Console.WriteLine("会話履歴をリセットしました。");
        Console.WriteLine();
        return true;
    }

    if (input.Equals("/history", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"現在の履歴メッセージ数: {messages.Count}");
        Console.WriteLine();
        return true;
    }

    return false;
}

async Task<string> StreamAssistantResponseAsync(
    OpenAIChatClient chatClient,
    List<ChatMessage> messages,
    CancellationToken cancellationToken)
{
    Console.Write("Assistant> ");

    string response = string.Empty;
    var streamingResponse = chatClient.CompleteChatStreamingAsync(messages, cancellationToken);

    await foreach (var chunk in streamingResponse)
    {
        var choice = chunk.Choices.FirstOrDefault();
        string? content = choice?.Message?.Content;
        if (string.IsNullOrEmpty(content))
        {
            continue;
        }

        Console.Write(content);
        Console.Out.Flush();
        response += content;
    }

    Console.WriteLine();
    return response;
}

void PrintHelp(string? modelId)
{
    Console.WriteLine("学習ポイント:");
    Console.WriteLine("  1. FoundryLocalManager を初期化する");
    Console.WriteLine("  2. 実行プロバイダーを登録する");
    Console.WriteLine("  3. モデルを取得、ダウンロード、ロードする");
    Console.WriteLine("  4. ChatClient へ会話履歴を渡して応答をストリーミングする");
    Console.WriteLine("  5. 終了時にモデルをアンロードする");
    Console.WriteLine();
    Console.WriteLine($"現在のモデル: {modelId ?? "(読み込み済みモデルはそのまま)"}");
    Console.WriteLine("コマンド: /help, /reset, /history, exit");
    Console.WriteLine();
}

void PrintCliHelp()
{
    Console.WriteLine("使用例:");
    Console.WriteLine("  dotnet run --project .\\WorkFoundaryLocal\\WorkFoundaryLocal.csproj");
    Console.WriteLine("  dotnet run --project .\\WorkFoundaryLocal\\WorkFoundaryLocal.csproj -- --model your-model-alias");
    Console.WriteLine("  dotnet run --project .\\WorkFoundaryLocal\\WorkFoundaryLocal.csproj -- --list-models --skip-ep-download");
    Console.WriteLine();
    Console.WriteLine("オプション:");
    Console.WriteLine("  --help                 このヘルプを表示");
    Console.WriteLine("  --model <alias>        使用するモデル別名を指定");
    Console.WriteLine("  --list-models          モデル一覧を表示して終了");
    Console.WriteLine("  --skip-ep-download     実行プロバイダーのダウンロードをスキップ");
    Console.WriteLine("  --skip-model-download  モデルダウンロードをスキップ");
}

void PrintFriendlyError(Exception ex)
{
    string summary = ex.Message
        .Replace("\r", " ")
        .Replace("\n", " ");

    int separatorIndex = summary.IndexOf(" ---> ", StringComparison.Ordinal);
    if (separatorIndex >= 0)
    {
        summary = summary[..separatorIndex];
    }

    int stackTraceIndex = summary.IndexOf("   at ", StringComparison.Ordinal);
    if (stackTraceIndex >= 0)
    {
        summary = summary[..stackTraceIndex];
    }

    Console.Error.WriteLine();
    Console.Error.WriteLine("起動に失敗しました。");
    Console.Error.WriteLine($"例外: {ex.GetType().Name}");
    Console.Error.WriteLine(summary.Trim());
    Console.Error.WriteLine();
    Console.Error.WriteLine("確認ポイント:");
    Console.Error.WriteLine("  1. ネットワーク接続があり、Foundry Local のモデルカタログへアクセスできること");
    Console.Error.WriteLine("  2. 実行ディレクトリ配下の .foundry-local-data に書き込みできること");
    Console.Error.WriteLine("  3. 初回起動ではモデル取得に時間がかかること");
}

internal sealed record AppOptions(
    string ModelAlias,
    bool ShowHelp,
    bool ListModelsOnly,
    bool SkipEpDownload,
    bool SkipModelDownload);
