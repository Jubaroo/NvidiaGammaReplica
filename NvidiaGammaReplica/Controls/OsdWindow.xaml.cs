using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NvidiaGammaReplica.Controls;

public partial class OsdWindow : Window
{
    private readonly DispatcherTimer _fadeTimer;

    public OsdWindow()
    {
        InitializeComponent();
        
        _fadeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
        _fadeTimer.Tick += (_, _) => FadeOut();
    }

    public void ShowValue(double value)
    {
        OsdValueText.Text = value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        OsdProgress.Value = value;

        // Position at bottom center of primary screen
        double screenW = SystemParameters.PrimaryScreenWidth;
        double screenH = SystemParameters.PrimaryScreenHeight;
        Left = (screenW - Width) / 2;
        Top = screenH - Height - 100;

        _fadeTimer.Stop();
        
        Opacity = 1.0;
        Show();

        _fadeTimer.Start();
    }

    private void FadeOut()
    {
        _fadeTimer.Stop();
        var anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
        anim.Completed += (s, e) => Hide();
        BeginAnimation(OpacityProperty, anim);
    }
}
