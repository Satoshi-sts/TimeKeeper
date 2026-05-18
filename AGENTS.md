# Codex Rules for TimeKeeper

このリポジトリは Windows WPF アプリ `TimeKeeper` のソースコード管理用です。

## 恒久ルール

- 既存の通知機能を壊さない。
- Velopack によるアプリ内更新機能を壊さない。
- GitHub Personal Access Token などの認証情報をアプリに埋め込まない。
- 開発用リポジトリ `Satoshi-sts/TimeKeeper` をアプリ更新元にしない。
- アプリ更新元は `Satoshi-sts/TimeKeeper-Releases` の GitHub Releases とする。
- `TimeKeeper-Releases` にはソースコードを置かず、Velopack の生成物のみ配置する。
- 通常のコード修正では `dotnet build NotificationTabApp\NotificationTabApp.csproj` まで実施する。
- ユーザーが明示的に「リリース」「更新配布」「Release作成」などを依頼した場合のみ、`docs/RELEASE.md` の手順に従ってバージョンアップ、publish、`vpk pack`、GitHub Releases アップロードを行う。
- リリースノートは日本語で作成する。
- 作業後は変更ファイル、ビルド結果、リリース実施有無を報告する。

## リリース作業の参照先

TimeKeeper のリリース手順は `docs/RELEASE.md` を参照すること。
