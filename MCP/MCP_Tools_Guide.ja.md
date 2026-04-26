# MCPツール設計ガイド

## 目的

このドキュメントは、MCPツールを実装する際に属性へどのような値を設定すべきかを整理し、あわせて一般的なMCPツールの種類を資料としてまとめたものです。

このワークスペースでは、C# の属性として `McpServerToolType`、`McpServerTool`、`Description` を利用してツールを公開しています。実装例は `SampleMcpServer.Tools` プロジェクトにある `EchoTool`、`CalculatorTool`、`SystemInfoTool` を参照できます。

---

## このサンプルで使っている属性

このサンプルでは、tool だけでなく resource / prompt も公開する構成に拡張しています。現在使っている主な属性は次のとおりです。

- `McpServerToolType`
- `McpServerTool`
- `McpServerResourceType`
- `McpServerResource`
- `McpServerPromptType`
- `McpServerPrompt`
- `Description`

### 1. `McpServerToolType`

ツールをまとめるクラスに付与します。

```csharp
[McpServerToolType]
public sealed class EchoTool
{
}
```

#### 設定方針

- 1つの責務ごとにクラスを分ける
- クラス名はツール群の役割が伝わる名前にする
- stateless なツールは `static` メソッド中心で構成する
- 公開する意図があるクラスにのみ付与する

#### 推奨例

- `EchoTool`
- `CalculatorTool`
- `SystemInfoTool`
- `FileSearchTool`
- `WeatherTool`

#### 避けたい例

- `Tool1`
- `MiscTool`
- `Helper`

役割が曖昧な名前は、クライアントやAIがツールの用途を理解しにくくなります。

---

### 2. `McpServerTool`

MCPに公開するメソッドに付与します。

```csharp
[McpServerTool]
public static double Add(double a, double b) => a + b;
```

#### 設定方針

- 1メソッド = 1ツール操作として明確にする
- メソッド名は動詞から始め、挙動が一意にわかるようにする
- 例外発生条件がある場合は `Description` に明記する
- 非同期I/Oを伴う処理は `Async` サフィックスを検討する

#### 推奨例

- `Add`
- `Subtract`
- `GetRuntimeInfo`
- `SearchFiles`
- `FetchWeatherForecastAsync`

#### 避けたい例

- `Do`
- `Run`
- `Execute`
- `Process`

汎用的すぎる名前は、ツール選択時の精度を下げます。

---

### 3. `Description`

クラス・メソッド・パラメーターの意味を自然言語で補足します。

```csharp
[McpServerTool, Description("Adds two numbers.")]
public static double Add(
    [Description("Left operand.")] double a,
    [Description("Right operand.")] double b)
    => a + b;
```

#### 設定方針

- 「何をするか」を一文で明確に書く
- 実装詳細ではなく、呼び出し側が判断に必要な情報を書く
- パラメーター説明は値の意味、単位、制約を含める
- 禁止条件や前提条件があれば明記する
- AIや利用者が誤用しにくい表現にする

#### 良いメソッド説明の例

- `"Adds two numbers."`
- `"Returns the current UTC timestamp in ISO 8601 format."`
- `"Divides a by b. Throws if b is zero."`
- `"Searches files under the workspace by partial file name."`

#### 良いパラメーター説明の例

- `"The message to echo."`
- `"Denominator (must be non-zero)."`
- `"The city name in English."`
- `"Maximum number of results to return."`

#### 避けたい説明の例

- `"This method does something useful."`
- `"Parameter a."`
- `"Input value."`
- `"Executes processing."`

---

### 4. `McpServerResourceType`

resource をまとめるクラスに付与します。

```csharp
[McpServerResourceType]
public sealed class ServerResourceCatalog
{
}
```

#### 意味

- このクラスに resource 定義が含まれることを SDK に伝える
- `WithResources<T>()` や `WithResourcesFromAssembly(...)` の対象になる

#### どう利用されるか

- `resources/list`
- `resources/read`
- `resources/templates/list`

といった resource 系の capability を構成するための探索対象になる。

---

### 5. `McpServerResource`

MCP resource として公開するメソッドに付与します。

```csharp
[McpServerResource(UriTemplate = "resource://server/info", MimeType = "text/plain", Name = "server-info", Title = "Server Info")]
public static string GetServerInfo() => "...";
```

#### 主な属性値

- `UriTemplate`
  - resource の識別子またはテンプレート
  - クライアントはこの URI を使って `resources/read` する
