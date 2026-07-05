using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NvidiaGammaReplica.Models;

namespace NvidiaGammaReplica.Controls;

public partial class GammaCurvePreview : UserControl
{
    private GammaSettings _settings = GammaSettings.Default();

    // Keeps the flash lit while values keep changing, then fades once they settle.
    private readonly DispatcherTimer _flashHold;
    private bool _flashActive;

    public GammaCurvePreview()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
        Loaded += (_, _) => Redraw();

        _flashHold = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(140) };
        _flashHold.Tick += (_, _) =>
        {
            _flashHold.Stop();
            FadeFlashOut();
        };
    }

    public void Update(GammaSettings settings)
    {
        _settings = settings.Clone();
        Redraw();
        Flash();
    }

    /// <summary>
    /// Lights the accent border immediately, and holds it lit for as long as
    /// updates keep arriving (e.g. during a slider drag), fading shortly after.
    /// </summary>
    private void Flash()
    {
        if (!IsLoaded) return;

        if (!_flashActive)
        {
            _flashActive = true;
            var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(60));
            FlashBorder.BeginAnimation(OpacityProperty, fadeIn);
        }

        _flashHold.Stop();
        _flashHold.Start();
    }

    private void FadeFlashOut()
    {
        _flashActive = false;
        var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(320))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        FlashBorder.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void Redraw()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        var ramp = GammaManager.BuildRamp(_settings);
        DrawGradient(ramp);
        DrawCurves(ramp);
    }

    private void DrawGradient(GammaManager.RAMP ramp)
    {
        var stops = new GradientStopCollection();
        for (int i = 0; i <= 16; i++)
        {
            int idx = i * 255 / 16;
            byte r = (byte)(ramp.Red[idx] >> 8);
            byte g = (byte)(ramp.Green[idx] >> 8);
            byte b = (byte)(ramp.Blue[idx] >> 8);
            stops.Add(new GradientStop(Color.FromRgb(r, g, b), i / 16.0));
        }
        GradientStrip.Fill = new LinearGradientBrush(stops, new Point(0, 0), new Point(1, 0));
    }

    private void DrawCurves(GammaManager.RAMP ramp)
    {
        CurveCanvas.Children.Clear();
        double w = CurveCanvas.ActualWidth;
        double h = CurveCanvas.ActualHeight;
        if (w <= 1 || h <= 1) return;

        AddCurve(ramp.Red, Brushes.IndianRed, w, h);
        AddCurve(ramp.Green, Brushes.LimeGreen, w, h);
        AddCurve(ramp.Blue, Brushes.CornflowerBlue, w, h);
    }

    private void AddCurve(ushort[] channel, Brush stroke, double w, double h)
    {
        var pts = new PointCollection();
        for (int i = 0; i < 256; i++)
        {
            double x = i / 255.0 * w;
            double y = h - channel[i] / 65535.0 * h;
            pts.Add(new Point(x, y));
        }
        var poly = new Polyline
        {
            Stroke = stroke,
            StrokeThickness = 1.2,
            Points = pts,
            Opacity = 0.85
        };
        CurveCanvas.Children.Add(poly);
    }
}
