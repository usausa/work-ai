# Sample MCP Servers (.NET 10)

.NET 10 で実装した MCP (Model Context Protocol) サーバーのサンプル 2 種。
共通のツール定義 (`SampleMcpServer.Tools`) を、stdio / HTTP の 2 つのトランスポート実装から再利用する構成。

| 項目 | Stdio | HTTP |
|------|-------|------|
| プロジェクト | `SampleMcpServer.Stdio` | `SampleMcpServer.Http` |
| SDK | `Microsoft.NET.Sdk` | `Microsoft.NET.Sdk.Web` |
| MCP パッケージ | `ModelContextProtocol` 1.2.0 | `ModelContextProtocol.AspNetCore` 1.2.0 |
| ホスト | `Microsoft.Extensions.Hosting` 10.0.7 | ASP.NET Core (Kestrel) |
| トランスポート | stdio | Streamable HTTP (`/mcp`) |
| NativeAOT | **対応** (`PublishAot=true`) | 非対応 (ASP.NET Core 側に AOT 非対応コードパスあり) |
| 用途 | エージェントの子プロセスとして起動 | 共有 / リモート MCP サーバー |

すべて `net10.0` ターゲット。最新版 NuGet パッケージを使用 (2026-04-26 時点)。

### 採用パッケージ

| パッケージ | バージョン | リリース日 | 用途 |
|------------|------------|------------|------|
| `ModelContextProtocol` | 1.2.0 | 2026-03-27 | Stdio サーバー (Hosting 統合込み) |
| `ModelContextProtocol.Core` | 1.2.0 | 2026-03-27 | Tools 共通ライブラリ用 (最小依存) |
| `ModelContextProtocol.AspNetCore` | 1.2.0 | 2026-03-27 | HTTP サーバー (Streamable HTTP) |
| `Microsoft.Extensions.Hosting` | 10.0.7 | 2026-04-21 | Stdio サーバーの Generic Host |

## ディレクトリ構成

```
.
├─ SampleMcpServer.slnx              ソリューション (.NET 10 の新 XML 形式)
├─ global.json                       SDK バージョン固定 (10.0.202)
├─ SampleMcpServer.Tools/            共通ツール (class lib, IsAotCompatible=true)
│   ├─ EchoTool.cs                   Echo / Reverse
│   ├─ CalculatorTool.cs             Add / Subtract / Multiply / Divide
│   └─ SystemInfoTool.cs             GetUtcNow / GetRuntimeInfo
├─ SampleMcpServer.Stdio/            stdio + AOT
│   ├─ Program.cs                    Host.CreateApplicationBuilder + WithStdioServerTransport
│   └─ SampleMcpServer.Stdio.csproj  PublishAot=true
└─ SampleMcpServer.Http/             ASP.NET Core / HTTP
    ├─ Program.cs                    WebApplication + WithHttpTransport + MapMcp("/mcp")
    ├─ appsettings.json              Kestrel を localhost:5080 に固定
    └─ SampleMcpServer.Http.csproj
```

ツールクラスは `[McpServerToolType]` + 静的メソッド + `[McpServerTool]` で宣言。
両プロジェクトの `Program.cs` は `WithTools<T>()` で**型を明示登録**する
(リフレクションに依存する `WithToolsFromAssembly` は AOT 非対応のため使わない)。

## 設計のポイント

- **共通ツールライブラリで両トランスポートに同じ実装を載せる** — ツール定義は MCP プロトコルから独立しているので、stdio と HTTP の双方から `ProjectReference` するだけで再利用できる。MCP の典型的な価値 (同じツールをローカル/リモートどちらでも使える) を反映した構成。
- **`WithTools<T>()` を使う (AOT 互換)** — `WithToolsFromAssembly()` はアセンブリスキャンにリフレクションを使うため AOT で動かない。型を 1 つずつ明示するこの書き方なら、AOT 発行でも警告ゼロで通る。
- **ログは stderr に固定 (stdio 版)** — `Logging.AddConsole` の `LogToStandardErrorThreshold = Trace` 設定で stdout を MCP プロトコルバイト列専用に保つ。stdout にログが混ざると JSON-RPC が壊れるため必須。
- **HTTP は `/mcp` にマップ + デフォルトでセッション必須** — Streamable HTTP の仕様準拠。`initialize` 応答ヘッダー `Mcp-Session-Id` を以降のリクエストに付ける必要がある。検証用途で簡略化したい場合は `WithHttpTransport(o => o.Stateless = true)` でステートレス化可能 (ただしリソース購読等の状態依存機能は不可)。
- **HTTP は AOT を有効化しない** — `ModelContextProtocol.AspNetCore` のセッション管理 / SSE 経路にリフレクションや動的ペイロードシリアライズが残っており、`PublishAot=true` で警告 / 実行時失敗が出る可能性がある。Stdio 版だけ単一ネイティブバイナリ配布の恩恵を取る。
- **`InvariantGlobalization=true` で AOT バイナリを軽くする** — Stdio 側は ICU データを同梱しないことで配布サイズを 13 MB に抑えている。ローカライズ依存処理が必要になったら外す。

