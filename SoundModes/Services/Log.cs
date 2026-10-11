using System.IO;

namespace SoundModes.Services;

/// <summary>Per-user data folder: %APPDATA%\SoundModes.</summary>
public static class AppPaths
{
    public static string DataDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SoundModes");
}

/// <summary>
/// Minimal structured file logger: "timestamp LEVEL message | exception" lines in %APPDATA%\SoundModes\log.txt.
/// Never throws; logging must not break the app.
/// </summary>
public static class Log
{
    private const long MaxBytes = 1_000_000;
    private static readonly object Gate = new();
    private static string FilePath => Path.Combine(AppPaths.DataDir, "log.txt");

    /// <summary>Logs a normal operation.</summary>
    public static void Info(string message) => Write("INFO", message, null);

    /// <summary>Logs something unusual that did not break the app.</summary>
    public static void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);

    /// <summary>Logs a failure.</summary>
    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                // ponytail: single file, reset when over 1 MB; rotate if history ever matters.
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > MaxBytes) File.Delete(FilePath);
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {level} {message}";
                if (ex != null) line += $" | {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
        }
        catch (Exception) { /* logging is best effort */ }
    }
}
