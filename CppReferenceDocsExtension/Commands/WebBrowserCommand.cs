using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace CppReferenceDocsExtension.Commands
{
    internal sealed class WebBrowserCommand
    {
        private const int CommandId = 0x0100;
        public const int WebBrowserWindowNavigateId = 0x101;
        public const int WebBrowserWindowToolbarID = 0x1000;

        private static readonly Guid s_commandSet = new Guid("48c3fadd-683b-4577-8583-c9817b4e5a50");

        private readonly ILogger _log = Log.Logger;
        private readonly AsyncPackage _package;
        private readonly DTE _dte;

        private WebBrowserCommand(AsyncPackage asyncPackage, DTE dteInstance, OleMenuCommandService commandService)
        {
            _package = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            _dte = dteInstance ?? throw new ArgumentNullException(nameof(dteInstance));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            CommandID menuCommandID = new CommandID(s_commandSet, CommandId);
            MenuCommand menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static WebBrowserCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            DTE dte = (DTE)await package.GetServiceAsync(typeof(DTE));
            Instance = new WebBrowserCommand(package, dte, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _ = _package.JoinableTaskFactory.RunAsync(async delegate
            {
                WebBrowserWindow window =
                    await _package.ShowToolWindowAsync(typeof(WebBrowserWindow), 0, true, _package.DisposalToken)
                        as WebBrowserWindow;
                if (window?.Frame == null)
                    _log.Error($"{nameof(WebBrowserCommand)}: Cannot create tool window");
            });
        }
    }
}
