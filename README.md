# TimeKeeper

TimeKeeper is an open-source Windows desktop notification app for lightweight, non-intrusive reminders while working, gaming, or using fullscreen applications.

The app provides a small persistent tab-style UI, real-world time reminders, Final Fantasy XIV Eorzea Time reminders, notification groups, mute and skip controls, upcoming reminders, and Velopack-based in-app update workflows.

This repository was recently made public and is actively maintained by the main maintainer.

短い補足: TimeKeeper は Windows デスクトップ上で軽量な通知を扱うための WPF アプリです。

## What Is TimeKeeper?

TimeKeeper is a C# / .NET WPF application that stays on the Windows desktop as a small translucent tab. Users can register reminders, choose real-world time or Eorzea Time, and receive compact popup notifications near the main tab.

The project is designed for situations where reminders should be visible but not disruptive. It does not read Final Fantasy XIV game state, logs, or process memory; Eorzea Time is calculated from the PC clock.

TimeKeeper is not affiliated with Square Enix or Final Fantasy XIV.

## Key Features

- Persistent translucent tab window for quick access
- Always-on-top toggle
- Global mute toggle
- Real-world time reminders
- Eorzea Time reminders
- One-time reminders
- Multiple time ranges per reminder
- Notification groups and group-level enable/disable controls
- Registered notification list with group folding, enable/disable controls, deletion mode, and real-time fuzzy search
- Upcoming reminder list with skip controls
- Compact notification popups with copyable text
- Popup actions for opening or muting the related notification
- Settings persistence under `%LOCALAPPDATA%\NotificationTabApp\settings.json`
- In-app update check and update flow powered by Velopack

## Use Cases

- Lightweight reminders during desk work
- Non-intrusive reminders while gaming or using fullscreen applications
- Repeating reminders based on the local PC clock
- Eorzea Time based reminders for Final Fantasy XIV activities
- Temporary one-time reminders that should disappear after firing or being skipped

## Installation

End-user release artifacts are published separately from the source repository:

- Release repository: https://github.com/Satoshi-sts/TimeKeeper-Releases
- Download the latest `TimeKeeper-win-Setup.exe` from the release assets.
- Install and launch TimeKeeper.

The source repository does not use its own GitHub Releases as the in-app update source. Velopack update artifacts are published to `Satoshi-sts/TimeKeeper-Releases`.

## How To Build From Source

Requirements:

- Windows
- .NET 8 SDK

Build:

```powershell
dotnet build NotificationTabApp\NotificationTabApp.csproj
```

Run from the built output, or open the project in Visual Studio / Rider and run the WPF application.

For release packaging, see [docs/RELEASE.md](docs/RELEASE.md). Release packaging uses Velopack and should only be done when publishing an app update.

## How To Use

The main TimeKeeper tab contains quick-access buttons:

- Pin: toggle always-on-top behavior.
- Mute: pause TimeKeeper popups and sounds.
- Registered notifications: open the registered notification list.
- Upcoming reminders: open the next scheduled reminders.
- Add: create a normal notification or a one-time reminder.
- Update: open the in-app update window.

When adding a reminder, choose a title, optional group, time mode, one or more time ranges, and the notification message. Normal reminders appear in the registered notification list. One-time reminders appear in upcoming reminders and are removed after firing or being skipped.

## Project Status

TimeKeeper is a newly public Windows desktop app. It is currently maintained by the main maintainer, with recent releases covering:

- Velopack-based in-app updates
- Notification popup improvements
- Multiple time ranges
- Registered notification search
- Main tab navigation changes

The current app version is tracked in [NotificationTabApp.csproj](NotificationTabApp/NotificationTabApp.csproj).

## Roadmap

Planned improvements may include:

- Documentation cleanup as the public repository matures
- More contributor-friendly issue labels and examples
- Expanded automated checks
- Optional reminder features such as weekday/date rules
- Optional startup or tray integration
- UI refinements based on user feedback

The following items are not currently implemented: weekday-specific reminders, date-specific reminders, notification sound selection, notification volume control, cloud sync, multi-device sync, user accounts, theme switching, and detailed notification history.

## Contributing

Contributions are welcome, especially bug reports, small fixes, documentation improvements, and focused feature proposals. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

For normal code changes, run:

```powershell
dotnet build NotificationTabApp\NotificationTabApp.csproj
```

Do not embed GitHub Personal Access Tokens or other credentials in the app. The update source must remain `Satoshi-sts/TimeKeeper-Releases`, not this source repository.

## Reporting Bugs / Requesting Features

Use GitHub Issues:

- Bug reports: include the app version, Windows version, steps to reproduce, expected behavior, and actual behavior.
- Feature requests: describe the workflow, why the feature is useful, and any relevant constraints.

Please avoid posting private reminder content or personal data in public issues.

## License

TimeKeeper is licensed under the MIT License. See [LICENSE](LICENSE).

## Maintainer

Maintained by [Satoshi-sts](https://github.com/Satoshi-sts).
