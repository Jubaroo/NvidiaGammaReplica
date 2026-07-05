using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NvidiaGammaReplica.Controls;

public partial class IdentifyWindow : Window
{
    private readonly DispatcherTimer _timer;

    public IdentifyWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
        _timer.Tick += (s, e) => FadeOut();
    }

    public void ShowNumber(string number, Rect screenBounds)
    {
        NumberText.Text = number;

        // Position in the center of the target screen
        Left = screenBounds.Left + (screenBounds.Width - Width) / 2;
        Top = screenBounds.Top + (screenBounds.Height - Height) / 2;

        _timer.Stop();
        Opacity = 1.0;
        Show();
        _timer.Start();
    }

    private void FadeOut()
    {
        _timer.Stop();
        var anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
        anim.Completed += (s, e) => Close(); // Close and dispose the window
        BeginAnimation(OpacityProperty, anim);
    }
}
