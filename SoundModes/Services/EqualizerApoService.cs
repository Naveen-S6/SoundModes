using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using SoundModes.Models;

namespace SoundModes.Services;

/// <summary>Result of checking whether Equalizer APO's config can be written.</summary>
public enum ApoStatus { Ok, NotInstalled, NotWritable }

/// <summary>
/// Writes sound modes into Equalizer APO's config.txt. Equalizer APO watches the file and applies changes live.
/// </summary>
public sealed class EqualizerApoService(string configDir)
{
    public const string FallbackConfigDir = @"C:\Program Files\EqualizerAPO\config";
    public const string DownloadUrl = "https://sourceforge.net/projects/equalizerapo/";
    public const string GrantPermissionArg = "--grant-permission";
    private const int WriteAttempts = 3;

    public string ConfigDir { get; } = configDir;
    public string ConfigPath => Path.Combine(ConfigDir, "config.txt");
    public string BackupPath => ConfigPath + ".soundmodes-backup";

    /// <summary>
    /// Returns Equalizer APO's config folder from HKLM\SOFTWARE\EqualizerAPO\ConfigPath,
    /// or the default install folder if the key is missing.
    /// </summary>
    public static string LocateConfigDir()
    {
        try
        {
            using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = hklm.OpenSubKey(@"SOFTWARE\EqualizerAPO");
            if (key?.GetValue("ConfigPath") is string path && path.Length > 0) return path;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            Log.Warn("Could not read Equalizer APO registry key", ex);
        }
        return FallbackConfigDir;
    }

    /// <summary>Checks that the config folder exists and the current user can create and replace files in it.</summary>
    public ApoStatus CheckStatus()
    {
        if (!Directory.Exists(ConfigDir)) return ApoStatus.NotInstalled;
        try
        {
            var probe = Path.Combine(ConfigDir, ".soundmodes-probe");
            File.WriteAllText(probe, "");
            File.Delete(probe);
            if (File.Exists(ConfigPath))
                using (File.Open(ConfigPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite)) { }
            return ApoStatus.Ok;
        }
        catch (UnauthorizedAccessException) { return ApoStatus.NotWritable; }
        catch (IOException) { return ApoStatus.Ok; } // transient lock (e.g. APO reading the file); Apply retries
    }

    /// <summary>
    /// Writes <paramref name="mode"/> (with the global <paramref name="boostDb"/>) to config.txt atomically
    /// (temp file, then replace), backing up the original config once. Retries transient IO errors.
    /// Throws IOException/UnauthorizedAccessException on failure.
    /// </summary>
    public void Apply(SoundMode mode, double boostDb = 0)
    {
        if (File.Exists(ConfigPath) && !File.Exists(BackupPath)) File.Copy(ConfigPath, BackupPath);

        var tmp = ConfigPath + ".soundmodes-tmp";
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                File.WriteAllText(tmp, mode.ToConfigText(boostDb));
                File.Move(tmp, ConfigPath, overwrite: true); // MoveFileEx(REPLACE_EXISTING): never half written
                return;
            }
            catch (IOException ex) when (attempt < WriteAttempts)
            {
                Log.Warn($"Writing config.txt failed (attempt {attempt}), retrying", ex);
                Thread.Sleep(50);
            }
        }
    }

    /// <summary>
    /// Elevated-helper entry point: grants the built-in Users group Modify on the config folder.
    /// The folder (not just config.txt) is needed for the atomic temp-file write and the backup.
    /// Returns icacls' exit code, or 2 if the folder does not exist.
    /// </summary>
    public static int GrantUsersModify(string configDir)
    {
        if (!Directory.Exists(configDir)) return 2;
        var psi = new ProcessStartInfo("icacls.exe") { CreateNoWindow = true, UseShellExecute = false };
        psi.ArgumentList.Add(configDir);
        psi.ArgumentList.Add("/grant");
        psi.ArgumentList.Add("*S-1-5-32-545:(OI)(CI)M");
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        return p.ExitCode;
    }

    /// <summary>
    /// Relaunches this exe elevated (one UAC prompt) to run <see cref="GrantUsersModify"/>.
    /// The helper locates the config folder itself, so no path is passed across the elevation boundary.
    /// Returns true if the helper succeeded; false if the user cancelled UAC or icacls failed.
    /// </summary>
    public static async Task<bool> RequestPermissionAsync()
    {
        try
        {
            var psi = new ProcessStartInfo(Environment.ProcessPath!, GrantPermissionArg) { UseShellExecute = true, Verb = "runas" };
            using var p = Process.Start(psi)!;
            await p.WaitForExitAsync();
            return p.ExitCode == 0;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Log.Warn("Permission helper not run (UAC cancelled?)", ex);
            return false;
        }
    }
}