- `MimeType`
  - 返すデータの MIME type
  - 表示方法や解釈に使われる
- `Name`
  - 安定した論理名
  - クライアントやモデルが resource を識別しやすくなる
- `Title`
  - UI 表示向けのタイトル

#### どう利用されるか

- `resources/list` で resource 一覧に出る
- `resources/templates/list` でテンプレート扱いの resource が出る
- `resources/read` で実体を返す

---

### 6. `McpServerPromptType`

prompt をまとめるクラスに付与します。

```csharp
[McpServerPromptType]
public sealed class ServerPromptCatalog
{
}
```

#### 意味

- このクラスに prompt 定義が含まれることを SDK に伝える
- `WithPrompts<T>()` や `WithPromptsFromAssembly(...)` の対象になる

#### どう利用されるか

- `prompts/list`
- `prompts/get`

の公開対象を構成する。

---

### 7. `McpServerPrompt`

prompt として公開するメソッドに付与します。

```csharp
[McpServerPrompt(Name = "server-overview", Title = "Server Overview")]
public static GetPromptResult GetServerOverview(string audience = "developer") => ...;
```

#### 主な属性値

- `Name`
  - prompt の安定名
  - `prompts/get` の対象識別子になる
- `Title`
  - UI 表示用の見出し
- `Description`
  - 何のための prompt かを説明する

#### どう利用されるか

- `prompts/list` で prompt 一覧に出る
- `prompts/get` で引数付きテンプレートとして取得される

---

## 属性値を決める際の実践ルール

### クラス名

- ドメイン単位でまとめる
- 末尾に `Tool` を付けると意図が明確になる
- 1クラスに unrelated な機能を詰め込まない

**例:**

- `CalculatorTool` は妥当
- `MathAndSystemAndFileTool` は不適切

### メソッド名

- できるだけ短く、意味は具体的にする
- `Get` `Create` `Update` `Delete` `Search` `Convert` などの動詞を優先する
- 同じ概念に対して命名規則を揃える

**例:**

- `GetUtcNow`
- `GetRuntimeInfo`
- `Reverse`
- `SearchDocuments`

### 説明文

- 英語で統一すると、多くのMCPクライアントやモデルとの親和性が高い
- 先頭は動詞で始める短文が扱いやすい
- 出力形式がある場合は明記する
- 制約は括弧または2文目で補足する

**例:**

- `"Returns weather data for the specified city."`
- `"Converts Markdown text to HTML."`
- `"Creates a task item and returns its identifier."`

### パラメーター

- 省略可能か必須かを型や説明で明確にする
- 曖昧な `value` `data` `input` より、意味のある名称を使う
- 複数候補がある場合は列挙値の意味を説明する

**例:**

- `cityName`
- `maxResults`
- `includeHiddenFiles`
- `format`

---

## このサンプルコードから学べるポイント

### `EchoTool`

- 用途が明確なクラス名
- `Echo` と `Reverse` という短く具体的なメソッド名
- `message` パラメーターの意味が明確

### `CalculatorTool`

- 四則演算という同一責務でまとまっている
- `Divide` ではゼロ除算の制約を `Description` と例外で表現している
- AIが事前に失敗条件を理解しやすい

### `SystemInfoTool`

- 情報取得系の責務に限定されている
- `GetUtcNow` で出力形式を明記している
- `GetRuntimeInfo` で返却内容の概要を説明している

---

## 一般的なMCPツールの種類

MCPツールは、LLMが外部機能を安全かつ明示的に呼び出すための窓口です。一般的には以下のようなカテゴリがあります。

### 1. 情報取得ツール

外部または内部の情報を取得するツールです。

例:

- 現在時刻取得
- システム情報取得
- 天気取得
- 株価取得
- ナレッジベース検索
- FAQ検索

**代表的なメソッド例**

- `GetUtcNow`
- `GetWeather`
- `SearchKnowledgeBase`
- `GetServerStatus`

### 2. 計算・変換ツール

純粋関数に近い処理を提供します。

例:

- 四則演算
- 単位変換
- 文字列変換
- MarkdownからHTMLへの変換
- JSON整形

**代表的なメソッド例**

- `Add`
- `ConvertTemperature`
- `Reverse`
- `FormatJson`

### 3. ファイル操作ツール

ローカルまたは仮想ファイルシステムを操作します。

例:

- ファイル検索
- ファイル読み込み
- ファイル書き込み
- ディレクトリ一覧取得

**注意点**

