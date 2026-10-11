# PRD: SoundModes, a Windows Tray App for Sound Modes

| Field | Value |
|---|---|
| Owner | Naveen S |
| Status | Draft v1 |
| Date | 29 Sep 2026 |
| Build approach | Claude Code |
| Platform | Windows 10 / 11 (x64) |

---

## 1. Problem Statement

I use an 8 year old Panasonic home theatre as the speaker for my laptop on my desk, connected over a 3.5 mm AUX cable. The home theatre has no smart connection to the laptop, so:

| Pain | Impact |
|---|---|
| Voices in meetings sound boomy, muddy or "busy" | I struggle to understand people on calls |
| No easy way to change the sound character for different uses | Music and movies sound flat or wrong with the same settings used for meetings |
| Volume control is split between the laptop and the home theatre remote | Constant back and forth, no quick control from the desk |
| Existing workaround (Equalizer APO + scripts + hotkeys) works but feels like a hack | No visible current mode, no UI, hard to tweak |

**Goal:** A simple, lightweight Windows app that lives in the taskbar system tray and lets me switch between sound modes (Meeting, Music, Movie, Flat) and control volume in one or two clicks.

---

## 2. Goals and Non Goals

| Goals (v1) | Non Goals (v1) |
|---|---|
| One click mode switching from the tray | Writing a custom audio driver or DSP engine |
| Master volume control from the tray | Controlling the Panasonic home theatre itself (no IR, no HDMI CEC) |
| Global hotkeys for each mode | Mac or Linux support |
| Show the active mode at a glance | Cloud sync, accounts, telemetry |
| Remember last mode and restore it on startup | Mobile companion app |
| Tiny footprint: under 50 MB RAM, near 0% CPU when idle | Store listing or auto updates |

---

## 3. Key Assumptions

| # | Assumption | Why it matters |
|---|---|---|
| A1 | **Equalizer APO** is installed and attached to the laptop output that feeds the AUX cable | The app does not process audio. It writes Equalizer APO config files, and Equalizer APO applies them live |
| A2 | Equalizer APO reloads `config.txt` automatically when the file changes | Mode switches take effect instantly with no restart |
| A3 | The config path can be read from registry `HKLM\SOFTWARE\EqualizerAPO\ConfigPath`, with a fallback of `C:\Program Files\EqualizerAPO\config` | Needed to locate the file to write |
| A4 | The user account needs write access to `config.txt` (granted once, with admin rights, during first run) | Normal switching should not need admin |
| A5 | Single user, single laptop, personal use | Keeps scope small |

---

## 4. User Stories

| ID | As a user, I want to... | So that... | Priority |
|---|---|---|---|
| US1 | Click the tray icon and pick Meeting / Music / Movie / Flat | I can change sound in 1 to 2 clicks | P0 |
| US2 | See which mode is active (icon badge, tooltip, checkmark) | I don't wonder why something sounds different | P0 |
| US3 | Adjust master volume with a slider in the tray popup | I don't reach for the Windows volume flyout | P0 |
| US4 | Use global hotkeys (Ctrl+Alt+1/2/3/0) | I can switch modes during a call without leaving the window | P0 |
| US5 | Have the app start with Windows and restore my last mode | It just works after a reboot | P0 |
| US6 | Get a clear message if Equalizer APO is missing or not writable | I know how to fix setup issues | P0 |
| US7 | Tweak a mode's bass / voice / treble with simple sliders | I can tune it to my room and my Panasonic | P1 |
| US8 | Mute / unmute with one click or hotkey | Quick silence when someone walks in | P1 |
| US9 | Auto switch to Meeting when Zoom, Teams or Google Meet (Chrome) is using audio, and back to the previous mode after | I don't have to remember | P2 |
| US10 | Adjust per app volume (Zoom vs Spotify) from the popup | I balance call audio against background music | P2 |

---

## 5. Functional Requirements

### 5.1 Tray icon and menu (P0)

| Req | Detail |
|---|---|
| FR1 | App runs with no main window; lives in the system tray |
| FR2 | **Left click** opens a small popup panel near the tray (see 6. UX) |
| FR3 | **Right click** opens a context menu: Meeting, Music, Movie, Flat (radio checked), separator, Edit modes..., Start with Windows (toggle), Open Equalizer APO folder, About, Exit |
| FR4 | Tray icon changes per mode (different glyph or colored badge). Tooltip reads "SoundModes: Meeting" |
| FR5 | Only one instance can run (single instance mutex). A second launch focuses the existing one |

### 5.2 Mode switching (P0)

| Req | Detail |
|---|---|
| FR6 | Switching a mode writes the mode's filter lines into Equalizer APO `config.txt` (atomic write: temp file then replace) |
| FR7 | Switch completes in under 300 ms and shows a short toast or on screen indicator ("Meeting mode ON") for about 1 second |
| FR8 | Last active mode is saved and reapplied on app start |
| FR9 | On first run, back up the existing `config.txt` to `config.txt.soundmodes-backup` (only if no backup exists) |
| FR10 | "Flat" writes only `Preamp: 0 dB` (effects off) |

