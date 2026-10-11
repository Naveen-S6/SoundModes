using System.Globalization;
using SoundModes.Models;

namespace SoundModes.Tests;

public class SoundModeTests
{
    private static string[] Lines(string text) => text.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

    [Fact]
    public void Meeting_renders_exactly_the_PRD_example()
    {
        const string expected = """
            # Managed by SoundModes. Manual edits will be overwritten.
            Preamp: -4 dB
            Filter 1: ON HP Fc 100 Hz Q 0.71
            Filter 2: ON PK Fc 300 Hz Gain -4 dB Q 1.0
            Filter 3: ON PK Fc 3000 Hz Gain 4 dB Q 1.0

            """;
        Assert.Equal(expected.ReplaceLineEndings(), SoundMode.Meeting.ToConfigText());
    }

    [Fact]
    public void Music_and_Movie_render_their_slider_values()
    {
        Assert.Equal(["Preamp: -3 dB", "Filter 1: ON LSC Fc 100 Hz Gain 3 dB", "Filter 2: ON HSC Fc 8000 Hz Gain 2 dB"],
            Lines(SoundMode.Music.ToConfigText())[1..]);
        Assert.Equal(["Preamp: -4 dB", "Filter 1: ON LSC Fc 100 Hz Gain 4 dB", "Filter 2: ON PK Fc 300 Hz Gain -1 dB Q 1.0",
                      "Filter 3: ON PK Fc 3000 Hz Gain 2 dB Q 1.0", "Filter 4: ON HSC Fc 8000 Hz Gain 1 dB"],
            Lines(SoundMode.Movie.ToConfigText())[1..]);
    }

    [Fact]
    public void Flat_writes_only_zero_preamp()
    {
        Assert.Equal(["# Managed by SoundModes. Manual edits will be overwritten.", "Preamp: 0 dB"], Lines(SoundMode.Flat.ToConfigText()));
    }

    [Fact]
    public void Numbers_are_written_with_invariant_culture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE"); // uses "," as decimal separator
            var text = new EqSettings(Bass: 2.5).ToConfigText();
            Assert.Contains("Preamp: -2.5 dB", text);
            Assert.Contains("Gain 2.5 dB", text);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void FindPreset_is_case_insensitive_and_excludes_Custom()
    {
        Assert.Same(SoundMode.Movie, SoundMode.FindPreset("movie"));
        Assert.Null(SoundMode.FindPreset("Custom"));
        Assert.Null(SoundMode.FindPreset(null));
    }

    [Fact]
    public void Preset_hotkey_digits_are_unique_and_Custom_has_none()
    {
        Assert.Equal(SoundMode.Presets.Count, SoundMode.Presets.Select(m => m.HotkeyDigit).Distinct().Count());
        Assert.Null(SoundMode.Custom(EqSettings.Flat).HotkeyText);
    }
}
