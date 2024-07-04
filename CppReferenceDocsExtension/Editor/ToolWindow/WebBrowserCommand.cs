using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace CppReferenceDocsExtension.Editor.ToolWindow {
    internal sealed class WebBrowserCommand {
        private const int CommandId = 0x0100;
        // public const int WebBrowserWindowNavigateId = 0x101;
        // public const int WebBrowserWindowToolbarID = 0x1000;
        private readonly DTE dte;

        private static readonly Guid SCommandSet = new Guid("48c3fadd-683b-4577-8583-c9817b4e5a50");
        private readonly ILogger log = Log.Logger;
        private readonly AsyncPackage package;

        public static WebBrowserCommand Instance { get; private set; }

        private WebBrowserCommand(AsyncPackage asyncPackage, DTE dteInstance, OleMenuCommandService commandService) {
            this.package = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            this.dte = dteInstance ?? throw new ArgumentNullException(nameof(dteInstance));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            var menuCommandID = new CommandID(SCommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package) {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var commandService = await package.GetServiceAsync(
                typeof(IMenuCommandService)
            ) as OleMenuCommandService;

            var dte = (DTE)await package.GetServiceAsync(typeof(DTE));
            Instance = new WebBrowserCommand(package, dte, commandService);
        }

        private void Execute(object sender, EventArgs e) {
            _ = this.package.JoinableTaskFactory.RunAsync(
                async delegate {
                    var window = await this.package.ShowToolWindowAsync(
                        typeof(WebBrowserWindow),
                        0,
                        true,
                        this.package.DisposalToken
                    ) as WebBrowserWindow;

                    if (window?.Frame == null)
                        this.log.Error($"{nameof(WebBrowserCommand)}: Cannot create tool window");
                }
            );
        }
    }
}
