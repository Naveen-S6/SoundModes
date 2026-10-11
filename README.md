# SoundModes

Better sound from your laptop speakers, headphones or external speakers, in one click.
SoundModes sits in the corner of your taskbar and switches your laptop's sound between modes made for
**meetings, music and movies**.

## What it fixes

| If this happens to you | SoundModes helps by |
|---|---|
| Voices on calls sound boomy or muddy and are hard to understand | **Meeting** mode cuts the boom and brings voices forward |
| Music sounds flat | **Music** mode adds warmth and sparkle |
| Movies lack punch, but you can't hear the dialogue | **Movie** mode adds depth while keeping speech clear |
| Changing sound settings means digging through Windows menus | One click from the taskbar, or a keyboard shortcut |
| It's still too quiet even at full volume | **Boost** makes everything louder than Windows' 100% |
| You want your own sound | **Advanced** lets you adjust bass, voice and treble yourself |

## How it works on your laptop

- SoundModes adds a small round icon near the clock. The icon's letter and color show the current mode.
- It changes the sound of **everything** your laptop plays: Zoom, Teams, YouTube, Spotify, games, all of it.
- It works with whatever you listen on: laptop speakers, headphones, or speakers plugged into the headphone jack.
  It doesn't control the speakers themselves (their own volume knob or remote still works as normal).
- The sound shaping is done by **Equalizer APO**, a free and widely used Windows sound tool. SoundModes is the
  easy remote control for it. You install Equalizer APO once, then never need to open it again.
- It remembers your last mode, and can start automatically when Windows starts.
- It's light (about 25 MB of memory, no CPU use when idle), works fully offline, and collects no data.

## Install

You need Windows 10 or 11 (64-bit). It takes about 5 minutes and one restart.

**1. Install Equalizer APO (free, one time)**

1. Download it from [sourceforge.net/projects/equalizerapo](https://sourceforge.net/projects/equalizerapo/) and run the installer.
2. At the end it opens the **Configurator** and lists your sound devices. Tick the device you listen on
   (for example **Speakers** or **Headphones**). If you use more than one, tick them all.
3. Click OK and **restart your laptop** when it asks.

**2. Set up SoundModes**

1. Download `SoundModes.exe` from this repository's **Releases** page (on the right side of the GitHub page).
   Put it somewhere permanent, for example a `SoundModes` folder in Documents. Nothing else needs installing.
2. Double-click it. If Windows shows "Windows protected your PC", click **More info**, then **Run anyway**.
   This appears because the app isn't from the Microsoft Store.
3. The SoundModes icon appears near the clock. If you don't see it, click the **^** arrow and drag the icon
   onto the taskbar so it stays visible.
4. Right-click the icon and tick **Start with Windows** so it's always ready.

## Using it

**Pick a mode:** click the icon and choose **Meeting**, **Music**, **Movie** or **Flat**.
You can also right-click the icon, or use a keyboard shortcut from any app:

| Mode | When to use it | Shortcut |
|---|---|---|
| Meeting | Calls and videos where people are talking | Ctrl + Alt + 1 |
| Music | Songs, playlists, podcasts with music | Ctrl + Alt + 2 |
| Movie | Films, shows, games | Ctrl + Alt + 3 |
| Flat | Your laptop's normal sound, unchanged | Ctrl + Alt + 0 |

A small "Meeting mode ON" message confirms the switch when you use a shortcut.

**Volume and Boost:** the **Volume** slider is the same as the Windows volume. **Boost** goes beyond it,
for when full volume still isn't loud enough. If loud parts start to crackle, turn Boost down a little.

**Make your own sound:** click **Advanced ▸** to see the current mode's settings:
**Bass** (thump and warmth), **Low-mid** (boominess), **Voice** (speech clarity), **Treble** (crispness) and
**Cut rumble**. Hover the ⓘ next to each for a plain explanation. Move any slider and SoundModes saves it as your own
**Custom** mode, which gets its own button. The built-in modes always stay as they are, so you can go back anytime.

**Tip for calls:** if voices still sound unclear in Meeting mode, try **Voice** a little higher and **Low-mid** a little lower.

## If something isn't right

| What you see | What to do |
|---|---|
| Red **!** icon, "Equalizer APO not found" | Install Equalizer APO (step 1 above), restart, then click **Check again** |
| "Can't write Equalizer APO's config" | Click **Grant permission** and choose **Yes** on the Windows prompt (needed once) |
| Modes switch but the sound doesn't change | Open **Equalizer APO Configurator** from the Start menu, tick the device you're listening on, restart |
| Sound is odd or processed twice | Turn off other sound "enhancements" (Realtek, Dolby, Waves) in their apps or in Windows sound settings |
| A shortcut doesn't work | Another app is using that key combination. Use the icon instead |
| Sound crackles | Lower **Boost**, or lower Bass in Advanced |

## Uninstall

1. Click the icon, choose **Flat**, then right-click → untick **Start with Windows** → **Exit**.
2. Delete `SoundModes.exe` (and, optionally, the `%APPDATA%\SoundModes` folder with your settings).
3. Optional: uninstall Equalizer APO from Windows **Settings → Apps** if you no longer want it.

## Privacy

Everything stays on your laptop. No account, no internet connection, no tracking.
The only web link is the **Download** button that opens the Equalizer APO site.

## Contributing

Ideas, bug reports and improvements are welcome. Open an **Issue** to report a bug or suggest something,
or fork the repo and raise a **Pull Request**. See [CONTRIBUTING.md](CONTRIBUTING.md) for how.

## License and terms

SoundModes is free and open source under the [MIT License](LICENSE). Using it means you accept the
[Terms of Use](docs/TERMS.md): it comes as is, with no warranty, and you're responsible for safe listening levels.
Bundled components are listed in [THIRD-PARTY-NOTICES.md](docs/THIRD-PARTY-NOTICES.md).
SoundModes is not affiliated with Equalizer APO or any audio brand.

---

Built with C# / .NET 8 (WPF), using Equalizer APO for the sound processing and NAudio for volume control.
Building, testing and technical details: [DEVELOPMENT.md](docs/DEVELOPMENT.md).
