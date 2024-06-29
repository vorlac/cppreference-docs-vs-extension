using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CppReferenceDocsExtension.Commands;
using CppReferenceDocsExtension.Core.Utils;
using CppReferenceDocsExtension.Settings;
using CppReferenceDocsExtension.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Constants = CppReferenceDocsExtension.Core.Constants;

namespace CppReferenceDocsExtension {
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)] [Guid(PackageGuidString)]
    [ProvideToolWindow(
        typeof(WebBrowserWindow),
        Style = VsDockStyle.Tabbed,
        DockedWidth = 600,
        Window = "DocumentWell",
        Orientation = ToolWindowOrientation.Right
    )]
    [ProvideOptionPage(typeof(WebBrowserOptionsPage), Constants.ExtensionName, "General", 0, 0, true)]
    [ProvideProfile(typeof(WebBrowserOptionsPage), Constants.ExtensionName, "General", 0, 0, true)]
    public sealed class WebBrowserExtensionPackage : AsyncPackage {
        private const string PackageGuidString = "1ba34956-275f-48c6-889b-a8834db18c23";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            await base.InitializeAsync(cancellationToken, progress);
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            this.InitializeLogging();

            await WebBrowserCommand.InitializeAsync(this);
        }

        private void InitializeLogging() {
            const string format = "{Timestamp:HH:mm:ss.fff} [{Level}] {Pid} {Message}{NewLine}{Exception}";
            IVsOutputWindow outputWindow = this.GetService<SVsOutputWindow, IVsOutputWindow>();
            var levelSwitch = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Verbose };
            Exception exception = null;
            string message = "";

            try {
                var settings = this.GetService<IWebBrowserSettings>();
                levelSwitch.MinimumLevel = settings.MinimumLogLevel;
                settings.PropertyChanged += (s, e) =>
                    levelSwitch.MinimumLevel = settings.MinimumLogLevel;
            }
            catch (Exception ex) {
                exception = ex;
                message = $"{nameof(WebBrowserExtensionPackage)}.{nameof(this.InitializeLogging)}(): "
                        + $"Could not retrieve Logging Configuration";
            }

            var sink = new OutputPaneEventSink(outputWindow, format);
            Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch)
                                                  .WriteTo.Sink(sink, levelSwitch: levelSwitch)
                                                  .WriteTo.Trace(outputTemplate: format)
                                                  .CreateLogger();

            if (exception != null)
                Log.Logger.Error(exception, message ?? $"{exception.Message}");
            else
                Log.Logger.Verbose("Logging initialization complete");
        }
    }
}
