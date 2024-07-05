using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace CppReferenceDocsExtension.Editor.ToolWindow
{
    internal sealed class DocsPanelBrowserCommand
    {
        private static class Constants
        {
            private const int CommandID = 0x0100;
            private const string CommandSetID = "DEADBEEF-FEEE-FEEE-CDCD-000000000002";

            public static CommandID MenuCommandID { get; }
                = new CommandID(new Guid(CommandSetID), CommandID);
        }

        private readonly DTE dte;
        private readonly ILogger log = Log.Logger;
        private readonly AsyncPackage package;

        private DocsPanelBrowserCommand(AsyncPackage asyncPackage, DTE dteInstance, OleMenuCommandService commandService) {
            if (commandService == null)
                throw new ArgumentNullException(nameof(commandService));
            if (dteInstance == null)
                throw new ArgumentNullException(nameof(dteInstance));
            if (asyncPackage == null)
                throw new ArgumentNullException(nameof(asyncPackage));

            this.package = asyncPackage;
            this.dte = dteInstance;

            MenuCommand menuItem = new MenuCommand(this.Execute, Constants.MenuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static DocsPanelBrowserCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package) {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            OleMenuCommandService commandService = await package.GetServiceAsync(
                typeof(IMenuCommandService)
            ) as OleMenuCommandService;

            DTE dte = (DTE)await package.GetServiceAsync(typeof(DTE));
            Instance = new DocsPanelBrowserCommand(package, dte, commandService);
        }

        private void Execute(object sender, EventArgs e) {
            _ = this.package.JoinableTaskFactory.RunAsync(
                async delegate {
                    DocsPanelBrowserWindow window = await this.package.ShowToolWindowAsync(
                        typeof(DocsPanelBrowserWindow),
                        0,
                        true,
                        this.package.DisposalToken
                    ) as DocsPanelBrowserWindow;

                    if (window?.Frame == null)
                        this.log.Error($"{nameof(DocsPanelBrowserCommand)}: Cannot create tool window");
                }
            );
        }
    }
}
