using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using SoundModes.Models;
using SoundModes.Services;

namespace SoundModes.Tray;

/// <summary>
/// The left-click panel: mode buttons, setup errors, volume slider, and a collapsed Advanced section for
/// viewing/editing the active mode's EQ (edits become the Custom mode). Hides on focus loss or Esc and is reused.
/// </summary>
public partial class TrayPopup : Window
{
    private readonly DispatcherTimer _editDebounce, _boostDebounce;
    private readonly RadioButton _customButton;
    private bool _syncingVolume, _syncingEq, _syncingBoost;
    private ApoStatus _status;

    /// <summary>When the popup last hid; lets a tray click that caused the hide not immediately reopen it.</summary>
    public DateTime HiddenAt { get; private set; }

    /// <summary>Raised with the chosen mode's name (a preset or "Custom").</summary>
    public event Action<string>? ModeSelected;
    public event Action<int>? VolumeChangeRequested;
    public event Action? CheckAgainClicked, GrantPermissionClicked;
    /// <summary>Raised when the user changes anything in Advanced; the app saves it as the Custom mode.</summary>
    public event Action<EqSettings>? CustomEdited;
    /// <summary>Raised (debounced) when the user moves the Boost slider, with the new value in dB.</summary>
    public event Action<double>? BoostChanged;

