using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NvidiaGammaReplica.Models;

namespace NvidiaGammaReplica;

public class DisplayMonitor
{
    public string DeviceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }

    public override string ToString() => IsPrimary ? $"{DisplayName} (Primary)" : DisplayName;
}

public static class GammaManager
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RAMP
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Red;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Green;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public ushort[] Blue;
    }

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

    [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CreateDC(string? lpszDriver, string? lpszDevice, string? lpszOutput, IntPtr lpInitData);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)] public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
        [MarshalAs(UnmanagedType.U4)] public DisplayDeviceStateFlags StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
    }

    [Flags]
    private enum DisplayDeviceStateFlags
    {
        AttachedToDesktop = 0x1,
        PrimaryDevice = 0x4,
        MirroringDriver = 0x8,
    }

    private const double GammaMin = 0.3;
    private const double GammaMax = 2.8;

    private static List<DisplayMonitor>? _cache;

    public static void InvalidateMonitorCache() => _cache = null;

    public static List<DisplayMonitor> GetMonitors()
    {
        if (_cache != null) return _cache;

        var monitors = new List<DisplayMonitor>();
        var d = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };

        for (uint i = 0; EnumDisplayDevices(null, i, ref d, 0); i++)
        {
            bool attached = (d.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) != 0;
            bool mirroring = (d.StateFlags & DisplayDeviceStateFlags.MirroringDriver) != 0;
            if (!attached || mirroring)
            {
                d = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
                continue;
            }

            bool isPrimary = (d.StateFlags & DisplayDeviceStateFlags.PrimaryDevice) != 0;
            monitors.Add(new DisplayMonitor
            {
                DeviceName = d.DeviceName,
                DisplayName = d.DeviceName.Replace(@"\\.\", ""),
                IsPrimary = isPrimary
            });

            d = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
        }

        _cache = monitors;
        return monitors;
    }

    public static RAMP BuildRamp(GammaSettings s)
    {
        double rGamma = Math.Clamp(s.Master + s.RedOffset, GammaMin, GammaMax);
        double gGamma = Math.Clamp(s.Master + s.GreenOffset, GammaMin, GammaMax);
        double bGamma = Math.Clamp(s.Master + s.BlueOffset, GammaMin, GammaMax);
        double contrast = 1.0 + s.Contrast;
        double brightness = s.Brightness;

        var ramp = new RAMP
        {
            Red = new ushort[256],
            Green = new ushort[256],
            Blue = new ushort[256]
        };

        for (int i = 0; i < 256; i++)
        {
            double n = i / 255.0;
            ramp.Red[i] = ToChannel(n, rGamma, contrast, brightness);
            ramp.Green[i] = ToChannel(n, gGamma, contrast, brightness);
            ramp.Blue[i] = ToChannel(n, bGamma, contrast, brightness);
        }

        return ramp;
    }

    private static ushort ToChannel(double n, double gamma, double contrast, double brightness)
    {
        double v = Math.Pow(n, 1.0 / gamma);
        v = (v - 0.5) * contrast + 0.5 + brightness;
        v = Math.Clamp(v, 0.0, 1.0);
        return (ushort)Math.Clamp(v * 65535.0, 0, 65535);
    }

    public static int SetGamma(GammaSettings settings, string? deviceName = null)
    {
        var ramp = BuildRamp(settings);
        int monitorsUpdated = 0;
        var targets = GetMonitors();

        foreach (var m in targets)
        {
            if (deviceName != null && m.DeviceName != deviceName) continue;

            IntPtr hdc = CreateDC(m.DeviceName, null, null, IntPtr.Zero);
            int err1 = Marshal.GetLastWin32Error();
            string usedPath = "lpszDriver";

            if (hdc == IntPtr.Zero)
            {
                hdc = CreateDC(null, m.DeviceName, null, IntPtr.Zero);
                int err2 = Marshal.GetLastWin32Error();
                usedPath = "lpszDevice";
                Debug.WriteLine($"[Gamma] CreateDC fallback for {m.DeviceName}: driver-path err={err1}, device-path err={err2}, hdc={hdc}");
            }

            if (hdc == IntPtr.Zero)
            {
                Debug.WriteLine($"[Gamma] {m.DeviceName}: CreateDC FAILED on both paths.");
                continue;
            }

            bool ok = SetDeviceGammaRamp(hdc, ref ramp);
            int setErr = Marshal.GetLastWin32Error();
            Debug.WriteLine($"[Gamma] {m.DeviceName} (primary={m.IsPrimary}, via {usedPath}): SetDeviceGammaRamp={ok}, master={settings.Master:F2}, lastErr={setErr}");

            if (ok) monitorsUpdated++;
            DeleteDC(hdc);
        }

        return monitorsUpdated;
    }
}
