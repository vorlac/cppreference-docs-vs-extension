#region

    using System;
    using System.ComponentModel.Design;
    using System.Threading.Tasks;
    using CppReferenceDocsExtension.Editor.ToolWindow;
    using EnvDTE;
    using Microsoft.VisualStudio.Shell;
    using Serilog;

#endregion

    namespace CppReferenceDocsExtension.Editor.Commands
    {
        internal sealed class OpenDocsToolWindowCommand
        {
            private static class Constants
            {
                private const int CommandID = 0x0100;
                private const string CommandSetID = "DEADBEEF-FEEE-FEEE-CDCD-000000000002";

                public static CommandID MenuCommandID { get; } = new(
                    menuGroup: new(CommandSetID),
                    commandID: CommandID
                );
            }

            private AsyncPackage Package { get; }
            private readonly ILogger log = Log.Logger;

            public static DTE DTEInstance { get; private set; }
            public static OpenDocsToolWindowCommand Instance { get; private set; }

            private OpenDocsToolWindowCommand(AsyncPackage package, DTE instance, OleMenuCommandService service) {
                if (service is null)
                    throw new ArgumentNullException(nameof(service));
                if (instance is null)
                    throw new ArgumentNullException(nameof(instance));
                if (package is null)
                    throw new ArgumentNullException(nameof(package));

                this.Package = package;
                OpenDocsToolWindowCommand.DTEInstance = instance;

                MenuCommand menuItem = new(
                    handler: this.Execute,
                    command: Constants.MenuCommandID
                );

                service.AddCommand(menuItem);
            }

            public static async Task InitializeAsync(AsyncPackage package) {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
                OleMenuCommandService commandService = await package.GetServiceAsync(
                    typeof(IMenuCommandService)
                ) as OleMenuCommandService;

                DTE dteInstance = await package.GetServiceAsync(
                    typeof(DTE)
                ) as DTE;

                OpenDocsToolWindowCommand.Instance = new(
                    package: package,
                    instance: dteInstance,
                    service: commandService
                );
            }

            private void Execute(object sender, EventArgs e) {
                _ = this.Package.JoinableTaskFactory.RunAsync(
                    async delegate {
                        DocsPanelBrowserWindow window = await this.Package.ShowToolWindowAsync(
                            toolWindowType: typeof(DocsPanelBrowserWindow),
                            id: 0,
                            create: true,
                            cancellationToken: this.Package.DisposalToken
                        ) as DocsPanelBrowserWindow;

                        if (window is null || window.Frame is null)
                            this.log.Error($"{this.GetType().FullName}: Cannot create tool window");
                    }
                );
            }
        }
    }
