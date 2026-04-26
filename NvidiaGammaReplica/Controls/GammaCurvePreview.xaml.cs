using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NvidiaGammaReplica.Models;

namespace NvidiaGammaReplica.Controls;

public partial class GammaCurvePreview : UserControl
{
    private GammaSettings _settings = GammaSettings.Default();

    public GammaCurvePreview()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
        Loaded += (_, _) => Redraw();
    }

    public void Update(GammaSettings settings)
    {
        _settings = settings.Clone();
        Redraw();
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
