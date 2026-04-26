using System;
using System.Windows;
using NvidiaGammaReplica.Services;

namespace NvidiaGammaReplica;

public partial class App : Application
{
    public bool StartMinimized { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        StartMinimized = Array.Exists(e.Args,
            a => string.Equals(a, AutoStartService.MinimizedArg, StringComparison.OrdinalIgnoreCase));

        var window = new MainWindow();
        if (!StartMinimized) window.Show();
    }
}
