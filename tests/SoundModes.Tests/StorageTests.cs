using SoundModes.Models;
using SoundModes.Services;

namespace SoundModes.Tests;

/// <summary>Equalizer APO file writing and settings persistence, against a throwaway temp folder.</summary>
public sealed class StorageTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("soundmodes-test-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Missing_config_folder_reports_not_installed()
    {
        var apo = new EqualizerApoService(Path.Combine(_dir, "nope"));
        Assert.Equal(ApoStatus.NotInstalled, apo.CheckStatus());
    }

    [Fact]
    public void Writable_folder_reports_ok_and_leaves_no_probe_file()
    {
        var apo = new EqualizerApoService(_dir);
        Assert.Equal(ApoStatus.Ok, apo.CheckStatus());
        Assert.Empty(Directory.GetFiles(_dir));
    }

    [Fact]
    public void Apply_writes_mode_and_leaves_no_temp_file()
    {
        var apo = new EqualizerApoService(_dir);
        apo.Apply(SoundMode.Music);
        Assert.Equal(SoundMode.Music.ToConfigText(), File.ReadAllText(apo.ConfigPath));
        Assert.Equal([apo.ConfigPath], Directory.GetFiles(_dir));
    }

    [Fact]
    public void Original_config_is_backed_up_once_and_never_overwritten()
    {
        var apo = new EqualizerApoService(_dir);
        File.WriteAllText(apo.ConfigPath, "original user config");

        apo.Apply(SoundMode.Meeting);
        apo.Apply(SoundMode.Movie);

        Assert.Equal("original user config", File.ReadAllText(apo.BackupPath));
        Assert.Equal(SoundMode.Movie.ToConfigText(), File.ReadAllText(apo.ConfigPath));
    }

    [Fact]
    public void Settings_round_trip()
    {
        var path = Path.Combine(_dir, "sub", "settings.json");
        var custom = new EqSettings(Bass: 3, LowMid: -2, CutRumble: true, ExtraLines: "Preamp: -1 dB");
        SettingsStore.Save(new Settings { LastMode = "Custom", Custom = custom, BoostDb = 4 }, path);
        var loaded = SettingsStore.Load(path);
        Assert.Equal("Custom", loaded.LastMode);
        Assert.Equal(4, loaded.Boost);
        Assert.Equal(custom, loaded.Custom);
    }

    [Fact]
    public void Missing_settings_file_gives_defaults()
    {
        Assert.Null(SettingsStore.Load(Path.Combine(_dir, "none.json")).LastMode);
    }

    [Fact]
    public void Corrupt_settings_give_defaults_and_keep_a_bad_copy()
    {
        var path = Path.Combine(_dir, "settings.json");
        File.WriteAllText(path, "{ not json");
        Assert.Null(SettingsStore.Load(path).LastMode);
        Assert.Equal("{ not json", File.ReadAllText(path + ".bad"));
    }
}
