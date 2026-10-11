using System.IO;
using System.Text.Json;
using SoundModes.Services;

namespace SoundModes.Models;

/// <summary>User settings persisted to %APPDATA%\SoundModes\settings.json.</summary>
public sealed class Settings
{
    /// <summary>Name of the last applied mode (a preset or "Custom"); null until the user picks one.</summary>
    public string? LastMode { get; set; }

    /// <summary>The user's Custom mode settings; null until they change something in Advanced.</summary>
    public EqSettings? Custom { get; set; }

    /// <summary>Global loudness Boost in dB added to every mode's preamp. Read via <see cref="Boost"/>.</summary>
    public double BoostDb { get; set; }

    /// <summary><see cref="BoostDb"/> clamped to 0..+12 dB (the file may have been hand-edited).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public double Boost => EqSettings.ClampBoost(BoostDb);

    /// <summary>
    /// Resolves a mode name to a preset or the Custom mode (clamped, since the file may have been hand-edited).
    /// Returns null for unknown names, or "Custom" when none has been saved.
    /// </summary>
    public SoundMode? Resolve(string? name) =>
        string.Equals(name, SoundMode.CustomName, StringComparison.OrdinalIgnoreCase)
            ? Custom == null ? null : SoundMode.Custom(Custom.Clamped())
            : SoundMode.FindPreset(name);
}

/// <summary>Loads and saves <see cref="Settings"/> as JSON.</summary>
public static class SettingsStore
{
    /// <summary>Default settings file path under %APPDATA%.</summary>
    public static string DefaultPath => Path.Combine(AppPaths.DataDir, "settings.json");

    /// <summary>
    /// Loads settings from <paramref name="path"/>. A missing file returns defaults; a corrupt file is
    /// kept as "settings.json.bad" and defaults are returned.
    /// </summary>
    public static Settings Load(string path)
    {
        if (!File.Exists(path)) return new Settings();
        try
        {
            return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path)) ?? new Settings();
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Settings file unreadable, using defaults: {path}", ex);
            try { File.Copy(path, path + ".bad", overwrite: true); } catch (IOException) { }
            return new Settings();
        }
    }

    /// <summary>Saves settings to <paramref name="path"/> via temp file + replace. Throws IOException on failure.</summary>
    public static void Save(Settings settings, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(tmp, path, overwrite: true);
    }
}
