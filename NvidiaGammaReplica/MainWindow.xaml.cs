using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using NvidiaGammaReplica.Models;
using NvidiaGammaReplica.Services;

namespace NvidiaGammaReplica;

public partial class MainWindow : Window
{
    private const string AllMonitorsKey = "ALL";

    private List<DisplayMonitor> _monitors = new();
    private bool _isUpdating = true;
    private bool _shuttingDown;

    private AppSettings _appSettings = new();
    private readonly DispatcherTimer _saveTimer;

    private HotkeyManager? _hotkeys;
    private Controls.OsdWindow? _osdWindow;
    private readonly List<Controls.IdentifyWindow> _activeIdentifyWindows = new();

    private const int HotkeyIdUp = 1;
    private const int HotkeyIdDown = 2;
    private const int HotkeyIdReset = 3;

    private const int WM_DISPLAYCHANGE = 0x007E;
    private const int WM_DEVICECHANGE = 0x0219;

    public MainWindow()
    {
        InitializeComponent();
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SettingsStore.Save(_appSettings);
        };

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        SourceInitialized += MainWindow_SourceInitialized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var src = HwndSource.FromHwnd(hwnd);
        src?.AddHook(WndProc);

        _hotkeys = new HotkeyManager(hwnd);
        _hotkeys.HotkeyPressed += OnHotkeyPressed;
        _hotkeys.Register(HotkeyIdUp, HotkeyModifiers.Control | HotkeyModifiers.Alt, Key.Up);
        _hotkeys.Register(HotkeyIdDown, HotkeyModifiers.Control | HotkeyModifiers.Alt, Key.Down);
        _hotkeys.Register(HotkeyIdReset, HotkeyModifiers.Control | HotkeyModifiers.Alt, Key.D0);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (_hotkeys != null && _hotkeys.HandleMessage(msg, wParam))
        {
            handled = true;
            return IntPtr.Zero;
        }

