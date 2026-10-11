namespace SoundModes;

/// <summary>All user-facing text in one place, so it can move to resources if translation is ever needed.</summary>
public static class Strings
{
    public const string AppName = "SoundModes";
    public const string Tooltip = "SoundModes: {0}";
    public const string TooltipNoMode = "SoundModes: no mode selected";
    public const string TooltipSetup = "SoundModes: setup needed";
    public const string ModeOn = "{0} mode ON";
    public const string SetupNeededToast = "SoundModes needs setup. Click the tray icon.";
    public const string SwitchFailed = "Couldn't switch mode. Click the tray icon for details.";

    public const string MenuStartWithWindows = "Start with Windows";
    public const string MenuOpenApoFolder = "Open Equalizer APO folder";
    public const string MenuAbout = "About";
    public const string MenuExit = "Exit";

    public const string ApoNotFound = "Equalizer APO not found. Install it, tick your speakers in its Configurator, restart, then check again.";
    public const string ApoNotWritable = "SoundModes can't write Equalizer APO's config yet. Grant permission once (needs admin).";
    public const string Download = "Download";
    public const string GrantPermission = "Grant permission";
    public const string CheckAgain = "Check again";
    public const string GrantFailed = "Permission wasn't granted. Try again, and accept the Windows prompt.";
    public const string DeviceHint = "No change in sound? Open Equalizer APO Configurator, tick your Speakers device, then restart.";
    public const string Advanced = "Advanced";
    public const string AdvancedClosed = "Advanced ▸";
    public const string AdvancedOpen = "Advanced ▾";
    public const string AdvancedPreset = "These are {0}'s settings. Change anything to make it your Custom mode.";
    public const string AdvancedCustom = "Your Custom mode. Changes apply live and are saved.";
    public const string AdvancedNoMode = "Change anything to create your Custom mode.";
    public const string CustomTip = "Your own settings, made in Advanced";
    public const string Bass = "Bass";
    public const string Voice = "Voice";
    public const string Treble = "Treble";
    public const string BassTip = "The deep thump and warmth: kick drums, bass guitar, movie explosions. Turn up for a fuller, punchier sound. Turn down if it booms or makes things rattle.\n(Technical: low shelf at 100 Hz)";
    public const string LowMid = "Low-mid";
    public const string LowMidTip = "The boxy, muddy zone where rooms and speakers boom. Turn down to make voices and music cleaner and clearer. Turn up for a warmer, thicker sound.\n(Technical: peak at 300 Hz)";
    public const string VoiceTip = "Where speech lives: how clearly you hear words and vocals. Turn up to understand people on calls. Turn down if voices sound harsh or shouty.\n(Technical: peak at 3 kHz)";
    public const string TrebleTip = "Sparkle and detail: cymbals, S and T sounds, rustling. Turn up for crisper sound. Turn down if it sounds sharp, hissy or tiring.\n(Technical: high shelf at 8 kHz)";
    public const string CutRumble = "Cut rumble (below 100 Hz)";
    public const string CutRumbleTip = "Removes the very deepest bass, the kind you feel more than hear. Turn on for calls or a small room to stop voices sounding boomy. Leave off for music and movies.\n(Technical: high-pass filter at 100 Hz)";
    public const string CustomLines = "Custom Equalizer APO lines";
    public const string CustomLinesTip = "Added to the end of config.txt, e.g.\nFilter: ON PK Fc 1000 Hz Gain -2 dB Q 1.0\nIf you boost here, lower the preamp yourself (e.g. Preamp: -3 dB).";
    public const string ApplyLines = "Apply";
    public const string Volume = "Volume";
    public const string Boost = "Boost";
    public const string BoostTip = "Makes everything louder, beyond Windows' 100%. Applies to every mode. Great for quiet calls, or to win back the volume the modes lose (they turn down slightly to avoid distortion). Loud music or explosions may crackle at high Boost: if they do, lower it.\n(Technical: adds to the Equalizer APO preamp)";
    public const string Close = "Close (Esc)";

    public const string HotkeyTaken = "These hotkeys are used by another app and won't work: {0}";
    public const string About = "SoundModes {0}\nSwitch Equalizer APO sound modes from the tray.\n\nHotkeys: Ctrl+Alt+1 Meeting, 2 Music, 3 Movie, 0 Flat.\n\nFree and open source under the MIT License. Provided as is, with no warranty. See LICENSE and TERMS.md.";
    public const string UnexpectedError = "Something went wrong in SoundModes. Your sound is not affected. Details were saved to the log file.";
}
