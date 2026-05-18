# TimeKeeper リリース手順

TimeKeeper は Velopack を使ってアプリ内更新を配布する。開発用リポジトリ `Satoshi-sts/TimeKeeper` はソースコード管理用であり、アプリの更新元には使わない。更新パッケージは Public リポジトリ `Satoshi-sts/TimeKeeper-Releases` の GitHub Releases に配置する。

## 1. リリースを作成するタイミング

コード修正のたびに GitHub Release は作らない。

Release は、ユーザーにアプリ内更新として配布したいタイミングで作成する。通常のコード修正や動作確認だけであれば `dotnet build` まででよい。

## 2. バージョン更新

`NotificationTabApp/NotificationTabApp.csproj` の以下を更新する。

```xml
<Version>0.1.3</Version>
<AssemblyVersion>0.1.3.0</AssemblyVersion>
<FileVersion>0.1.3.0</FileVersion>
<InformationalVersion>0.1.3</InformationalVersion>
```

例:

```text
0.1.2 -> 0.1.3
```

`vpk pack` の `--packVersion` には `0.1.3` のような3桁バージョンを使う。

## 3. リリースノート作成

リリースごとにリリースノートを作成する。
リリースノート本文は日本語で書く。

例:

```text
release-notes-0.1.3.md
```

内容例:

```markdown
TimeKeeper v0.1.3

- ミュートボタンを追加しました
- 通知ミュート中は通知ポップアップと通知音を停止します
```

## 4. ビルド

```powershell
dotnet build NotificationTabApp\NotificationTabApp.csproj
```

ビルドエラーがある状態でリリースを作らない。

## 5. publish

```powershell
dotnet publish NotificationTabApp\NotificationTabApp.csproj -c Release -r win-x64 -o publish
```

`publish` フォルダはリリース作業用の生成物であり、ソースコードとして管理しない。

## 6. Velopack パッケージ作成

```powershell
vpk pack --packId TimeKeeper --packVersion 0.1.3 --packDir publish --mainExe NotificationTabApp.exe --packTitle TimeKeeper --outputDir Releases --releaseNotes release-notes-0.1.3.md
```

`Releases` フォルダに以下のようなファイルが生成される。

```text
TimeKeeper-0.1.3-full.nupkg
TimeKeeper-0.1.3-delta.nupkg
TimeKeeper-win-Setup.exe
TimeKeeper-win-Portable.zip
releases.win.json
assets.win.json
RELEASES
```

delta パッケージは、既存の過去バージョンが `Releases` フォルダに残っている場合に生成される。

## 7. GitHub Releases アップロード

アップロード先は `Satoshi-sts/TimeKeeper-Releases` とする。`Satoshi-sts/TimeKeeper` にはアップロードしない。

標準コマンド:

```powershell
gh release create v0.1.3 `
  .\Releases\TimeKeeper-0.1.3-full.nupkg `
  .\Releases\TimeKeeper-0.1.3-delta.nupkg `
  .\Releases\TimeKeeper-win-Setup.exe `
  .\Releases\TimeKeeper-win-Portable.zip `
  .\Releases\releases.win.json `
  .\Releases\assets.win.json `
  .\Releases\RELEASES `
  --repo Satoshi-sts/TimeKeeper-Releases `
  --title "TimeKeeper v0.1.3" `
  --notes-file .\release-notes-0.1.3.md
```

delta パッケージが存在しない場合は、該当する `.nupkg` の引数を外す。

## 8. アプリ内更新確認

1. インストール済みの TimeKeeper を起動する。
2. 通常画面の歯車ボタンから更新画面を開く。
3. `更新を確認` を押す。
4. 最新バージョンと更新内容が表示されることを確認する。
5. `更新する` を押す。
6. ダウンロード完了後、再起動確認で `今すぐ再起動` を選ぶ。
7. 再起動後、バージョンが上がっていることを確認する。

## 9. 注意事項

- `TimeKeeper-win-Setup.exe` を再実行して更新確認しない。アプリ内更新の確認は、インストール済みアプリの更新画面から行う。
- 過去の GitHub Releases は削除しない。
- `TimeKeeper-Releases` にソースコードをアップロードしない。
- アプリに GitHub トークンや Personal Access Token を埋め込まない。
- 開発用 Private リポジトリ `Satoshi-sts/TimeKeeper` を更新元にしない。
- `Releases` フォルダ内の manifest は過去バージョン情報を含むため、リリース作業中に必要なく削除しない。
- コード署名を導入するまでは Velopack の pack 時に署名なし警告が出る。これは現時点では許容する。
