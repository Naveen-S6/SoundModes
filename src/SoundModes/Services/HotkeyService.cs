using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace SoundModes.Services;

/// <summary>Global Ctrl+Alt+key hotkeys via Win32 RegisterHotKey on a hidden message-only window.</summary>
public sealed class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_ALT = 0x1, MOD_CONTROL = 0x2, MOD_NOREPEAT = 0x4000;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private readonly HwndSource _window;
    private readonly Dictionary<int, Action> _actions = [];
    private int _nextId = 1;

    /// <summary>Creates the message-only window that receives WM_HOTKEY.</summary>
    public HotkeyService()
    {
        _window = new HwndSource(new HwndSourceParameters("SoundModesHotkeys") { ParentWindow = HWND_MESSAGE });
        _window.AddHook(WndProc);
    }

    /// <summary>
    /// Registers Ctrl+Alt+<paramref name="key"/> to run <paramref name="action"/> on the UI thread.
    /// Returns false if another app already owns that hotkey.
    /// </summary>
    public bool Register(Key key, Action action)
    {
        int id = _nextId++;
        if (!RegisterHotKey(_window.Handle, id, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, (uint)KeyInterop.VirtualKeyFromKey(key)))
            return false;
        _actions[id] = action;
        return true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>Unregisters all hotkeys and destroys the window.</summary>
    public void Dispose()
    {
        foreach (var id in _actions.Keys) UnregisterHotKey(_window.Handle, id);
        _window.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
