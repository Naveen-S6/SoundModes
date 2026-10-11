namespace SoundModes.Models;

/// <summary>
/// A named sound mode: its EQ settings and, for presets, a global hotkey digit (Ctrl+Alt+digit).
/// The Custom mode is built at runtime from the user's saved settings and has no hotkey.
/// </summary>
public sealed record SoundMode(string Name, EqSettings Eq, char? HotkeyDigit = null)
{
    public const string CustomName = "Custom";

    // PRD 5.3 presets, expressed in slider terms (Bass 100 Hz, Low-mid 300 Hz, Voice 3 kHz, Treble 8 kHz).
    public static readonly SoundMode Meeting = new("Meeting", new(LowMid: -4, Voice: 4, CutRumble: true), '1');
    public static readonly SoundMode Music = new("Music", new(Bass: 3, Treble: 2), '2');
    public static readonly SoundMode Movie = new("Movie", new(Bass: 4, LowMid: -1, Voice: 2, Treble: 1), '3');
    public static readonly SoundMode Flat = new("Flat", EqSettings.Flat, '0');

    /// <summary>Built-in presets in menu order.</summary>
    public static readonly IReadOnlyList<SoundMode> Presets = [Meeting, Music, Movie, Flat];

    /// <summary>The user's Custom mode with the given settings.</summary>
    public static SoundMode Custom(EqSettings eq) => new(CustomName, eq);

    /// <summary>Finds a preset by name (case-insensitive). Returns null for Custom, unknown or null names.</summary>
    public static SoundMode? FindPreset(string? name) =>
        Presets.FirstOrDefault(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));

    public bool IsCustom => Name == CustomName;

    /// <summary>Human readable hotkey, e.g. "Ctrl+Alt+1"; null for Custom.</summary>
    public string? HotkeyText => HotkeyDigit is char d ? $"Ctrl+Alt+{d}" : null;

    /// <summary>Renders the full Equalizer APO config.txt content for this mode, plus the global Boost (dB).</summary>
    public string ToConfigText(double boostDb = 0) => Eq.ToConfigText(boostDb);
}
