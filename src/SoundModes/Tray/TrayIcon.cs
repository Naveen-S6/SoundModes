using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using SoundModes.Models;

namespace SoundModes.Tray;

/// <summary>
/// The system tray icon (WinForms NotifyIcon) and its right-click menu.
/// Icons are drawn at startup (colored circle + glyph per mode), so there are no .ico assets to maintain.
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly Dictionary<string, ToolStripMenuItem> _modeItems = [];
    private readonly ToolStripMenuItem _autostartItem;
    private readonly Dictionary<string, Icon> _icons = new()
    {
        [SoundMode.Meeting.Name] = Draw(Color.FromArgb(0x00, 0x67, 0xC0), "M"),
        [SoundMode.Music.Name] = Draw(Color.FromArgb(0x10, 0x7C, 0x10), "♪"),
        [SoundMode.Movie.Name] = Draw(Color.FromArgb(0x87, 0x64, 0xB8), "▶"),
        [SoundMode.Flat.Name] = Draw(Color.FromArgb(0x5C, 0x5C, 0x5C), "F"),
        [SoundMode.CustomName] = Draw(Color.FromArgb(0xCA, 0x50, 0x10), "C"),
        [NoModeKey] = Draw(Color.FromArgb(0x5C, 0x5C, 0x5C), "S"),
        [SetupKey] = Draw(Color.FromArgb(0xC4, 0x2B, 0x1C), "!"),
    };
    private const string NoModeKey = "", SetupKey = "!";

    /// <summary>Raised with the clicked mode's name (a preset or "Custom").</summary>
    public event Action<string>? ModeClicked;
    public event Action? LeftClicked, OpenApoFolderClicked, AboutClicked, ExitClicked;
    public event Action<bool>? AutostartToggled;

    /// <summary>Builds the menu and shows the icon.</summary>
    public TrayIcon()
    {
        var menu = new ContextMenuStrip();
        foreach (var (name, hotkey) in SoundMode.Presets.Select(m => (m.Name, m.HotkeyText)).Append((SoundMode.CustomName, null)))
        {
            var item = new ToolStripMenuItem(name, null, (_, _) => ModeClicked?.Invoke(name)) { ShortcutKeyDisplayString = hotkey };
            _modeItems[name] = item;
            menu.Items.Add(item);
        }
        menu.Items.Add(new ToolStripSeparator());
        _autostartItem = new ToolStripMenuItem(Strings.MenuStartWithWindows) { CheckOnClick = true };
        _autostartItem.Click += (_, _) => AutostartToggled?.Invoke(_autostartItem.Checked);
        menu.Items.Add(_autostartItem);
        menu.Items.Add(Strings.MenuOpenApoFolder, null, (_, _) => OpenApoFolderClicked?.Invoke());
        menu.Items.Add(Strings.MenuAbout, null, (_, _) => AboutClicked?.Invoke());
        menu.Items.Add(Strings.MenuExit, null, (_, _) => ExitClicked?.Invoke());

        _icon = new NotifyIcon { ContextMenuStrip = menu, Icon = _icons[NoModeKey], Text = Strings.TooltipNoMode, Visible = true };
        _icon.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) LeftClicked?.Invoke(); };
    }

    /// <summary>
    /// Updates icon, tooltip and menu checkmarks for the active mode (or the setup-needed state).
    /// The Custom menu item only shows once the user has made a Custom mode.
    /// </summary>
    public void Show(SoundMode? mode, bool setupNeeded, bool hasCustom)
    {
        _icon.Icon = _icons[setupNeeded ? SetupKey : mode?.Name ?? NoModeKey];
        _icon.Text = setupNeeded ? Strings.TooltipSetup
            : mode == null ? Strings.TooltipNoMode
            : string.Format(Strings.Tooltip, mode.Name);
        foreach (var (name, item) in _modeItems) item.Checked = name == mode?.Name;
        _modeItems[SoundMode.CustomName].Visible = hasCustom;
    }

    /// <summary>Sets the "Start with Windows" checkmark.</summary>
    public bool AutostartChecked { set => _autostartItem.Checked = value; }

    /// <summary>Shows a Windows balloon notification for warnings that must stay visible longer than a toast.</summary>
    public void Warn(string text) => _icon.ShowBalloonTip(5000, Strings.AppName, text, ToolTipIcon.Warning);

    private static Icon Draw(Color color, string glyph)
    {
        int size = SystemInformation.SmallIconSize.Width; // DPI-aware tray size
        using var bmp = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bmp))
        using (var fill = new SolidBrush(color))
        using (var font = new Font("Segoe UI Symbol", size * 0.55f, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel))
        using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.FillEllipse(fill, 0, 0, size - 1, size - 1);
            g.DrawString(glyph, font, Brushes.White, new RectangleF(0, 0, size, size), format);
        }
        // ponytail: HICON is never destroyed; six icons live for the whole process.
        return Icon.FromHandle(bmp.GetHicon());
    }

    /// <summary>Removes the icon from the tray (otherwise a ghost icon stays until hovered).</summary>
    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
    }
}
