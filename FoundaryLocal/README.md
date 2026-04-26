# WorkFoundaryLocal

`Foundry Local` の使い方を学ぶための最小コンソールサンプルです。  
起動すると次の流れをそのまま体験できます。

1. `FoundryLocalManager` を初期化する
2. 実行プロバイダーをダウンロード / 登録する
3. モデルカタログからモデルを選ぶ
4. モデルをダウンロードしてロードする
5. チャットをストリーミング応答で実行する
6. 終了時にモデルをアンロードする

このサンプルは学習しやすさを優先して、最新の標準パッケージ `Microsoft.AI.Foundry.Local` を使用しています。

## 実行方法

```powershell
dotnet run --project .\WorkFoundaryLocal\WorkFoundaryLocal.csproj
```

別のモデル別名を使いたい場合は、起動時に表示されたモデル ID を引数または環境変数で指定できます。

```powershell
dotnet run --project .\WorkFoundaryLocal\WorkFoundaryLocal.csproj -- your-model-alias
```

```powershell
$env:FOUNDRY_MODEL_ALIAS = "your-model-alias"
dotnet run --project .\WorkFoundaryLocal\WorkFoundaryLocal.csproj
```

モデル一覧だけを見たい場合や、初回ダウンロードを避けて流れだけ確認したい場合は次を使えます。

```powershell
dotnet run --project .\WorkFoundaryLocal\WorkFoundaryLocal.csproj -- --list-models --skip-ep-download
```

```powershell
dotnet run --project .\WorkFoundaryLocal\WorkFoundaryLocal.csproj -- --help
```

## 画面内コマンド

- `/help`: 学習ポイントとコマンドを再表示
- `/reset`: 会話履歴をリセット
- `/history`: 保持している会話メッセージ数を表示
- `exit` / `quit`: 終了

## 起動オプション

- `--help`: 起動オプションを表示
- `--model <alias>`: 使用するモデル別名を指定
- `--list-models`: モデル一覧を表示して終了
- `--skip-ep-download`: 実行プロバイダーのダウンロードをスキップ
- `--skip-model-download`: モデルのダウンロードをスキップ

## 補足

- 既定のモデル別名は `qwen2.5-0.5b` です。
- Foundry Local のアプリデータ、ログ、モデルキャッシュは実行ディレクトリ配下の `.foundry-local-data` に保存します。
- 指定したモデルが見つからない場合は、`phi-4-mini` などの学習しやすい小さめモデルを優先してフォールバックします。
- 初回起動ではモデル本体や実行プロバイダーのダウンロードが走るため、少し時間がかかります。
