using System;
using System.Threading;
using System.Windows;
using NvidiaGammaReplica.Services;

namespace NvidiaGammaReplica;

public partial class App : Application
{
    private const string AppId = "NvidiaGammaReplica-76B900-SingleInstance";
    private static Mutex? _mutex;
    private static EventWaitHandle? _eventWaitHandle;

    public bool StartMinimized { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, AppId, out bool createdNew);
        _eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, AppId + "_Event");

        if (!createdNew)
        {
            _eventWaitHandle.Set();
            Shutdown();
            return;
        }

        ThreadPool.RegisterWaitForSingleObject(_eventWaitHandle, (state, timeout) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                if (MainWindow is MainWindow mw)
                {
                    mw.ShowAndActivate();
                }
            });
        }, null, Timeout.Infinite, false);

        base.OnStartup(e);

        StartMinimized = Array.Exists(e.Args,
            a => string.Equals(a, AutoStartService.MinimizedArg, StringComparison.OrdinalIgnoreCase));

        var window = new MainWindow();
        MainWindow = window;
        if (!StartMinimized) window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _eventWaitHandle?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
