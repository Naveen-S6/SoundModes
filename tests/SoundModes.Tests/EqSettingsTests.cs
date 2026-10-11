using SoundModes.Models;

namespace SoundModes.Tests;

public class EqSettingsTests
{
    private static string[] Lines(string text) => text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void Preamp_is_minus_the_largest_boost()
    {
        Assert.Equal(-5, new EqSettings(Bass: 2, LowMid: 5, Voice: -3).PreampDb);
        Assert.Equal(0, new EqSettings(Bass: -6, Treble: -2).PreampDb);
    }

    [Fact]
    public void Boost_is_added_to_the_preamp_only()
    {
        Assert.Equal(["Preamp: 3 dB", "Filter 1: ON LSC Fc 100 Hz Gain 3 dB", "Filter 2: ON HSC Fc 8000 Hz Gain 2 dB"],
            Lines(SoundMode.Music.ToConfigText(boostDb: 6))[1..]); // -3 + 6
        Assert.Contains("Preamp: 4 dB", SoundMode.Flat.ToConfigText(boostDb: 4));
    }

    [Fact]
    public void Boost_is_clamped_to_0_to_12_dB()
    {
        Assert.Equal((0.0, 12.0, 0.0, 5.0),
            (EqSettings.ClampBoost(-3), EqSettings.ClampBoost(40), EqSettings.ClampBoost(double.NaN), EqSettings.ClampBoost(5)));
        Assert.Equal(12, new Settings { BoostDb = 99 }.Boost);
    }

    [Fact]
    public void Filters_go_low_to_high_and_skip_zero_gains()
    {
        var lines = Lines(new EqSettings(Bass: 2, Treble: -3, CutRumble: true).ToConfigText());
        Assert.Equal(["Filter 1: ON HP Fc 100 Hz Q 0.71", "Filter 2: ON LSC Fc 100 Hz Gain 2 dB", "Filter 3: ON HSC Fc 8000 Hz Gain -3 dB"],
            lines[2..]);
    }

    [Fact]
    public void Clamped_limits_gains_and_raw_text_length()
    {
        var eq = new EqSettings(Bass: 40, LowMid: -40, Treble: double.NaN, ExtraLines: new string('x', 5000)).Clamped();
        Assert.Equal((12.0, -12.0, 0.0), (eq.Bass, eq.LowMid, eq.Treble));
        Assert.Equal(EqSettings.MaxExtraLinesLength, eq.ExtraLines.Length);
    }

    [Fact]
    public void Custom_lines_are_appended_trimmed_without_blank_lines()
    {
        var lines = Lines(new EqSettings(ExtraLines: "  Filter: ON PK Fc 1000 Hz Gain -2 dB Q 1.0 \r\n\r\nPreamp: -1 dB\n").ToConfigText());
        Assert.Equal(["Filter: ON PK Fc 1000 Hz Gain -2 dB Q 1.0", "Preamp: -1 dB"], lines[^2..]);
    }

    [Fact]
    public void Editing_a_preset_starts_from_its_values()
    {
        // The popup builds Custom from the preset's values plus the user's change.
        var custom = SoundMode.Custom(SoundMode.Music.Eq with { Voice = 3 });
        Assert.Equal((3.0, 3.0, 2.0), (custom.Eq.Bass, custom.Eq.Voice, custom.Eq.Treble));
        Assert.True(custom.IsCustom);
    }

    [Fact]
    public void Settings_Resolve_handles_presets_custom_and_missing_custom()
    {
        var settings = new Settings();
        Assert.Same(SoundMode.Music, settings.Resolve("Music"));
        Assert.Null(settings.Resolve("Custom"));
        Assert.Null(settings.Resolve("Karaoke"));

        settings.Custom = new EqSettings(Bass: 99);
        var custom = settings.Resolve("custom");
        Assert.NotNull(custom);
        Assert.Equal(12, custom.Eq.Bass); // clamped: settings.json may be hand-edited
    }
}