    /// <summary>Builds the preset buttons plus a Custom button that stays hidden until a Custom mode exists.</summary>
    public TrayPopup()
    {
        InitializeComponent();
        foreach (var mode in SoundMode.Presets)
            AddModeButton(mode.Name, mode.HotkeyText);
        _customButton = AddModeButton(SoundMode.CustomName, Strings.CustomTip);
        HintText.Text = Strings.DeviceHint;
        AdvancedLabel.Text = Strings.AdvancedClosed;
        _editDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _editDebounce.Tick += (_, _) => RaiseCustomEdited();
        foreach (var slider in new[] { BassSlider, LowMidSlider, VoiceSlider, TrebleSlider })
            slider.ValueChanged += OnEqSliderChanged;
        _boostDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _boostDebounce.Tick += (_, _) => { _boostDebounce.Stop(); BoostChanged?.Invoke(BoostSlider.Value); };
        BoostSlider.ValueChanged += OnBoostSliderChanged;
        Deactivated += (_, _) => HidePopup();
        PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) HidePopup(); };
        SizeChanged += (_, _) => PlaceAboveTray();
    }

    private RadioButton AddModeButton(string name, string? tooltip)
    {
        var button = new RadioButton { Content = name, Tag = name, ToolTip = tooltip };
        button.Click += (_, _) => ModeSelected?.Invoke(name);
        ModeButtons.Children.Add(button);
        return button;
    }

    /// <summary>Shows the popup above the tray area, themed, with keyboard focus on the active mode.</summary>
    public void ShowPopup()
    {
        ApplyTheme();
        Show();
        PlaceAboveTray();
        Activate();
        var focus = ModeButtons.Children.OfType<RadioButton>().FirstOrDefault(b => b.IsChecked == true)
                    ?? ModeButtons.Children.OfType<RadioButton>().First();
        focus.Focus();
    }

    /// <summary>Hides the popup and records when.</summary>
    public void HidePopup()
    {
        if (!IsVisible) return;
        Hide();
        HiddenAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Reflects the active mode, setup status and whether a Custom mode exists. The Advanced section shows the
    /// active mode's actual values; mode buttons and Advanced are disabled until setup is OK.
    /// </summary>
    public void ShowState(SoundMode? active, ApoStatus status, bool hasCustom)
    {
        _status = status;
        _customButton.Visibility = hasCustom ? Visibility.Visible : Visibility.Collapsed;
        foreach (var button in ModeButtons.Children.OfType<RadioButton>())
        {
            button.IsChecked = (string)button.Tag == active?.Name;
            button.IsEnabled = status == ApoStatus.Ok;
        }
        SetupPanel.Visibility = status == ApoStatus.Ok ? Visibility.Collapsed : Visibility.Visible;
        SetupText.Text = status == ApoStatus.NotInstalled ? Strings.ApoNotFound : Strings.ApoNotWritable;
        SetupAction.Content = status == ApoStatus.NotInstalled ? Strings.Download : Strings.GrantPermission;

        _editDebounce.Stop(); // a pending slider change belonged to the previous state
        EqControls.IsEnabled = status == ApoStatus.Ok;
        BoostSlider.IsEnabled = status == ApoStatus.Ok;
        AdvancedTitle.Text = active == null ? Strings.AdvancedNoMode
            : active.IsCustom ? Strings.AdvancedCustom
            : string.Format(Strings.AdvancedPreset, active.Name);
        ShowEq(active?.Eq ?? EqSettings.Flat);
    }

    private void ShowEq(EqSettings eq)
    {
        _syncingEq = true;
        BassSlider.Value = eq.Bass;
        LowMidSlider.Value = eq.LowMid;
        VoiceSlider.Value = eq.Voice;
        TrebleSlider.Value = eq.Treble;
        CutRumbleCheck.IsChecked = eq.CutRumble;
        ExtraLinesBox.Text = eq.ExtraLines;
        ShowSliderValues();
        _syncingEq = false;
    }

    // Reads every Advanced control, so a slider move also applies any typed custom lines.
    private EqSettings CurrentEq() => new EqSettings(
        BassSlider.Value, LowMidSlider.Value, VoiceSlider.Value, TrebleSlider.Value,
        CutRumble: CutRumbleCheck.IsChecked == true,
        ExtraLines: ExtraLinesBox.Text).Clamped();

    private void RaiseCustomEdited()
    {
        _editDebounce.Stop();
        CustomEdited?.Invoke(CurrentEq());
    }

    private void OnEqSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ShowSliderValues();
        if (_syncingEq) return;
        _editDebounce.Stop(); // live preview, but at most one config write per pause in dragging
        _editDebounce.Start();
    }

    private void ShowSliderValues()
    {
        BassText.Text = FormatDb(BassSlider.Value);
        LowMidText.Text = FormatDb(LowMidSlider.Value);
        VoiceText.Text = FormatDb(VoiceSlider.Value);
        TrebleText.Text = FormatDb(TrebleSlider.Value);
    }

    /// <summary>Updates the Boost slider without echoing the change back.</summary>
    public void ShowBoost(double boostDb)
    {
        _syncingBoost = true;
        BoostSlider.Value = boostDb;
        BoostText.Text = FormatDb(boostDb);
        _syncingBoost = false;
    }

    private void OnBoostSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        BoostText.Text = FormatDb(e.NewValue);
        if (_syncingBoost) return;
        _boostDebounce.Stop(); // live, but one config write per pause in dragging
        _boostDebounce.Start();
    }

    private static string FormatDb(double db) => db > 0 ? $"+{db:0} dB" : $"{db:0} dB";

    private void OnCutRumbleClick(object sender, RoutedEventArgs e) => RaiseCustomEdited();

    private void OnApplyLinesClick(object sender, RoutedEventArgs e) => RaiseCustomEdited();

    private void OnAdvancedToggled(object sender, RoutedEventArgs e)
    {
        bool open = AdvancedToggle.IsChecked == true;
        AdvancedPanel.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        AdvancedLabel.Text = open ? Strings.AdvancedOpen : Strings.AdvancedClosed;
    }

    /// <summary>Shows a one-off message in the setup area (e.g. permission was not granted).</summary>
    public void ShowSetupMessage(string text) => SetupText.Text = text;

    /// <summary>Updates the slider without echoing the change back; null disables it (no device).</summary>
    public void ShowVolume(int? percent)
    {
        _syncingVolume = true;
        VolumeSlider.IsEnabled = percent != null;
        VolumeSlider.Value = percent ?? 0;
        VolumeText.Text = percent?.ToString() ?? "-";
        _syncingVolume = false;
    }

    private void OnVolumeSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        VolumeText.Text = ((int)e.NewValue).ToString();
        if (!_syncingVolume) VolumeChangeRequested?.Invoke((int)e.NewValue);
    }

    private void OnSetupActionClick(object sender, RoutedEventArgs e)
    {
        if (_status == ApoStatus.NotInstalled) App.OpenUrl(EqualizerApoService.DownloadUrl);
        else GrantPermissionClicked?.Invoke();
    }

    private void OnCheckAgainClick(object sender, RoutedEventArgs e) => CheckAgainClicked?.Invoke();

    private void OnCloseClick(object sender, RoutedEventArgs e) => HidePopup();

    // ponytail: anchors to the bottom-right of the primary work area; per-monitor tray detection if the taskbar moves.
    private void PlaceAboveTray()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth;
        Top = area.Bottom - ActualHeight;
    }

    private void ApplyTheme()
    {
        bool light = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1) is not 0;
        SetBrush("Bg", light ? "#F9F9F9" : "#2B2B2B");
        SetBrush("Fg", light ? "#1A1A1A" : "#FFFFFF");
        SetBrush("Subtle", light ? "#5C5C5C" : "#C5C5C5");
        SetBrush("Edge", light ? "#E0E0E0" : "#3D3D3D");
        SetBrush("ButtonBg", light ? "#EDEDED" : "#3A3A3A");
        SetBrush("ButtonHover", light ? "#E0E0E0" : "#474747");
        SetBrush("Warning", light ? "#9D5D00" : "#FCE100");
    }

    private void SetBrush(string key, string hex) =>
        Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
}
