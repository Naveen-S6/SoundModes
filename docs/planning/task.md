# SoundModes: Build Tasks

Source: [SoundModes_PRD.md](SoundModes_PRD.md). Stack: C# .NET 8 WPF, NAudio, Win32 `RegisterHotKey`, `System.Text.Json`.
Status legend: `[ ]` todo, `[x]` done, `[~]` in progress, `[?]` blocked on a decision, `[U]` needs Naveen to verify by hand.
**This pass: v1 = M1 + M2. Status: built, 16 tests passing, manual checks below.**

## 0. Environment

- [x] .NET 8 SDK 8.0.425 installed per-user (`%LOCALAPPDATA%\Microsoft\dotnet`, added to user PATH + DOTNET_ROOT)
- [x] Equalizer APO installed (config dir `C:\Program Files\EqualizerAPO\config`; its installer grants Users full control)
- [x] `git init`, `.gitignore`, repo-local `nuget.config` (global NuGet config had no sources)
- [x] Solution: `SoundModes` (WPF app) + `SoundModes.Tests` (xUnit)

## M1: Core (tray, modes, Equalizer APO)

- [x] `Models/SoundMode.cs`: name, preamp, filter lines, hotkey digit (bass/voice/treble fields deferred to M3 editor)
- [x] Default presets exactly per PRD 5.3
- [x] `Models/Settings.cs`: `%APPDATA%\SoundModes\settings.json`, corrupt file -> defaults + `.bad` copy
- [x] `Services/EqualizerApoService.cs`: registry/fallback locate, render, atomic write (temp + `File.Move` overwrite, 3 retries), backup once, status check
- [x] Grant-permission flow: elevated relaunch with `--grant-permission`, grants Users Modify on the config **folder** (needed for temp file + backup; PRD said file only)
- [x] Single instance (named mutex); second launch opens the popup
- [x] Tray icon (WinForms `NotifyIcon`) + right-click menu. "Edit modes..." omitted until M3
- [x] Per-mode tray icon (drawn at runtime, no `.ico` files) + tooltip "SoundModes: Meeting"
- [x] Restore last mode on startup
- [x] Global unhandled-exception handler + `%APPDATA%\SoundModes\log.txt`
- [x] Tests: rendering, PRD example, invariant culture, preamp headroom, atomic write, backup-once, settings round-trip/corrupt
- [U] **Done when:** switching modes audibly changes sound

## M2: Daily use

- [x] `Tray/TrayPopup.xaml`: anchored bottom-right, closes on focus loss/Esc/✕, light/dark, rounded corners, 44 px mode buttons
- [x] Setup-error states: not found (Download + Check again), Grant permission, device hint line
- [x] `Services/VolumeService.cs`: get/set, external change sync, rebind on default device change
- [x] Volume slider synced both ways
- [x] `Services/HotkeyService.cs`: Ctrl+Alt+1/2/3/0, balloon warning if taken
- [x] Toast: click-through, no focus steal, fades after ~1 s (only when popup closed)
- [x] Start with Windows (HKCU Run), path refreshed on each start
- [x] Publish: single-file self-contained, uncompressed (`publish\SoundModes.exe`, 155 MB)
- [x] README, CHANGELOG

### Acceptance criteria (PRD 10)

| # | Result |
|---|---|
| AC1 | [x] config.txt written in 29 to 86 ms. [U] confirm voices sound less boomy |
| AC2 | [x] Simulated Ctrl+Alt+3 / Ctrl+Alt+1: mode applied, "Meeting mode ON" toast seen |
| AC3 | [U] Turn on Start with Windows, reboot, check icon + mode |
| AC4 | [U] Press keyboard volume keys with popup open; slider should follow |
| AC5 | [x] Ran before Equalizer APO was installed: status NotInstalled, no crash |
| AC6 | [U] Not exercised: Equalizer APO's installer already makes the folder writable. Optional: remove Users write, click Grant permission |
| AC7 | [U] 8 h soak. Measured so far: ~26 MB private working set, 0% idle CPU, stable over minutes |

## M3: Tuning (P1), in progress

- [x] Mode editor as a collapsed "Advanced" section in the popup showing the active mode's real values: Bass (LSC 100 Hz), Low-mid (PK 300 Hz), Voice (PK 3 kHz), Treble (HSC 8 kHz), -12..+12 dB, Cut rumble (HP 100 Hz)
- [x] Editing switches to a saved Custom mode (popup button + tray item appear once it exists); presets stay fixed
- [x] Auto preamp = -(max positive gain) for every mode; live preview
- [x] Raw-text box for custom Equalizer APO lines
- [U] Tune Meeting mode to the room (Naveen)
- [ ] Mute button + Ctrl+Alt+M
- [ ] Mouse wheel over tray icon changes volume by 2 (needs a hook; NotifyIcon has no wheel event)
- [ ] Rebindable hotkeys in Settings
- [x] Tests: preset rendering, preamp calc, filter order, clamping, raw lines, Custom resolve, settings round-trip

## M4: Smart (P2)

- [ ] Auto Meeting mode: watch audio sessions for Zoom.exe, ms-teams.exe, Teams.exe only (+ configurable list; Chrome/Meet not auto-detected, decided); restore previous mode 10 s after end; default off
- [ ] Per-app volume list in popup
- [ ] Tests: auto-meeting state machine (detect -> switch -> end -> 10 s -> restore)

## Known limitations

- Popup anchors to the bottom-right of the primary screen's work area (wrong spot if the taskbar is on top/left).
- One unexplained native crash (access violation) seen once in an early Debug build with GPU rendering; not reproduced in 10 stress runs after switching to software rendering.

## Global standards not applied (desktop, offline, single user)

API/DB/health endpoint/rate limiting/CI deploy/backups/cookie consent/.env sections of the global CLAUDE.md do not apply (no secrets, no server).
i18n: all user-facing text lives in `Strings.cs`, so translation is possible later. No translation work in v1.
