# Contributing to TimeKeeper

Thank you for your interest in TimeKeeper. This project is a newly public Windows desktop app and is currently maintained by the main maintainer. Contributions are welcome when they are focused, understandable, and aligned with the existing app behavior.

## Useful Context

- Source repository: `Satoshi-sts/TimeKeeper`
- Update artifact repository: `Satoshi-sts/TimeKeeper-Releases`
- Main project: `NotificationTabApp/NotificationTabApp.csproj`
- Release process: `docs/RELEASE.md`
- App settings location: `%LOCALAPPDATA%\NotificationTabApp\settings.json`

Do not use the source repository as the app update source. Do not embed GitHub tokens, Personal Access Tokens, passwords, signing keys, or other credentials in the app.

## Development Setup

Requirements:

- Windows
- .NET 8 SDK

Build:

```powershell
dotnet build NotificationTabApp\NotificationTabApp.csproj
```

## Contribution Guidelines

- Keep changes small and focused.
- Follow the existing WPF and C# patterns in the codebase.
- Avoid large refactors unless they are discussed first.
- Keep user-facing text consistent with the current Japanese UI.
- Preserve existing notification behavior.
- Preserve the Velopack in-app update flow.
- Do not modify release packaging or create a release unless explicitly requested by the maintainer.

## Pull Request Checklist

Before opening a pull request:

- Confirm the change is scoped to one purpose.
- Run `dotnet build NotificationTabApp\NotificationTabApp.csproj`.
- Update documentation if behavior changes.
- Add or update release notes only when preparing a release.
- Avoid committing generated output such as `publish/`, `Releases/`, `bin/`, or `obj/`.

## Reporting Issues

When reporting bugs, include:

- TimeKeeper version
- Windows version
- Whether the app was installed with `TimeKeeper-win-Setup.exe` or run from a local build
- Steps to reproduce
- Expected behavior
- Actual behavior

Please do not include private reminder text or other personal data unless it is necessary and safe to share.
