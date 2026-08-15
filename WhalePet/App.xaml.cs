using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace WhalePet
{
    public partial class App : System.Windows.Application
    {
        private static readonly string LogDir = Path.Combine(Path.GetTempPath(), "whalepet-logs");
        private static readonly string CrashLog = Path.Combine(LogDir, "crash.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try { Directory.CreateDirectory(LogDir); } catch { }
            try { WhaleTheme.Load(); } catch (Exception ex) { Log("ThemeLoad", ex); }
            DispatcherUnhandledException += (s, a) =>
            {
                Log("DispatcherUnhandledException", a.Exception);
                a.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, a) =>
            {
                Log("AppDomain.UnhandledException", a.ExceptionObject as Exception);
            };
            TaskScheduler.UnobservedTaskException += (s, a) =>
            {
                Log("UnobservedTaskException", a.Exception);
                a.SetObserved();
            };
        }

        private static void Log(string kind, Exception ex)
        {
            try
            {
                File.AppendAllText(CrashLog,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {kind}\n{ex}\n\n");
            }
            catch { }
        }
    }
}