- 対象ディレクトリを制限する
- 上書き可否を明示する
- パス traversal 対策を行う

### 4. データアクセスツール

データベースやストレージの読み書きを行います。

例:

- 顧客一覧取得
- 注文検索
- 在庫更新
- ドキュメント保存

**注意点**

- 読み取り系と更新系を分ける
- 権限と監査を考慮する
- 失敗時の返却や例外方針を明確にする

### 5. 外部API連携ツール

HTTP APIやSaaSとの連携を行います。

例:

- GitHub Issue作成
- Teams通知送信
- Slack投稿
- Maps APIによる位置検索

**注意点**

- タイムアウト設定
- リトライ方針
- レート制限対策
- 認証情報の安全管理

### 6. 業務操作ツール

業務アプリケーション上の明確な操作を提供します。

例:

- チケット作成
- 承認申請
- 顧客登録
- 請求書生成

**推奨事項**

- 破壊的操作は特に説明を明確にする
- 監査ログを残す
- 必要なら dry-run 相当の確認用ツールを用意する

### 7. 開発支援ツール

開発者向けの支援機能です。

例:

- ビルド実行
- テスト実行
- ログ検索
- API仕様検索
- ソースコード検索

### 8. AI補助ツール

LLMの判断を補助するための専門ツールです。

例:

- 類似文書検索
- ベクトル検索
- プロンプトテンプレート取得
- 用語辞書参照

---

## よくある設計パターン

## `AddMcpServer` で設定できる主な項目

このサンプルでは `AddMcpServer(options => ...)` に対して、利用価値が高い項目をできるだけ明示設定する方針を採っています。

```csharp
builder.Services.AddMcpServer(options =>
{
    // 代表例
    options.ProtocolVersion = "2024-11-05";
    options.ServerInstructions = "...";
    options.InitializationTimeout = TimeSpan.FromSeconds(30);
    options.ScopeRequests = true;
    options.SendTaskStatusNotifications = true;
    options.MaxSamplingOutputTokens = 2048;
});
```

### `McpServerOptions` の主要項目

#### `ProtocolVersion`

- サーバーが前提とする MCP プロトコルバージョン
- `initialize` 応答時の互換性判断に使われる

#### `ServerInfo`

- サーバー名、タイトル、バージョン、説明、Web サイト URL などのメタデータ
- クライアントに「このサーバーが何者か」を伝える

#### `ServerInstructions`

- クライアントやモデル向けの利用ガイド
- 「どの tool / resource / prompt をどう使うべきか」の説明に使う

#### `InitializationTimeout`

- `initialize` フェーズで許容するタイムアウト
- 遅い初期化処理を持つ場合のフェイルファスト制御に使う

#### `ScopeRequests`

- リクエストごとにスコープを分けるかどうか
- DI スコープ境界や request 単位のサービス寿命に影響する

#### `SendTaskStatusNotifications`

- タスク進捗通知を有効にするかどうか
- 長時間処理の可視化に使う

#### `MaxSamplingOutputTokens`

- sampling 系で返す最大トークン数の上限
- 出力サイズ制御に使う

#### `Capabilities`

- サーバーがどの MCP capability を提供するかを宣言する
- `initialize` 応答の中心情報

このサンプルでは次を明示設定している。

- `Logging`
- `Prompts`
- `Resources`
- `Tools`
- `Completions`
- `Extensions`
- `Experimental`

##### `PromptsCapability.ListChanged`

- prompt 一覧の変更通知をサポートするか

##### `ResourcesCapability.ListChanged`

- resource 一覧の変更通知をサポートするか

##### `ResourcesCapability.Subscribe`

- resource subscribe / unsubscribe をサポートするか

##### `ToolsCapability.ListChanged`

- tool 一覧の変更通知をサポートするか

#### `Handlers`

- 明示的に上書き・追加する各種リクエストハンドラー

この SDK では少なくとも次がある。

- `ListToolsHandler`
- `CallToolHandler`
- `ListResourcesHandler`
- `ListResourceTemplatesHandler`
- `ReadResourceHandler`
- `SubscribeToResourcesHandler`
- `UnsubscribeFromResourcesHandler`
- `ListPromptsHandler`
- `GetPromptHandler`
- `CompleteHandler`
- `SetLoggingLevelHandler`

このサンプルでは特に以下を明示している。

- `SetLoggingLevelHandler`
  - クライアントからの logging level 変更要求を受ける
- `CompleteHandler`
  - prompt / tool パラメーター補完候補を返す
