using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using SoundModes.Models;
using SoundModes.Services;
using SoundModes.Tray;

namespace SoundModes;

/// <summary>
/// Entry point and controller: single instance, tray, popup, hotkeys, volume, and mode switching.
/// Also runs as the elevated permission helper when started with --grant-permission.
/// </summary>
public partial class App : Application
{
    private const string MutexName = @"Local\SoundModes.SingleInstance";
    private const string ShowEventName = @"Local\SoundModes.ShowPopup";

    private Mutex? _singleInstance;
    private EventWaitHandle? _showSignal;
    private EqualizerApoService _apo = null!;
    private Settings _settings = null!;
    private TrayIcon? _tray;
    private TrayPopup _popup = null!;
    private Toast _toast = null!;
    private HotkeyService? _hotkeys;
    private VolumeService? _volume;
    private SoundMode? _current;
    private ApoStatus _status;

    /// <summary>Starts the app, or runs the permission helper and exits.</summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args is [EqualizerApoService.GrantPermissionArg])
        {
            Shutdown(EqualizerApoService.GrantUsersModify(EqualizerApoService.LocateConfigDir()));
            return;
        }

        _singleInstance = new Mutex(true, MutexName, out bool isFirst);
        if (!isFirst)
        {
            SignalRunningInstance();
            Shutdown();
            return;
        }

        InstallErrorHandlers();
        // Two tiny windows don't need the GPU; software rendering avoids ~100 MB of driver allocations.
        System.Windows.Media.RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.SoftwareOnly;
        _apo =new EqualizerApoService(EqualizerApoService.LocateConfigDir());
        _settings = SettingsStore.Load(SettingsStore.DefaultPath);
        BuildUi();
        StartVolume();
        RegisterHotkeys();
        ListenForSecondLaunch();
        if (Autostart.IsEnabled) TryRun(() => Autostart.Set(true), "Refreshing autostart path"); // exe may have moved

