using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace SoundModes.Tray;

/// <summary>
/// Small on-screen indicator ("Meeting mode ON") near the bottom of the screen. Never takes focus,
/// ignores the mouse, and fades out after about a second.
/// </summary>
public partial class Toast : Window
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20, WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000;
    private int _version;

    public Toast() => InitializeComponent();

    /// <summary>Shows <paramref name="text"/> for ~1 s, then fades out. A new call restarts the timer.</summary>
    public void ShowMessage(string text)
    {
        Message.Text = text;
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;
        Show();
        UpdateLayout();
        var area = SystemParameters.WorkArea;
        Left = area.Left + (area.Width - ActualWidth) / 2;
        Top = area.Bottom - ActualHeight - 48;

        int version = ++_version;
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300)) { BeginTime = TimeSpan.FromSeconds(1) };
        fade.Completed += (_, _) => { if (version == _version) Hide(); };
        BeginAnimation(OpacityProperty, fade);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