## ビルド

```bash
dotnet build SampleMcpServer.slnx -c Release
```

## Stdio: NativeAOT 発行と利用

```bash
dotnet publish SampleMcpServer.Stdio -c Release -r win-x64
```

成果物: `SampleMcpServer.Stdio/bin/Release/net10.0/win-x64/publish/sample-mcp-server-stdio.exe`
(約 13 MB の単体ネイティブ実行ファイル / .NET ランタイム不要)。

> Windows で AOT を使う場合、Visual Studio C++ build tools と `vswhere.exe`
> (`C:\Program Files (x86)\Microsoft Visual Studio\Installer`) が PATH または
> Developer Command Prompt 経由で参照できる必要がある。

他 RID 例:

```bash
dotnet publish SampleMcpServer.Stdio -c Release -r linux-x64
dotnet publish SampleMcpServer.Stdio -c Release -r osx-arm64
```

### Claude Desktop 設定例

`claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "sample-stdio": {
      "command": "D:\\WorkGenerative\\MCP\\SampleMcpServer.Stdio\\bin\\Release\\net10.0\\win-x64\\publish\\sample-mcp-server-stdio.exe"
    }
  }
}
```

ログは stderr (`Logging.AddConsole` で `LogToStandardErrorThreshold = Trace` を設定し stdio プロトコル本体を汚染しない)。

## HTTP: 起動と利用

```bash
dotnet run --project SampleMcpServer.Http -c Release
# → http://localhost:5080/mcp で待ち受け
```

MCP エンドポイントは Streamable HTTP (`POST /mcp`)。レスポンスは `text/event-stream` (SSE) で返る。

### Claude Desktop / Claude Code 設定例

```json
{
  "mcpServers": {
    "sample-http": {
      "url": "http://localhost:5080/mcp"
    }
  }
}
```

### curl での疎通確認

```bash
# 1) initialize → レスポンスヘッダー Mcp-Session-Id を取り出す
curl -i -X POST http://localhost:5080/mcp \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{
        "protocolVersion":"2024-11-05","capabilities":{},
        "clientInfo":{"name":"curl","version":"0.0"}}}'

# 2) 取得した Mcp-Session-Id を使って tools/list を呼ぶ
curl -X POST http://localhost:5080/mcp \
  -H 'Content-Type: application/json' \
  -H 'Accept: application/json, text/event-stream' \
  -H 'Mcp-Session-Id: <上で取得した値>' \
  -d '{"jsonrpc":"2.0","id":2,"method":"tools/list"}'
```

> セッション管理を省略したいだけなら `WithHttpTransport(o => o.Stateless = true)` でステートレス化できる
> (リソース購読など状態を必要とする MCP 機能は使えなくなる)。

## なぜ HTTP は AOT 対応にしないのか

ASP.NET Core 自体は AOT 対応路線にあるものの、
`ModelContextProtocol.AspNetCore` のセッション管理 / SSE 周りはリフレクションや
動的ペイロードのシリアライズに依存する箇所があり、現時点で `PublishAot=true` を有効化すると
警告 / 実行時失敗が発生しうる。stdio 版は単一バイナリ配布のメリットが大きいので AOT、
HTTP 版は通常の JIT 実行 (`dotnet run` / `dotnet publish`) で運用するのが今は現実的。

## 公開ツール一覧

| ツール | パラメーター | 戻り値 |
|--------|--------------|--------|
| `Echo` | `message: string` | `"Echo: <message>"` |
| `Reverse` | `message: string` | 反転した文字列 |
| `Add` / `Subtract` / `Multiply` | `a: number`, `b: number` | `number` |
| `Divide` | `a: number`, `b: number` (b≠0) | `number` (0 除算時 `McpException`) |
| `GetUtcNow` | (なし) | 現在 UTC ISO 8601 文字列 |
| `GetRuntimeInfo` | (なし) | OS / Arch / Framework / PID / Hostname の複数行文字列 |

## 動作確認結果 (2026-04-26)

| 項目 | 結果 |
|------|------|
| `dotnet build SampleMcpServer.slnx -c Release` | 0 警告 / 0 エラー |
| `dotnet publish SampleMcpServer.Stdio -c Release -r win-x64` | AOT 発行成功、`sample-mcp-server-stdio.exe` 約 13 MB の単体ネイティブ生成 |
| Stdio スモークテスト (`initialize` + `tools/list` を stdin 投入) | 両ハンドラが 0.1ms 未満で完了。stderr に正常な Hosting / MCP ログ |
| HTTP `dotnet run` 起動 | `http://localhost:5080/mcp` で待ち受け |
| HTTP `POST /mcp` (`initialize`) | `text/event-stream` で `protocolVersion`, `serverInfo: sample-mcp-server-http 1.0.0.0` を含む正常応答 |
| HTTP `POST /mcp` (`tools/list`、セッション ID なし) | プロトコル準拠のセッション必須エラー (`-32000 Bad Request`) を返却 = 仕様通り |