        _status = _apo.CheckStatus();
        var last = _settings.Resolve(_settings.LastMode);
        if (last != null) ApplyMode(last);
        else RefreshUi();
        Log.Info($"Started. Config dir: {_apo.ConfigDir}, status: {_status}");
    }

    private void BuildUi()
    {
        _toast = new Toast();
        _popup = new TrayPopup();
        _popup.ModeSelected += ApplyModeByName;
        _popup.VolumeChangeRequested += v => _volume?.SetVolume(v);
        _popup.CheckAgainClicked += RecheckSetup;
        _popup.GrantPermissionClicked += GrantPermission;
        _popup.CustomEdited += ApplyCustom;
        _popup.BoostChanged += ApplyBoost;
        _popup.ShowBoost(_settings.Boost);

        _tray = new TrayIcon { AutostartChecked = Autostart.IsEnabled };
        _tray.ModeClicked += ApplyModeByName;
        _tray.LeftClicked += TogglePopup;
        _tray.AutostartToggled += on => TryRun(() => Autostart.Set(on), "Changing autostart");
        _tray.OpenApoFolderClicked += OpenApoFolder;
        _tray.AboutClicked += () => MessageBox.Show(string.Format(Strings.About, typeof(App).Assembly.GetName().Version?.ToString(3)),
            Strings.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
        _tray.ExitClicked += Shutdown;
    }

    private void StartVolume()
    {
        try
        {
            _volume = new VolumeService();
            _volume.Changed += v => _popup.ShowVolume(v);
            _popup.ShowVolume(_volume.Volume);
        }
        catch (Exception ex) // Core Audio unavailable: modes still work, slider stays disabled
        {
            Log.Error("Volume control unavailable", ex);
            _popup.ShowVolume(null);
        }
    }

    private void RegisterHotkeys()
    {
        _hotkeys = new HotkeyService();
        var taken = SoundMode.Presets
            .Where(m => !_hotkeys.Register(Key.D0 + (m.HotkeyDigit!.Value - '0'), () => ApplyMode(m)))
            .Select(m => m.HotkeyText)
            .ToList();
        if (taken.Count > 0)
        {
            Log.Warn($"Hotkeys already in use: {string.Join(", ", taken)}");
            _tray!.Warn(string.Format(Strings.HotkeyTaken, string.Join(", ", taken)));
        }
    }

    /// <summary>
    /// Switches to <paramref name="mode"/>: writes config.txt, saves it as last mode, updates the UI.
    /// Shows a toast when the popup is not open. If setup is broken, shows the popup with the fix instead.
    /// </summary>
    private void ApplyMode(SoundMode mode)
    {
        _status = _apo.CheckStatus();
        if (_status != ApoStatus.Ok)
        {
            RefreshUi();
            _popup.ShowPopup();
            return;
        }

        var timer = Stopwatch.StartNew();
        try
        {
            _apo.Apply(mode, _settings.Boost);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Applying {mode.Name} failed", ex);
            _status = _apo.CheckStatus();
            RefreshUi();
            _toast.ShowMessage(Strings.SwitchFailed);
            return;
        }

        _current = mode;
        _settings.LastMode = mode.Name;
        TryRun(() => SettingsStore.Save(_settings, SettingsStore.DefaultPath), "Saving settings");
        RefreshUi();
        if (!_popup.IsVisible) _toast.ShowMessage(string.Format(Strings.ModeOn, mode.Name));
        Log.Info($"Applied {mode.Name} in {timer.ElapsedMilliseconds} ms");
    }

    /// <summary>Applies a preset or "Custom" by name, as chosen in the tray menu or popup. Unknown names are ignored.</summary>
    private void ApplyModeByName(string name)
    {
        if (_settings.Resolve(name) is { } mode) ApplyMode(mode);
    }

    /// <summary>Saves <paramref name="eq"/> as the Custom mode and switches to it live (edits from Advanced).</summary>
    /// <summary>Saves the global Boost (dB) and re-applies the active mode so it takes effect live.</summary>
    private void ApplyBoost(double boostDb)
    {
        _settings.BoostDb = EqSettings.ClampBoost(boostDb);
        if (_current != null) ApplyMode(_current); // also saves settings
        else TryRun(() => SettingsStore.Save(_settings, SettingsStore.DefaultPath), "Saving settings");
    }

    private void ApplyCustom(EqSettings eq)
    {
        _settings.Custom = eq.Clamped();
        ApplyMode(SoundMode.Custom(_settings.Custom)); // also saves settings
    }

    private void RefreshUi()
    {
        _tray?.Show(_current, setupNeeded: _status != ApoStatus.Ok, hasCustom: _settings.Custom != null);
        _popup.ShowState(_current, _status, hasCustom: _settings.Custom != null);
    }

    private void RecheckSetup()
    {
        _status = _apo.CheckStatus();
        var last = _settings.Resolve(_settings.LastMode);
        if (_status == ApoStatus.Ok && last != null && _current == null) ApplyMode(last);
        else RefreshUi();
    }

    private async void GrantPermission()
    {
        bool granted = await EqualizerApoService.RequestPermissionAsync();
        RecheckSetup();
        if (!granted) _popup.ShowSetupMessage(Strings.GrantFailed);
    }

    private void TogglePopup()
    {
        if (_popup.IsVisible) _popup.HidePopup();
        // The click on the tray icon deactivates the popup first; don't reopen it straight away.
        else if (DateTime.UtcNow - _popup.HiddenAt > TimeSpan.FromMilliseconds(300)) _popup.ShowPopup();
    }

    private void OpenApoFolder()
    {
        if (Directory.Exists(_apo.ConfigDir)) OpenUrl(_apo.ConfigDir);
        else _popup.ShowPopup(); // shows the "not found" message
    }

    /// <summary>Opens a URL or folder with the shell. Failures are logged, not thrown.</summary>
    public static void OpenUrl(string target) =>
        TryRun(() => Process.Start(new ProcessStartInfo(target) { UseShellExecute = true })?.Dispose(), $"Opening {target}");

    private void SignalRunningInstance()
    {
        try { using var signal = EventWaitHandle.OpenExisting(ShowEventName); signal.Set(); }
        catch (WaitHandleCannotBeOpenedException) { } // first instance still starting
    }

    private void ListenForSecondLaunch()
    {
        _showSignal = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        ThreadPool.RegisterWaitForSingleObject(_showSignal, (_, _) => Dispatcher.BeginInvoke(_popup.ShowPopup), null, -1, false);
    }

    private static void TryRun(Action action, string what)
    {
        try { action(); }
        catch (Exception ex) { Log.Error($"{what} failed", ex); }
    }

    private void InstallErrorHandlers()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            Log.Error("Unhandled UI exception", e.Exception);
            MessageBox.Show(Strings.UnexpectedError, Strings.AppName, MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Error("Unhandled exception", e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => { Log.Error("Unobserved task exception", e.Exception); e.SetObserved(); };
    }

    /// <summary>Releases the tray icon, hotkeys and audio on exit.</summary>
    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        _hotkeys?.Dispose();
        _volume?.Dispose();
        _showSignal?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
