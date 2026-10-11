using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;

namespace SoundModes.Models;

/// <summary>
/// The full sound of a mode, in the same terms as the Advanced sliders: four gains in dB, an optional rumble
/// cut, and raw Equalizer APO lines. Presets and the Custom mode are both just an EqSettings.
/// </summary>
public sealed record EqSettings(double Bass = 0, double LowMid = 0, double Voice = 0, double Treble = 0,
    bool CutRumble = false, string ExtraLines = "")
{
    public const double MaxGainDb = 12;
    public const int MaxExtraLinesLength = 4000;
    public const double MaxBoostDb = 12;

    /// <summary>Clamps a Boost value to 0..+12 dB (NaN/infinity -> 0), for values read from disk or UI.</summary>
    public static double ClampBoost(double boostDb) => double.IsFinite(boostDb) ? Math.Clamp(boostDb, 0, MaxBoostDb) : 0;

    public static readonly EqSettings Flat = new();

    /// <summary>Returns a copy with gains clamped to ±12 dB and raw lines capped, for values read from disk or UI.</summary>
    public EqSettings Clamped() => this with
    {
        Bass = Clamp(Bass),
        LowMid = Clamp(LowMid),
        Voice = Clamp(Voice),
        Treble = Clamp(Treble),
        ExtraLines = (ExtraLines ?? "").Length > MaxExtraLinesLength ? ExtraLines![..MaxExtraLinesLength] : ExtraLines ?? "",
    };

    /// <summary>Preamp in dB: minus the largest boost, so nothing clips (PRD FR19). 0 when nothing is boosted.</summary>
    [JsonIgnore]
    public double PreampDb => 0 - Math.Max(0, Math.Max(Math.Max(Bass, LowMid), Math.Max(Voice, Treble)));

    /// <summary>Equalizer APO filter definitions (without the "Filter N: ON" prefix), low to high frequency.</summary>
    public IEnumerable<string> Filters()
    {
        if (CutRumble) yield return "HP Fc 100 Hz Q 0.71";
        if (Bass != 0) yield return Invariant($"LSC Fc 100 Hz Gain {Bass} dB");
        if (LowMid != 0) yield return Invariant($"PK Fc 300 Hz Gain {LowMid} dB Q 1.0");
        if (Voice != 0) yield return Invariant($"PK Fc 3000 Hz Gain {Voice} dB Q 1.0");
        if (Treble != 0) yield return Invariant($"HSC Fc 8000 Hz Gain {Treble} dB");
    }

    /// <summary>Non-empty raw lines, trimmed, appended verbatim to config.txt.</summary>
    public IEnumerable<string> RawLines() =>
        (ExtraLines ?? "").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0);

    /// <summary>
    /// Renders the full Equalizer APO config.txt content. <paramref name="boostDb"/> (the global Boost) is added to
    /// the preamp; positive totals can clip loud passages.
    /// </summary>
    public string ToConfigText(double boostDb = 0)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Managed by SoundModes. Manual edits will be overwritten.");
        sb.AppendLine(Invariant($"Preamp: {PreampDb + boostDb} dB"));
        int n = 1;
        foreach (var filter in Filters())
            sb.AppendLine($"Filter {n++}: ON {filter}");
        foreach (var line in RawLines())
            sb.AppendLine(line);
        return sb.ToString();
    }

    private static double Clamp(double gain) => double.IsFinite(gain) ? Math.Clamp(gain, -MaxGainDb, MaxGainDb) : 0;

    private static string Invariant(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
