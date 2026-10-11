# Changelog

## 2026-10-11: Repository reorganized

### Changed
- App moved to `src/SoundModes/`, tests to `tests/SoundModes.Tests/`, and docs to `docs/` (DEVELOPMENT, TERMS, THIRD-PARTY-NOTICES), with the PRD and task list in `docs/planning/`. README, LICENSE, CHANGELOG and CONTRIBUTING stay at the top level, where GitHub detects them.
- Solution, project reference, document links and build commands updated. Build commands now use `src/SoundModes`, e.g. `dotnet run --project src/SoundModes`.

## 2026-10-11: Open-source release prep

### Added
- `LICENSE` (MIT, © 2026 Naveen S), `TERMS.md` (terms of use: as is, safe listening, what it changes, privacy, liability), `CONTRIBUTING.md` (issues, fork → branch → PR workflow, checks, code guidelines), `.github/pull_request_template.md`, and `THIRD-PARTY-NOTICES.md` (bundled .NET runtime and NAudio, both MIT; Equalizer APO is not bundled).
- README sections for Contributing and for License and terms. Copyright embedded in the exe's properties; the About box mentions the license.

## 2026-10-11: v1.3.1 (quieter tooltips, user-facing README)

### Changed
- Sliders (Bass, Low-mid, Voice, Treble, Boost) no longer show a tooltip; it popped up while dragging. The explanation now appears only when hovering the label or its ⓘ. Screen readers still get it as help text.
- README rewritten for users: what it fixes, how it works on the laptop, install, everyday use, troubleshooting, uninstall.
  Build, test, architecture and file details moved to the new DEVELOPMENT.md.

## 2026-10-11: v1.3.0 (Boost)

### Added
- **Boost** slider under Volume (0 to +12 dB). Makes everything louder beyond Windows' 100% by adding to the Equalizer APO preamp. Global (applies to every mode), live, saved as `BoostDb` in `settings.json`, and clamped on load.
- An ⓘ tooltip that explains in plain words that high Boost can make loud passages crackle.

### Why
- The modes lower the preamp to avoid clipping (Music −3 dB, Movie −4 dB), so they sound quieter than Flat. Boost lets you win that back, or push quiet calls louder.
- There's no limiter: Equalizer APO has none built in, and adding one would need a VST plugin. Clipping is left to the user's judgement, and the tooltip warns about it.

## 2026-10-11: v1.2.1 (plain-language help in Advanced)

### Added
- ⓘ next to Bass, Low-mid, Voice, Treble and Cut rumble. Hovering the label or icon shows, in plain words, what it changes and when to turn it up or down, with the technical frequency on a last line. The same text shows when a slider gets keyboard focus, and is exposed to screen readers as help text.
- Tooltips now wrap at 280 px, appear after 150 ms and stay up for 30 s, so there's time to read them.

## 2026-10-11: v1.2.0 (Advanced shows real values; edits become Custom)

### Changed
- Advanced now shows the **actual** settings of the selected mode (e.g. Music: Bass +3, Treble +2) instead of zero offsets.
- Changing anything in Advanced switches to a saved **Custom** mode, which starts from the current mode's values. A Custom button appears in the popup, and a Custom item in the right-click menu, once one exists. Presets themselves are never modified, so "Reset to default" was removed (just click a preset).
- Every mode is now described by the same controls: Bass (100 Hz shelf), new **Low-mid** (300 Hz peak), Voice (3 kHz peak), Treble (8 kHz shelf), Cut rumble (100 Hz high-pass), and custom lines. "Reduce boom" is now Cut rumble plus the Low-mid slider.
- Preset tweaks so they fit the controls: Music bass shelf 105 → 100 Hz; Movie voice 2.5 → 3 kHz, treble 9 → 8 kHz, preamp −5 → −4 dB (PRD FR19 rule). Meeting is unchanged and still matches the PRD example exactly.
- `settings.json`: per-mode `Tweaks` replaced by a single `Custom` entry. Old `Tweaks` entries are ignored.

### Why
- Showing zeros for a preset that clearly boosts bass was confusing. Editing a copy as "Custom" keeps the presets predictable.

## 2026-10-11: v1.1.0 (Advanced tuning in the popup)

### Added
- Collapsed **Advanced ▸** section in the popup. The four quick mode buttons are unchanged; the section only opens when clicked.
  For the active mode (not Flat) it offers Bass (low shelf 100 Hz), Voice (peak 3 kHz) and Treble (high shelf 8 kHz) sliders from −12 to +12 dB,
  a "Reduce boom" toggle, a box for custom Equalizer APO lines, and Reset to default.
- Tweaks are layered on top of the preset (all zero = preset exactly), applied live (150 ms debounce), and saved per mode in `settings.json` (`Tweaks`).
- Preamp drops by the largest tweak boost, so there's no clipping (FR19). Values read from disk are clamped to ±12 dB, and custom lines are capped at 4,000 characters.
- 10 new tests (26 total).

### Decisions (why)
- Tweaks add filters on top of the presets instead of replacing them, so the tuned PRD presets stay intact and "Reset" is just "no tweaks".
- "Reduce boom" is shown as already on, and greyed out, for Meeting, which has the cut built in, so the filters aren't doubled.
- Custom lines don't adjust the preamp automatically. The tooltip tells you to lower it yourself if you boost.

## 2026-10-11: v1.0.0 (M1 + M2)

### Added
- Tray app: 4 modes (Meeting, Music, Movie, Flat) written to Equalizer APO `config.txt` with an atomic replace, the PRD 5.3 presets, and a one-time backup of the original config.
- Status checks for Equalizer APO not installed or config not writable. Includes a one-click elevated permission fix (`--grant-permission`).
- Right-click menu, a per-mode tray icon and tooltip, and a left-click popup with mode buttons, a volume slider and setup messages. The popup follows Windows light/dark mode.
- Master volume via NAudio Core Audio. Stays in sync with external volume changes and rebinds when the default device changes.
- Global hotkeys Ctrl+Alt+1/2/3/0, with a warning when a hotkey is already taken.
- Click-through toast ("Meeting mode ON") shown when the popup is closed.
- Start with Windows (HKCU Run), last mode restored on startup, single instance (a second launch opens the popup).
- File log, global error handler with a friendly message.
- xUnit tests (16) for preset rendering, preamp headroom, atomic write/backup and settings persistence.

### Decisions (why)
- The permission grant covers the config **folder**, not just `config.txt`. The atomic temp-file write and the backup need to create files there. Equalizer APO's own installer already grants Users full control, so the fix is a fallback.
- WinForms `NotifyIcon` instead of the `H.NotifyIcon` package: one less dependency.
- Tray icons are drawn at runtime instead of shipping `.ico` files.
- Software rendering, and no drop shadow on the popup: GPU rendering used ~120 MB of private memory, versus ~26 MB now (Task Manager).
- Uncompressed single-file publish: compression shrinks the exe but costs ~70 MB of RAM.
- "Edit modes..." is left out of the menu until the editor exists (M3).