- `SubscribeToResourcesHandler`
  - 非対応であることを明示的に返す
- `UnsubscribeFromResourcesHandler`
  - 非対応であることを明示的に返す

#### `Filters`

- message 単位・request 単位のパイプラインフィルター
- ログ、認可、検証、監査に利用できる

##### `Filters.Message`

- 受信メッセージ / 送信メッセージ全体に対するフィルター
- このサンプルでは incoming / outgoing の debug ログを付与している

##### `Filters.Request`

- 各 MCP メソッドごとのフィルター
- このサンプルでは以下に対して debug ログを付与している

- `tools/list`
- `tools/call`
- `resources/list`
- `resources/templates/list`
- `resources/read`
- `prompts/list`
- `prompts/get`
- `completion`
- `logging/setLevel`
- `resources/subscribe`
- `resources/unsubscribe`

#### `ToolCollection` / `ResourceCollection` / `PromptCollection`

- 登録済みの tool / resource / prompt の集合
- 通常は `WithTools<T>()`、`WithResources<T>()`、`WithPrompts<T>()` で構成する

#### `TaskStore`

- MCP task を保存・追跡するストア
- 長時間処理や task API を本格運用する場合の拡張点

#### `KnownClientInfo` / `KnownClientCapabilities`

- 特定の既知クライアントに合わせた最適化や制御に使える情報
- 今回のサンプルでは積極利用していないが、クライアント差分吸収のためのフックになる

### どのような意味で利用されるか

`AddMcpServer` の設定は、主に次の 3 箇所で利用される。

1. **`initialize` 応答**  
   `ProtocolVersion`、`ServerInfo`、`Capabilities`、`ServerInstructions` がクライアントへ通知される。

2. **各種 request の実行パイプライン**  
   `Handlers` と `Filters` が request 処理中に使われる。

3. **実行時の安全性・運用性**  
   `InitializationTimeout`、`ScopeRequests`、`MaxSamplingOutputTokens`、`TaskStore` などが運用特性に効く。

### 設定方針

- capability は「実装しているものだけ」を有効化する
- subscribe 非対応なら capability と handler の両方で意図を一致させる
- サーバー説明はクライアントが読んで判断できる文章にする
- filter は便利だが、過剰なログ出力は避ける

### パターン1: 小さく明確なツール

1つのツールが1つの明確な操作だけを行う方式です。

**利点**

- 説明しやすい
- AIが選択しやすい
- テストしやすい

### パターン2: ドメインごとにグループ化したツールクラス

同じ業務領域の操作を1クラスにまとめます。

**例**

- `CustomerTool`
- `InvoiceTool`
- `RepositoryTool`

### パターン3: 読み取り系と更新系の分離

副作用のある操作とない操作を分けます。

**例**

- `CustomerQueryTool`
- `CustomerCommandTool`

この分離は、AIに対して安全性を伝える上でも有効です。

---

## 推奨テンプレート

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace SampleMcpServer.Tools;

[McpServerToolType]
public sealed class ExampleTool
{
    [McpServerTool, Description("Returns a normalized greeting message for the specified name.")]
    public static string GetGreeting(
        [Description("The display name of the user.")] string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return $"Hello, {name}!";
    }
}
```

このテンプレートでは、次の点が満たされています。

- クラス名が責務を表している
- メソッド名が具体的である
- メソッド説明が短く明確である
- パラメーター説明が利用者視点で書かれている
- 入力値検証がある

---

## 属性値の推奨チェックリスト

MCPツールを追加する前に、次を確認してください。

- クラス名から責務がわかるか
- メソッド名から動作がわかるか
- `Description` が曖昧でないか
- パラメーター説明に制約や単位が含まれているか
- 失敗条件が利用者に伝わるか
- 副作用の有無が明確か
- セキュリティ上危険な操作になっていないか
- AIが誤用しにくい設計になっているか

---

## まとめ

MCPツールの属性値は、単なるメタデータではなく、クライアントやAIがツールを正しく選択し、安全に利用するためのインターフェース定義です。

特に重要なのは次の3点です。

1. クラス名とメソッド名を具体的にする
2. `Description` に利用者が必要とする情報を書く
3. 制約、失敗条件、副作用を明示する

このサンプルでは、`EchoTool`、`CalculatorTool`、`SystemInfoTool` がその基本形になっています。今後ツールを追加する場合も、同じ方針で命名と説明を揃えることで、使いやすく保守しやすいMCPサーバーになります。
