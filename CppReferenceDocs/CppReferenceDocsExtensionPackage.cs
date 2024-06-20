using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CppReferenceDocsExtension.Settings;
using CppReferenceDocsExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CppReferenceDocsExtension
{
    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(CppReferenceDocsPanel))]
    [ProvideOptionPage(typeof(CppReferenceDocsPanelOptionsPage), Constants.ExtensionName, "General", 0, 0, true)]
    [ProvideProfile(typeof(CppReferenceDocsPanelOptionsPage), Constants.ExtensionName, "General", 0, 0, true)]
    public sealed class CppReferenceDocsExtensionPackage : AsyncPackage
    {
        public const string PackageGuidString = "1aa34956-275f-48c6-889b-a8834db18c23";

        protected override async Task InitializeAsync(CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            InitializeLogging();
            await CppReferenceDocsPanelCommand.InitializeAsync(this);
        }

        private void InitializeLogging()
        {
            const string format = "{Timestamp:HH:mm:ss.fff} [{Level}] {Pid} {Message}{NewLine}{Exception}";
            IVsOutputWindow outputWindow = this.GetService<SVsOutputWindow, IVsOutputWindow>();

            LoggingLevelSwitch levelSwitch = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Verbose };
            (Exception exception, string message) = (null, "");
            try
            {
                IDocsBrowserSettings settings = this.GetService<IDocsBrowserSettings>();
                levelSwitch.MinimumLevel = settings.MinimumLogLevel;
                settings.PropertyChanged += (s, e) => levelSwitch.MinimumLevel = settings.MinimumLogLevel;
            }
            catch (Exception ex)
            {
                exception = ex;
                message =
                    $"{nameof(CppReferenceDocsExtensionPackage)}.{nameof(InitializeLogging)}(): Could not retrieve Logging Configuration";
            }

            OutputPaneEventSink sink = new OutputPaneEventSink(outputWindow, outputTemplate: format);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
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
