namespace NvidiaGammaReplica.Models;

public sealed class GammaSettings
{
    public double Master { get; set; } = 1.0;
    public double RedOffset { get; set; }
    public double GreenOffset { get; set; }
    public double BlueOffset { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; }

    public bool IsDefault =>
        System.Math.Abs(Master - 1.0) < 0.001 &&
        System.Math.Abs(RedOffset) < 0.001 &&
        System.Math.Abs(GreenOffset) < 0.001 &&
        System.Math.Abs(BlueOffset) < 0.001 &&
        System.Math.Abs(Brightness) < 0.001 &&
        System.Math.Abs(Contrast) < 0.001;

    public GammaSettings Clone() => (GammaSettings)MemberwiseClone();

    public static GammaSettings Default() => new();
}
