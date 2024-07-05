using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CppReferenceDocsExtension.Core.Utils;
using CppReferenceDocsExtension.Editor.Settings;
using CppReferenceDocsExtension.Editor.ToolWindow;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Core;
using Serilog.Events;
//
using Constants = CppReferenceDocsExtension.Core.Constants;

namespace CppReferenceDocsExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuidString)]
    [ProvideToolWindow(
        typeof(DocsPanelBrowserWindow),
        DockedWidth = 600,
        Window = "DocumentWell",
        Orientation = ToolWindowOrientation.Right
    )]
    [ProvideOptionPage(typeof(DialogPageProvider.General), Constants.ExtensionName, "General", 0, 0, true)]
    [ProvideOptionPage(typeof(DialogPageProvider.Other), Constants.ExtensionName, "Other", 0, 0, true)]
    public sealed class CppReferenceDocsExtensionPackage : AsyncPackage
    {
        private const string PackageGuidString = "DEADBEEF-FEEE-FEEE-CDCD-000000000000";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress) {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();
            await DocsPanelBrowserCommand.InitializeAsync(this);
            this.InitializeLogging();
        }

        private async void InitializeLogging() {
            const string format = "{Timestamp:HH:mm:ss.fff} [{Level}] {Pid} {Message}{NewLine}{Exception}";
            IVsOutputWindow outputWindow = this.GetService<SVsOutputWindow, IVsOutputWindow>();
            LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Verbose };
            Exception exception = null;
            string message = "";

            try {
                GeneralOptions settings = await GeneralOptions.GetLiveInstanceAsync();
                levelSwitch.MinimumLevel = settings.MinimumLoggingLevel;
            }
            catch (Exception ex) {
                exception = ex;
                message = $"{nameof(CppReferenceDocsExtensionPackage)}.{nameof(this.InitializeLogging)}(): "
                        + $"Could not retrieve Logging Configuration";
            }

            OutputPaneEventSink sink = new OutputPaneEventSink(outputWindow, format);
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