### 5.3 Default presets (P0)

Equalizer APO syntax. Preamp equals minus the largest boost, to avoid clipping.

| Mode | Preamp | Filters |
|---|---|---|
| Meeting | -4 dB | `HP Fc 100 Hz Q 0.71`; `PK Fc 300 Hz Gain -4 dB Q 1.0`; `PK Fc 3000 Hz Gain 4 dB Q 1.0` |
| Music | -3 dB | `LSC Fc 105 Hz Gain 3 dB`; `HSC Fc 8000 Hz Gain 2 dB` |
| Movie | -5 dB | `LSC Fc 100 Hz Gain 4 dB`; `PK Fc 300 Hz Gain -1 dB Q 1.0`; `PK Fc 2500 Hz Gain 2 dB Q 1.0`; `HSC Fc 9000 Hz Gain 1 dB` |
| Flat | 0 dB | none |

Example generated `config.txt` for Meeting:

```
# Managed by SoundModes. Manual edits will be overwritten.
Preamp: -4 dB
Filter 1: ON HP Fc 100 Hz Q 0.71
Filter 2: ON PK Fc 300 Hz Gain -4 dB Q 1.0
Filter 3: ON PK Fc 3000 Hz Gain 4 dB Q 1.0
```

### 5.4 Volume (P0 / P1)

| Req | Detail | Priority |
|---|---|---|
| FR11 | Master volume slider (0 to 100) for the default playback device, using Windows Core Audio | P0 |
| FR12 | Slider stays in sync if volume changes elsewhere (keyboard keys, Windows flyout) | P0 |
| FR13 | Mute toggle button plus hotkey Ctrl+Alt+M | P1 |
| FR14 | Scroll the mouse wheel over the tray icon to change volume in steps of 2 | P1 |
| FR15 | Per app volume list (apps currently playing audio) | P2 |

### 5.5 Hotkeys (P0)

| Hotkey | Action |
|---|---|
| Ctrl+Alt+1 | Meeting |
| Ctrl+Alt+2 | Music |
| Ctrl+Alt+3 | Movie |
| Ctrl+Alt+0 | Flat |
| Ctrl+Alt+M | Mute toggle (P1) |

Requirements: registered globally; if a hotkey is already taken, show a warning and let the user rebind it in Settings (P1).

### 5.6 Mode editor (P1)

| Req | Detail |
|---|---|
| FR16 | Simple editor with 3 sliders per mode: **Bass** (low shelf ~100 Hz), **Voice** (peak ~3 kHz), **Treble** (high shelf ~8 kHz), range -12 to +12 dB, plus a "Reduce boom" toggle (high pass at 100 Hz + cut at 300 Hz) |
| FR17 | Changes preview live if the edited mode is active |
| FR18 | "Reset to default" per mode |
| FR19 | Preamp auto calculated as minus the highest positive gain |
| FR20 | Advanced (optional): raw text box to paste Equalizer APO lines |

### 5.7 Auto Meeting mode (P2)

| Req | Detail |
|---|---|
| FR21 | Detect active audio sessions from `Zoom.exe`, `ms-teams.exe` / `Teams.exe`, or a configurable process list |
| FR22 | Switch to Meeting when detected, and restore the previous mode 10 seconds after the session ends |
| FR23 | Toggle on/off in the menu, default off |

### 5.8 Setup and error handling (P0)

| Scenario | Behaviour |
|---|---|
| Equalizer APO not installed | Popup shows "Equalizer APO not found" with a Download button (opens the official site) and a "Check again" button. Modes are disabled |
| `config.txt` not writable | Show "Grant permission" button that relaunches a small elevated helper (UAC) to run `icacls <config.txt> /grant *S-1-5-32-545:(M)` once |
| Equalizer APO not attached to the current output device | Show hint: "Open Equalizer APO Configurator and tick your Speakers/Headphones device, then restart" |
| Output device changes (e.g. headphones plugged in) | Volume slider rebinds to the new default device |

---

## 6. UX

### Tray popup (left click), approx 320 x 220 px, follows Windows light/dark theme

```
+--------------------------------------+
|  SoundModes                     [x]  |
|                                      |
|  [ Meeting ] [ Music ] [ Movie ] [Flat]  <- segmented buttons, active one highlighted
|                                      |
|  Volume  (spk) ======o-------  62  [M]|  <- slider + mute
|                                      |
|  Auto Meeting mode   [ off ]         |  (P2)
|  Edit modes...            Settings   |
+--------------------------------------+
```

| UX rule | Detail |
|---|---|
| Opens | Anchored above the tray, closes on focus loss or Esc |
| Speed | Popup opens in under 200 ms |
| Accessibility | Keyboard navigable, visible focus, tooltips on buttons |
| Visual | Windows 11 look (rounded corners, Mica/acrylic if simple), respects dark mode |
| Feedback | Brief toast on hotkey switch, since the popup is not visible then |

