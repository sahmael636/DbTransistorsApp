using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DbTransistorsApp.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            RegisterGlobalExceptionHandlers();
        }

        private void RegisterGlobalExceptionHandlers()
        {
            // AppDomain unhandled
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                LogException(ex, "AppDomain.CurrentDomain.UnhandledException");
            };

            // TaskScheduler unobserved
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };

            // UI thread unhandled (WinUI)
            try
            {
                this.UnhandledException += (s, e) =>
                {
                    LogException(e.Exception, "Microsoft.UI.Xaml.Application.UnhandledException");
                    // do not mark handled here; allow existing behavior
                };
            }
            catch
            {
                // ignore if event not available
            }
        }

        private void LogException(Exception? ex, string source)
        {
            try
            {
                var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DbTransistorsApp", "logs");
                Directory.CreateDirectory(logDir);
                var logFile = Path.Combine(logDir, "errors.log");
                var sb = new StringBuilder();
                sb.AppendLine("=== " + DateTime.UtcNow.ToString("o") + " ===");
                sb.AppendLine("Source: " + source);
                // Additional environment info
                sb.AppendLine($"Machine: {Environment.MachineName}");
                sb.AppendLine($"OS: {Environment.OSVersion}");
                sb.AppendLine($"Process: {Process.GetCurrentProcess().ProcessName} (pid {Process.GetCurrentProcess().Id})");
                var entryVer = System.Reflection.Assembly.GetEntryAssembly()?.GetName()?.Version?.ToString() ?? "unknown";
                sb.AppendLine($"AppVersion: {entryVer}");

                if (ex != null)
                {
                    sb.AppendLine(ex.ToString());
                }
                else
                {
                    sb.AppendLine("Exception object was null.");
                }
                sb.AppendLine();
                File.AppendAllText(logFile, sb.ToString());
                Debug.WriteLine(sb.ToString());
                // Also attempt to show a user-friendly alert on the UI
                try
                {
                    ShowExceptionToUser(ex);
                }
                catch
                {
                    // ignore any failures showing the alert
                }
            }
            catch
            {
                // best-effort logging; swallow exceptions to avoid recursion
            }
        }

        private void ShowExceptionToUser(Exception? ex)
        {
            // Ensure we run on the main thread and that a page is available
            try
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var page = Microsoft.Maui.Controls.Application.Current?.MainPage;
                        var message = ex?.Message ?? "Se produjo un error inesperado.";
                        // Shorten message if too long
                        if (message.Length > 1000) message = message.Substring(0, 1000) + "...";
                        if (page != null)
                        {
                            await page.DisplayAlert("Error inesperado", message + "\n\nSe ha registrado en logs locales.", "OK");
                        }
                    }
                    catch
                    {
                        // ignore UI failures
                    }
                });
            }
            catch
            {
                // ignore
            }
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }

}
