# ShadowPlay Reminder Xbox Game Bar Widget

This sample implements a pure Xbox Game Bar **UWP/XAML** widget that keeps an eye on the temporary ShadowPlay files under
`C:\Users\<you>\AppData\Local\Temp\9343b833-e7af-42ea-8a61-31bc41eefe2b`. When a game starts and no recent
`Sha*.tmp` activity is detected the widget uses the Game Bar notification SDK to prompt you to switch ShadowPlay back on.

## Features

- Built as a C# UWP project with XAML UI, Game Bar target tracking, and a file-system watcher for ShadowPlay.
- Uses `XboxGameBarAppTargetTracker` so reminders only fire when the active target is a game.
- Leverages `StorageFileQueryResult` change events instead of polling so ShadowPlay updates surface instantly.
- Sends Game Bar notifications (with a toast fallback) when ShadowPlay appears to be off.
- Requires the `broadFileSystemAccess` capability to observe the ShadowPlay temp directory.

## Project layout

```
shadowplay-gamebar-widget/
├── App.xaml                          # Application entry point
├── App.xaml.cs
├── MainPage.xaml                     # Widget UI
├── MainPage.xaml.cs                  # UI + notification logic
├── Models/                           # Event argument + enums for ShadowPlay state
├── Services/ShadowPlayMonitor.cs     # File watcher + status evaluation
├── Properties/                       # Assembly metadata + .rd.xml
├── ShadowPlayReminderWidget.csproj   # UWP project file
├── Assets/                           # Icons referenced by the manifest
└── package.appxmanifest              # UWP manifest with Game Bar widget extension
```

## Building and sideloading

1. Open the project in Visual Studio 2019 (or newer) with the latest Windows 10 SDK installed.
2. Ensure the **ShadowPlayReminderWidget** project is selected, then choose **Build** > **Publish** > **Create App Packages**.
3. After installing the package, visit **Settings** > **Privacy & security** > **File system** and enable access for *ShadowPlay Reminder*.
4. Launch any game, open the Xbox Game Bar (Win + G), pin *ShadowPlay Reminder*, and it will warn you when a game starts without ShadowPlay activity.

## Continuous integration packaging

The GitHub Actions workflow (`Build ShadowPlay widget package`) now restores NuGet packages, builds the UWP project with
`msbuild`, creates an unsigned APPX in Release/x64 mode, and uploads the output as the `shadowplay-reminder-appx` artifact.
You can download the artifact from any run to sideload or sign it locally.

## Troubleshooting

- If the status reports that the ShadowPlay directory cannot be accessed, confirm that you granted file system access in the Windows privacy settings.
- Game Bar notifications respect the user's widget notification settings. If they are disabled the widget falls back to a Windows toast notification.
