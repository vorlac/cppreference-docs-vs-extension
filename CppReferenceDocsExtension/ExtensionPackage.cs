using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CppReferenceDocsExtension.Core.Utils;
using CppReferenceDocsExtension.Editor;
using CppReferenceDocsExtension.Editor.Commands;
using CppReferenceDocsExtension.Editor.Settings;
using CppReferenceDocsExtension.Editor.ToolWindow;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace CppReferenceDocsExtension
{
    using Constants = Core.Constants;

    [
        // yo dawg, I heard you like dependency injection... :-|
        Guid(guid: PackageGuidString)] [
        PackageRegistration(
            UseManagedResourcesOnly = true,
            AllowsBackgroundLoading = true
        )] [
        ProvideAutoLoad(
            cmdUiContextGuid: UIContextGuids80.SolutionExists,
            flags: PackageAutoLoadFlags.BackgroundLoad
        )] [
        InstalledProductRegistration(
            productName: "#110",
            productDetails: "#112",
            productId: "1.0",
            IconResourceID = 400
        )] [
        ProvideMenuResource(
            resourceID: "Menus.ctmenu",
            version: 1
        )] [
        ProvideToolWindow(
            toolType: typeof(DocsPanelBrowserWindow),
            DockedWidth = 600,
            Window = "DocumentWell",
            Orientation = ToolWindowOrientation.Right
        )] [
        ProvideOptionPage(
            pageType: typeof(DialogPageProvider.General),
            categoryName: Constants.ExtensionName,
            pageName: "General",
            categoryResourceID: 0,
            pageNameResourceID: 0,
            supportsAutomation: true
        )] [
        ProvideOptionPage(
            pageType: typeof(DialogPageProvider.Other),
            categoryName: Constants.ExtensionName,
            pageName: "Other",
            categoryResourceID: 0,
            pageNameResourceID: 0,
            supportsAutomation: true
        )
    ]
    public sealed class ExtensionPackage : AsyncPackage
    {
        private readonly ILogger log = Log.Logger;
        private const string PackageGuidString = "DEADBEEF-FEEE-FEEE-CDCD-000000000000";

        protected override async Task InitializeAsync(
            CancellationToken token, IProgress<ServiceProgressData> progress) {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();
            EditorUtils.Initialize(this);

            await OpenDocsToolWindowCommand.InitializeAsync(this);
            this.InitializeLogging();
            this.log.Debug(messageTemplate: $"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");
        }

        private async void InitializeLogging() {
            const string format = "{Timestamp:HH:mm:ss.fff} [{Level}] {Pid} {Message}{NewLine}{Exception}";
            IVsOutputWindow outputWindow = this.GetService<SVsOutputWindow, IVsOutputWindow>();

            try {
                GeneralOptions settings = await BaseOptionModel<GeneralOptions>.GetLiveInstanceAsync();
                LoggingLevelSwitch levelSwitch = new() { MinimumLevel = settings.MinimumLoggingLevel };
                OutputPaneEventSink sink = new(outputWindow: outputWindow, outputTemplate: format);
                Log.Logger = new LoggerConfiguration().MinimumLevel.ControlledBy(levelSwitch: levelSwitch)
                                                      .WriteTo.Sink(logEventSink: sink, levelSwitch: levelSwitch)
                                                      .WriteTo.Trace(outputTemplate: format)
                                                      .CreateLogger();
                Log.Logger.Verbose(messageTemplate: "Logging initialization complete");
            }
            catch (Exception e) {
                Log.Logger?.Error(
                    e,
                    $"{nameof(ExtensionPackage)}.{nameof(this.InitializeLogging)}(): "
                  + $"Could not retrieve Logging Configuration"
                );
            }
        }
    }
}
