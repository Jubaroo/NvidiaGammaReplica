using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace NvidiaGammaReplica.Services;

[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008,
    NoRepeat = 0x4000
}

public sealed class HotkeyManager : IDisposable
{
    public const int WmHotkey = 0x0312;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly IntPtr _hwnd;
    private readonly List<int> _registered = new();

    public event Action<int>? HotkeyPressed;

    public HotkeyManager(IntPtr hwnd) => _hwnd = hwnd;

    public bool Register(int id, HotkeyModifiers modifiers, Key key)
    {
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        bool ok = RegisterHotKey(_hwnd, id, (uint)(modifiers | HotkeyModifiers.NoRepeat), vk);
        if (ok) _registered.Add(id);
        else Debug.WriteLine($"[Hotkey] Failed to register id={id}, err={Marshal.GetLastWin32Error()}");
        return ok;
    }

    public bool HandleMessage(int msg, IntPtr wParam)
    {
        if (msg != WmHotkey) return false;
        HotkeyPressed?.Invoke(wParam.ToInt32());
        return true;
    }

    public void Dispose()
    {
        foreach (var id in _registered)
        {
            UnregisterHotKey(_hwnd, id);
        }
        _registered.Clear();
    }
}