---

## 7. Non Functional Requirements

| Area | Requirement |
|---|---|
| Performance | Idle RAM under 50 MB, idle CPU about 0%, mode switch under 300 ms |
| Reliability | Never leave `config.txt` half written (atomic write). App crash must not break audio |
| Privacy | Fully offline. No telemetry, no network calls except the optional "Download Equalizer APO" link |
| Startup | Start with Windows via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` (no admin) |
| Storage | Settings and presets in `%APPDATA%\SoundModes\settings.json` |
| Packaging | Single portable `.exe` (self contained) and optionally a simple installer |
| Permissions | Runs as normal user. Admin only once, for the permission fix |

---

## 8. Suggested Technical Approach (for Claude Code)

| Decision | Recommendation | Reason |
|---|---|---|
| Language / framework | **C# .NET 8, WPF** (or WinForms for the absolute simplest) | Native tray support, small, easy single exe publish |
| Tray icon | `H.NotifyIcon.Wpf` or WinForms `NotifyIcon` | Mature, supports popups and context menus |
| Volume control | **NAudio** (`MMDeviceEnumerator`, `AudioEndpointVolume`, `AudioSessionManager` for per app) | Standard Core Audio wrapper |
| Global hotkeys | Win32 `RegisterHotKey` via P/Invoke | No extra dependency |
| Toasts | Small custom borderless topmost window that fades out (simpler than Windows toast registration) | No AppID setup needed |
| Settings | `System.Text.Json` to `%APPDATA%` | Built in |
| Publish | `dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true` | One file to run |

Alternative if preferred: Python + `pystray` + `pycaw` + `keyboard`, packaged with PyInstaller. Faster to prototype, heavier exe and less native look.

### Suggested project structure

```
SoundModes/
  App.xaml / App.xaml.cs          # startup, single instance, tray
  Tray/TrayPopup.xaml             # left click panel
  Services/EqualizerApoService.cs # locate config, write presets, backup, permission check
  Services/VolumeService.cs       # NAudio master volume + mute + device change events
  Services/HotkeyService.cs       # RegisterHotKey
  Services/AutoMeetingService.cs  # P2 audio session watcher
  Models/SoundMode.cs             # name, preamp, filters, bass/voice/treble
  Models/Settings.cs
  Assets/icons/*.ico              # one icon per mode
```

---

## 9. Milestones

| Milestone | Scope | Done when |
|---|---|---|
| M1: Core | Tray icon, right click menu, 4 modes writing to Equalizer APO, last mode restore, setup checks | I can switch modes from the tray and hear the change |
| M2: Daily use | Left click popup, volume slider, hotkeys, toast, start with Windows | I use it through a full work day without touching the Windows flyout |
| M3: Tuning | Mode editor sliders, mute hotkey, scroll to change volume | I have tuned Meeting mode to my room |
| M4: Smart | Auto Meeting mode, per app volume | Joining a Zoom call switches mode automatically |

---

## 10. Acceptance Criteria (v1 = M1 + M2)

| # | Test | Expected |
|---|---|---|
| AC1 | Click Meeting in the tray | `config.txt` contains Meeting filters within 300 ms; voices audibly less boomy |
| AC2 | Press Ctrl+Alt+3 while another app has focus | Movie mode applies, toast shows "Movie mode ON" |
| AC3 | Reboot the laptop | App auto starts, last mode reapplied, icon shows it |
| AC4 | Change volume with keyboard keys | Popup slider reflects the new value |
| AC5 | Uninstall Equalizer APO and launch the app | Clear "not found" message, no crash |
| AC6 | Remove write permission on `config.txt` | "Grant permission" flow fixes it with one UAC prompt |
| AC7 | Leave running for 8 hours | RAM stays under 50 MB, no leaks, no audio glitches |

---

## 11. Risks and Open Questions

| Item | Type | Notes / Mitigation |
|---|---|---|
| Equalizer APO attached to the wrong device | Risk | Detect via its registry/device list if feasible; otherwise clear hint in UI |
| Conflicts with Realtek / Dolby / Waves enhancements | Risk | Show a one time tip to disable them |
| Hotkey clashes with other apps | Risk | Rebindable hotkeys (P1) |
| Should Loudness Equalization (Windows enhancement) be toggled by the app? | Open | Not officially exposed via API; keep as a manual tip in v1 |
| Add a compressor / leveler for quiet meeting voices? | Open | Would need a VST plugin in Equalizer APO; consider for v2 |
| WPF vs WinForms | Open | Default to WPF for a nicer popup; WinForms if speed of build matters more |

---

## 12. Reference: Existing Workaround

The current script kit (`SoundModes.zip`: Setup.bat, Switch-Mode.ps1, presets/*.txt) proves the core mechanism: copying a preset into Equalizer APO `config.txt` changes the sound instantly. The app replaces this with a proper tray UI, volume control and settings.
