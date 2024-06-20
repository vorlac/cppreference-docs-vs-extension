using System;
using System.Windows;
using NLog;

namespace WebViewBrowserPanel
{
    public partial class App : Application
    {
        private static readonly Logger s_log = LogManager.GetCurrentClassLogger();

        static App()
        {
            s_log.Info("Starting Application...");
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string details;
            if (e.ExceptionObject is Exception exception)
            {
                details = exception.Message;
                s_log.Fatal(exception, $"Fatal Error: {details}");
            }
            else
            {
                details = e.ExceptionObject?.ToString() ?? "<null>";
                s_log.Fatal($"Fatal Error: {details}");
            }

            _ = MessageBox.Show($"A fatal error occurred:\r\n\r\n{details}", "Fatal Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        public App() => Exit += (s, e) => s_log.Info("Exiting");
    }
}
