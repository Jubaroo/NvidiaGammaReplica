using System;
using System.Globalization;
using System.Windows.Data;

namespace NvidiaGammaReplica.Controls;

public sealed class TickPositionConverter : IValueConverter
{
    public const double GammaMin = 0.3;
    public const double GammaMax = 2.8;
    public const double ThumbWidth = 16.0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double width || width <= 0) return 0.0;
        if (parameter is not string s || !double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var tick))
            return 0.0;
        var pct = (tick - GammaMin) / (GammaMax - GammaMin);
        var usable = Math.Max(0.0, width - ThumbWidth);
        return ThumbWidth / 2.0 + pct * usable - 0.5;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
