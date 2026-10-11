# SoundModes: developer notes

For building, testing and maintaining SoundModes. The user guide is [README.md](../README.md).
Spec: [SoundModes_PRD.md](planning/SoundModes_PRD.md). Progress: [task.md](planning/task.md). History: [CHANGELOG.md](../CHANGELOG.md).

How it works: SoundModes does not process audio itself. It writes Equalizer APO's `config.txt`, and Equalizer APO applies the change live.

## Prerequisites

- Equalizer APO installed and attached to your output device (in its Configurator, tick the speakers/AUX device, then reboot)
- To build: .NET 8 SDK

No environment variables or secrets are needed, so there is no `.env` file.

## Build, run, test

```powershell
dotnet restore           # packages come from nuget.org via ./nuget.config
dotnet run --project src/SoundModes
dotnet test
```

## Publish (single portable exe)

```powershell
dotnet publish src/SoundModes -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

Output: `publish\SoundModes.exe` (~155 MB, no .NET install needed). Copy it anywhere and run it.
Don't add `EnableCompressionInSingleFile`: it shrinks the exe but roughly quadruples RAM use (~26 MB → ~97 MB),
because assemblies get unpacked into private memory.

To update: exit SoundModes from the tray, replace the exe, start it again. To roll back, keep the previous exe and swap it back.

## Files it touches

| Path | Purpose |
|---|---|
| `<Equalizer APO>\config\config.txt` | Rewritten on every mode switch (temp file + atomic replace) |
| `<Equalizer APO>\config\config.txt.soundmodes-backup` | Your original config, saved once on first switch. Copy it back over `config.txt` to undo SoundModes |
| `%APPDATA%\SoundModes\settings.json` | Last mode, your Custom mode settings and Boost |
| `%APPDATA%\SoundModes\log.txt` | Log (reset at 1 MB) |
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\SoundModes` | Only when "Start with Windows" is on |

The Equalizer APO folder comes from `HKLM\SOFTWARE\EqualizerAPO\ConfigPath`, falling back to `C:\Program Files\EqualizerAPO\config`.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Red **!** icon, "Equalizer APO not found" | Install Equalizer APO, reboot, click **Check again** |
| "can't write Equalizer APO's config" | Click **Grant permission** and accept the one UAC prompt (grants the Users group Modify on the config folder) |
| Mode switches but sound doesn't change | Equalizer APO isn't attached to the device you're using: open its Configurator, tick the device, reboot. Also turn off Realtek/Dolby/Waves enhancements |
| Warning that hotkeys are in use | Another app owns that Ctrl+Alt combo; use the tray instead (rebinding is planned) |
| Something else | Check `%APPDATA%\SoundModes\log.txt` |

## Repository layout

```
README.md, LICENSE, CHANGELOG.md, CONTRIBUTING.md   top level, where GitHub looks for them
SoundModes.sln, nuget.config                        open the .sln in Visual Studio / Rider / VS Code
.github/pull_request_template.md                    filled into every new PR

src/SoundModes/                       the app
  App.xaml.cs                         controller: single instance, wiring, mode switching, error handlers,
                                      elevated --grant-permission helper mode
  Strings.cs                          all user-facing text
  Models/SoundMode.cs                 presets (as EqSettings) + Custom
  Models/EqSettings.cs                a mode's sound in slider terms -> filters + auto preamp + config text
  Models/Settings.cs                  settings.json load/save (last mode, Custom, Boost), name -> mode
  Services/EqualizerApoService.cs     locate config, status check, atomic write, backup, permission grant
  Services/VolumeService.cs           NAudio Core Audio master volume, follows default-device changes
  Services/HotkeyService.cs           RegisterHotKey on a message-only window
  Services/Autostart.cs               HKCU Run key
  Services/Log.cs                     file logger + %APPDATA% path
  Tray/TrayIcon.cs                    WinForms NotifyIcon + context menu, icons drawn at runtime
  Tray/TrayPopup.xaml                 left-click panel (follows Windows light/dark)
  Tray/Toast.xaml                     click-through "Meeting mode ON" indicator

tests/SoundModes.Tests/               xUnit: preset rendering, preamp/boost, atomic write/backup, settings

docs/
  DEVELOPMENT.md                      this file
  TERMS.md, THIRD-PARTY-NOTICES.md    terms of use, bundled-component licenses
  planning/                           original PRD and build task list (historical)

publish/                              build output from dotnet publish (git-ignored)
```

Flow: tray click / hotkey → `App.ApplyMode` → `EqualizerApoService.CheckStatus` → `Apply` (backup once, temp write, replace)
→ save `LastMode` → update tray icon, popup and toast. On startup the last mode is reapplied.

Rendering is software-only (`RenderMode.SoftwareOnly`): GPU rendering cost ~100 MB of driver memory for two tiny windows.