        if (msg == WM_DISPLAYCHANGE || msg == WM_DEVICECHANGE)
        {
            GammaManager.InvalidateMonitorCache();
            Dispatcher.BeginInvoke(new Action(RefreshMonitors));
        }
        return IntPtr.Zero;
    }

    private void OnHotkeyPressed(int id)
    {
        if (MonitorComboBox.SelectedItem is not DisplayMonitor selected) return;
        switch (id)
        {
            case HotkeyIdUp:
                StopSliderAnimations();
                GammaSlider.Value = Math.Min(GammaSlider.Maximum, GammaSlider.Value + 0.05);
                ShowOsd(GammaSlider.Value);
                break;
            case HotkeyIdDown:
                StopSliderAnimations();
                GammaSlider.Value = Math.Max(GammaSlider.Minimum, GammaSlider.Value - 0.05);
                ShowOsd(GammaSlider.Value);
                break;
            case HotkeyIdReset:
                ApplySettingsToUi(GammaSettings.Default());
                ShowOsd(1.00);
                break;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        _appSettings = SettingsStore.Load();
        LoadMonitors();
        StartWithWindowsMenuItem.IsChecked = AutoStartService.IsEnabled();

        if (Application.Current is App { StartMinimized: true })
        {
            Hide();
        }
    }
    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_shuttingDown)
        {
            _osdWindow?.Close();
            foreach (var w in _activeIdentifyWindows)
            {
                try { w.Close(); } catch { }
            }
            _activeIdentifyWindows.Clear();
            return;
        }
        e.Cancel = true;
        Hide();
    }

    private void LoadMonitors(string? preserveSelection = null)
    {
        _isUpdating = true;
        _monitors = GammaManager.GetMonitors();

        var allMonitorsOption = new DisplayMonitor
        {
            DeviceName = AllMonitorsKey,
            DisplayName = "All Monitors",
            IsPrimary = false
        };

        var comboOptions = new List<DisplayMonitor> { allMonitorsOption };
        comboOptions.AddRange(_monitors);

        MonitorComboBox.ItemsSource = comboOptions;

        foreach (var m in _monitors)
        {
            if (!_appSettings.Monitors.ContainsKey(m.DeviceName))
            {
                _appSettings.Monitors[m.DeviceName] = new GammaSettings();
            }
            else
            {
                GammaManager.SetGamma(_appSettings.Monitors[m.DeviceName], m.DeviceName);
            }
        }

        DisplayMonitor? target = null;
        if (preserveSelection != null)
        {
            target = comboOptions.FirstOrDefault(m => m.DeviceName == preserveSelection);
        }
        target ??= comboOptions.FirstOrDefault(m => m.IsPrimary) ?? comboOptions.FirstOrDefault();

        MonitorComboBox.SelectedItem = target;
        _isUpdating = false;

        if (target != null) PullSettingsIntoUi(target);
    }

    private void RefreshMonitors()
    {
        var current = GammaManager.GetMonitors();
        var currentNames = current.Select(m => m.DeviceName).ToHashSet();
        var previousNames = _monitors.Select(m => m.DeviceName).ToHashSet();
        if (currentNames.SetEquals(previousNames)) return;

        var previousSelection = (MonitorComboBox.SelectedItem as DisplayMonitor)?.DeviceName;
        LoadMonitors(preserveSelection: previousSelection);

        foreach (var m in _monitors)
        {
            if (_appSettings.Monitors.TryGetValue(m.DeviceName, out var s) && !s.IsDefault)
            {
                GammaManager.SetGamma(s, m.DeviceName);
            }
        }
    }

    private GammaSettings GetActiveSettings()
    {
        if (MonitorComboBox.SelectedItem is not DisplayMonitor selected)
            return new GammaSettings();

        if (selected.DeviceName == AllMonitorsKey)
        {
            // Use first monitor as the source-of-truth for the UI when "All" is picked.
            var first = _monitors.FirstOrDefault();
            if (first != null && _appSettings.Monitors.TryGetValue(first.DeviceName, out var s))
                return s;
            return new GammaSettings();
        }

        if (!_appSettings.Monitors.TryGetValue(selected.DeviceName, out var existing))
        {
            existing = new GammaSettings();
            _appSettings.Monitors[selected.DeviceName] = existing;
        }
        return existing;
    }

    private void PullSettingsIntoUi(DisplayMonitor monitor)
    {
        _isUpdating = true;
        var s = monitor.DeviceName == AllMonitorsKey
            ? GetActiveSettings()
            : _appSettings.Monitors.TryGetValue(monitor.DeviceName, out var existing) ? existing : new GammaSettings();

        GammaSlider.Value = s.Master;
        RedSlider.Value = s.RedOffset;
        GreenSlider.Value = s.GreenOffset;
        BlueSlider.Value = s.BlueOffset;
        BrightnessSlider.Value = s.Brightness;
        ContrastSlider.Value = s.Contrast;

        UpdateValueLabels(s);
        CurvePreview.Update(s);
        _isUpdating = false;
        UpdateActivePresetHighlight();
    }

    private void StopSliderAnimations()
    {
        GammaSlider.BeginAnimation(Slider.ValueProperty, null);
        RedSlider.BeginAnimation(Slider.ValueProperty, null);
        GreenSlider.BeginAnimation(Slider.ValueProperty, null);
        BlueSlider.BeginAnimation(Slider.ValueProperty, null);
        BrightnessSlider.BeginAnimation(Slider.ValueProperty, null);
        ContrastSlider.BeginAnimation(Slider.ValueProperty, null);
    }

    private void AnimateSliderTo(Slider slider, double targetValue, Action? onCompleted = null)
    {
        var anim = new DoubleAnimation
        {
            To = targetValue,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        anim.Completed += (s, e) =>
        {
            slider.BeginAnimation(Slider.ValueProperty, null);
            slider.Value = targetValue;
            onCompleted?.Invoke();
        };
        slider.BeginAnimation(Slider.ValueProperty, anim);
    }

    private void ApplySettingsToUi(GammaSettings s)
    {
        StopSliderAnimations();

        _isUpdating = false; // Allow slider changes to propagate to hardware in real time

        int completedCount = 0;
        Action checkDone = () =>
        {
            completedCount++;
            if (completedCount == 6)
            {
                PulseValues();
                UpdateActivePresetHighlight();
            }
        };

        AnimateSliderTo(GammaSlider, s.Master, checkDone);
        AnimateSliderTo(RedSlider, s.RedOffset, checkDone);
        AnimateSliderTo(GreenSlider, s.GreenOffset, checkDone);
        AnimateSliderTo(BlueSlider, s.BlueOffset, checkDone);
        AnimateSliderTo(BrightnessSlider, s.Brightness, checkDone);
        AnimateSliderTo(ContrastSlider, s.Contrast, checkDone);
    }

    private void WriteUiIntoSettings()
    {
        if (MonitorComboBox.SelectedItem is not DisplayMonitor selected) return;

        var snap = new GammaSettings
        {
            Master = GammaSlider.Value,
            RedOffset = SnapZero(RedSlider.Value),
            GreenOffset = SnapZero(GreenSlider.Value),
            BlueOffset = SnapZero(BlueSlider.Value),
            Brightness = SnapZero(BrightnessSlider.Value),
            Contrast = SnapZero(ContrastSlider.Value)
        };

        if (selected.DeviceName == AllMonitorsKey)
        {
            foreach (var m in _monitors)
            {
                _appSettings.Monitors[m.DeviceName] = snap.Clone();
            }
        }
        else
        {
            _appSettings.Monitors[selected.DeviceName] = snap;
        }
    }

    private void ApplyCurrentToHardware()
    {
        if (MonitorComboBox.SelectedItem is not DisplayMonitor selected) return;
        var s = GetActiveSettings();
        if (selected.DeviceName == AllMonitorsKey)
            GammaManager.SetGamma(s, null);
        else
            GammaManager.SetGamma(s, selected.DeviceName);
    }

    private static double SnapZero(double v) => Math.Abs(v) < 0.01 ? 0.0 : v;

    private void UpdateValueLabels(GammaSettings s)
    {
        SetIfNotFocused(GammaValueText, s.Master.ToString("F2", CultureInfo.InvariantCulture));
        SetIfNotFocused(RedValueText, FormatSigned(s.RedOffset));
        SetIfNotFocused(GreenValueText, FormatSigned(s.GreenOffset));
        SetIfNotFocused(BlueValueText, FormatSigned(s.BlueOffset));
        SetIfNotFocused(BrightnessValueText, FormatSigned(s.Brightness));
        SetIfNotFocused(ContrastValueText, FormatSigned(s.Contrast));
    }

    private static void SetIfNotFocused(TextBox box, string text)
    {
        if (!box.IsKeyboardFocused) box.Text = text;
    }

    private void PulseValues()
    {
        PulseValue(GammaValueText);
        PulseValue(RedValueText);
        PulseValue(GreenValueText);
        PulseValue(BlueValueText);
        PulseValue(BrightnessValueText);
        PulseValue(ContrastValueText);
    }

    private static void PulseValue(TextBox box)
    {
        if (box.RenderTransform is not ScaleTransform scale) return;
        var anim = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(220),
            KeyFrames =
            {
                new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)),
                new EasingDoubleKeyFrame(1.18, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90)),
                    new CubicEase { EasingMode = EasingMode.EaseOut }),
                new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220)),
                    new CubicEase { EasingMode = EasingMode.EaseIn })
            }
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    // Keeps the gamma readout glowing while the value is actively changing, then fades it.
    private DispatcherTimer? _gammaGlowHold;
    private bool _gammaGlowActive;

    private void HoldGammaGlow()
    {
        _gammaGlowHold ??= CreateGammaGlowTimer();

        if (!_gammaGlowActive)
        {
            _gammaGlowActive = true;
            AnimateGammaGlow(blur: 14, opacity: 0.95, milliseconds: 80);
        }

        _gammaGlowHold.Stop();
        _gammaGlowHold.Start();
    }

    private DispatcherTimer CreateGammaGlowTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(160) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            _gammaGlowActive = false;
            AnimateGammaGlow(blur: 0, opacity: 0, milliseconds: 340);
        };
        return timer;
    }

    private void AnimateGammaGlow(double blur, double opacity, double milliseconds)
    {
        var dur = TimeSpan.FromMilliseconds(milliseconds);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        GammaValueGlow.BeginAnimation(DropShadowEffect.BlurRadiusProperty,
            new DoubleAnimation(blur, dur) { EasingFunction = ease });
        GammaValueGlow.BeginAnimation(DropShadowEffect.OpacityProperty,
            new DoubleAnimation(opacity, dur) { EasingFunction = ease });
    }

    // Lights up whichever preset button matches the current settings (if any).
    private void UpdateActivePresetHighlight()
    {
        var current = GetActiveSettings();
        string? activePreset = null;
        foreach (var (button, name) in EnumeratePresetButtons())
        {
            bool active = _appSettings.Presets.TryGetValue(name, out var preset)
                          && SettingsMatch(preset, current);
            Controls.ToggleState.SetIsActive(button, active);
            if (active) activePreset = name;
        }
        UpdateTrayTooltip(current, activePreset);
    }

    private void UpdateTrayTooltip(GammaSettings s, string? activePreset)
    {
        var label = activePreset != null ? $" · {activePreset}" : "";
        TrayIcon.ToolTipText =
            $"Gamma Adjuster — γ {s.Master.ToString("F2", CultureInfo.InvariantCulture)}{label}";
    }

    private IEnumerable<(Button Button, string Name)> EnumeratePresetButtons()
    {
        yield return (Preset1Button, SettingsStore.Preset1);
        yield return (Preset2Button, SettingsStore.Preset2);
        yield return (Preset3Button, SettingsStore.Preset3);
        yield return (Preset4Button, SettingsStore.Preset4);
    }

    private static bool SettingsMatch(GammaSettings a, GammaSettings b)
    {
        const double eps = 0.005;
        return Math.Abs(a.Master - b.Master) < eps
               && Math.Abs(a.RedOffset - b.RedOffset) < eps
               && Math.Abs(a.GreenOffset - b.GreenOffset) < eps
               && Math.Abs(a.BlueOffset - b.BlueOffset) < eps
               && Math.Abs(a.Brightness - b.Brightness) < eps
               && Math.Abs(a.Contrast - b.Contrast) < eps;
    }

    private static string FormatSigned(double v) => (v >= 0 ? "+" : "") + v.ToString("F2", CultureInfo.InvariantCulture);

    private void ScheduleSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    // ---------- Event handlers ----------

    private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating) return;
        if (MonitorComboBox.SelectedItem is DisplayMonitor m) PullSettingsIntoUi(m);
    }

    private void IdentifyButton_Click(object sender, RoutedEventArgs e)
    {
        IdentifySelectedMonitor();
    }

    private void IdentifySelectedMonitor()
    {
        // Close any currently active identify windows first to avoid accumulation
        foreach (var w in _activeIdentifyWindows)
        {
            try { w.Close(); } catch { }
        }
        _activeIdentifyWindows.Clear();

        if (MonitorComboBox.SelectedItem is not DisplayMonitor selected) return;

        if (selected.DeviceName == AllMonitorsKey)
        {
            int idx = 1;
            foreach (var m in _monitors)
            {
                IdentifyMonitor(m, idx.ToString());
                idx++;
            }
        }
        else
        {
            int idx = _monitors.FindIndex(m => m.DeviceName == selected.DeviceName) + 1;
            if (idx > 0)
            {
                IdentifyMonitor(selected, idx.ToString());
            }
        }
    }

    private void IdentifyMonitor(DisplayMonitor monitor, string number)
    {
        var rect = GammaManager.GetMonitorRect(monitor.DeviceName);
        if (rect != Rect.Empty)
        {
            // Convert physical coordinates to WPF logical coordinates based on screen DPI
            double scale = 1.0;
            try
            {
                var dpi = VisualTreeHelper.GetDpi(this);
                scale = dpi.DpiScaleX;
            }
            catch { }

            var logicalRect = new Rect(rect.X / scale, rect.Y / scale, rect.Width / scale, rect.Height / scale);

            var idWindow = new Controls.IdentifyWindow();
            _activeIdentifyWindows.Add(idWindow);
            idWindow.ShowNumber(number, logicalRect);
        }
    }

    private void GammaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating)
        {
            SetIfNotFocused(GammaValueText, e.NewValue.ToString("F2", CultureInfo.InvariantCulture));
            return;
        }
        WriteUiIntoSettings();
        var s = GetActiveSettings();
        UpdateValueLabels(s);
        CurvePreview.Update(s);
        ApplyCurrentToHardware();
        HoldGammaGlow();
        UpdateActivePresetHighlight();
        ScheduleSave();
    }

    private void ChannelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdating) return;
        WriteUiIntoSettings();
        var s = GetActiveSettings();
        UpdateValueLabels(s);
        CurvePreview.Update(s);
        ApplyCurrentToHardware();
        UpdateActivePresetHighlight();
        ScheduleSave();
    }

    private void ValueInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box) return;
        if (e.Key == Key.Enter)
        {
            CommitValueInput(box);
            Keyboard.ClearFocus();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            // Revert by re-pulling settings into the labels.
            var s = GetActiveSettings();
            box.Text = box.Tag switch
            {
                "Gamma" => s.Master.ToString("F2", CultureInfo.InvariantCulture),
                "Red" => FormatSigned(s.RedOffset),
                "Green" => FormatSigned(s.GreenOffset),
                "Blue" => FormatSigned(s.BlueOffset),
                "Brightness" => FormatSigned(s.Brightness),
                "Contrast" => FormatSigned(s.Contrast),
                _ => box.Text
            };
            Keyboard.ClearFocus();
            e.Handled = true;
        }
    }

    private void ValueInput_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box) CommitValueInput(box);
    }

    private void CommitValueInput(TextBox box)
    {
        if (box.Tag is not string field) return;
        if (!double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
        {
            // Bad input — just refresh from the model.
            UpdateValueLabels(GetActiveSettings());
            return;
        }

        Slider? slider = field switch
        {
            "Gamma" => GammaSlider,
            "Red" => RedSlider,
            "Green" => GreenSlider,
            "Blue" => BlueSlider,
            "Brightness" => BrightnessSlider,
            "Contrast" => ContrastSlider,
            _ => null
        };
        if (slider == null) return;

        v = Math.Clamp(v, slider.Minimum, slider.Maximum);
        slider.Value = v;
        // Sliders are bound to the value-changed handlers, which will refresh labels and persist.
    }

    private void StartWithWindows_Click(object sender, RoutedEventArgs e)
    {
        var enable = StartWithWindowsMenuItem.IsChecked;
        AutoStartService.SetEnabled(enable);
        // Re-sync in case the registry write failed silently.
        StartWithWindowsMenuItem.IsChecked = AutoStartService.IsEnabled();
    }

    private void MaxButton_Click(object sender, RoutedEventArgs e)
    {
        GammaSlider.Value = GammaSlider.Maximum;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        ApplySettingsToUi(GammaSettings.Default());
        ScheduleSave();
    }

    private void PresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string name) return;
        if (!_appSettings.Presets.TryGetValue(name, out var preset)) return;
        ApplySettingsToUi(preset.Clone());
        ScheduleSave();
    }

    private void PresetButton_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string name) return;
        var current = GetActiveSettings().Clone();
        var result = MessageBox.Show(
            this,
            $"Overwrite the '{name}' preset with the current settings?",
            "Save Preset",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.OK) return;

        _appSettings.Presets[name] = current;
        SettingsStore.Save(_appSettings);
        UpdateActivePresetHighlight();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }

    private void TrayIcon_LeftClick(object sender, RoutedEventArgs e)
    {
        ShowAndActivate();
    }

    private void TrayShow_Click(object sender, RoutedEventArgs e) => ShowAndActivate();

    private void TrayResetAll_Click(object sender, RoutedEventArgs e)
    {
        var def = GammaSettings.Default();
        foreach (var m in _monitors)
        {
            _appSettings.Monitors[m.DeviceName] = def.Clone();
            GammaManager.SetGamma(def, m.DeviceName);
        }
        if (MonitorComboBox.SelectedItem is DisplayMonitor sel) PullSettingsIntoUi(sel);
        ScheduleSave();
    }

    private void TrayQuit_Click(object sender, RoutedEventArgs e)
    {
        _shuttingDown = true;

        // Close OSD
        _osdWindow?.Close();

        // Close Identify overlays
        foreach (var w in _activeIdentifyWindows)
        {
            try { w.Close(); } catch { }
        }
        _activeIdentifyWindows.Clear();

        // Reset hardware to neutral so the user doesn't get stuck with a tinted screen.
        var def = GammaSettings.Default();
        foreach (var m in _monitors)
        {
            GammaManager.SetGamma(def, m.DeviceName);
        }

        // Persist any pending changes immediately.
        if (_saveTimer.IsEnabled)
        {
            _saveTimer.Stop();
            SettingsStore.Save(_appSettings);
        }

        _hotkeys?.Dispose();
        TrayIcon.Dispose();
        Application.Current.Shutdown();
    }

    private void ShowOsd(double val)
    {
        if (_osdWindow == null)
        {
            _osdWindow = new Controls.OsdWindow();
        }
        _osdWindow.ShowValue(val);
    }

    private void TrayPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item && item.Tag is string name)
        {
            if (!_appSettings.Presets.TryGetValue(name, out var preset)) return;
            ApplySettingsToUi(preset.Clone());
            ScheduleSave();
        }
    }

    public void ShowAndActivate()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }
}
