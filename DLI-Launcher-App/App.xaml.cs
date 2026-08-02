using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace DLI_Launcher_App;

public partial class App : Application
{
    private const string MutexName = "DLI_Launcher_SingleInstance";
    private Mutex? _mutex;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    private const int SW_RESTORE = 9;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out var createdNew);

        if (!createdNew)
        {
            var hWnd = FindWindow(null, "DLI Launcher");
            if (hWnd == IntPtr.Zero)
            {
                var existing = Process.GetProcessesByName("DLI-Launcher")
                    .FirstOrDefault(p => p.Id != Environment.ProcessId);
                if (existing != null)
                    hWnd = existing.MainWindowHandle;
            }

            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
            }

            _mutex.Dispose();
            Shutdown();
            return;
        }

        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.Dispose();
        base.OnExit(e);
    }
}

