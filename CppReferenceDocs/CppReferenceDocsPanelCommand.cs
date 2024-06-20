using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace CppReferenceDocsExtension
{
    internal sealed class CppReferenceDocsPanelCommand
    {
        public const int CommandId = 0x0100;
        public const int CppReferenceDocsPanelNavigateId = 0x101;
        public const int CppReferenceDocsPanelToolbarID = 0x1000;

        public static readonly Guid CommandSet = new Guid("48c3fadd-683b-4577-8583-c9817b4e5a50");

        private readonly ILogger _log = Log.Logger;
        private readonly AsyncPackage _package;
        private readonly DTE _dte;

        private CppReferenceDocsPanelCommand(AsyncPackage asyncPackage, DTE dteInstance,
            OleMenuCommandService commandService)
        {
            _package = asyncPackage ?? throw new ArgumentNullException(nameof(asyncPackage));
            _dte = dteInstance ?? throw new ArgumentNullException(nameof(dteInstance));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            CommandID menuCommandID = new CommandID(CommandSet, CommandId);
            MenuCommand menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static CppReferenceDocsPanelCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService =
                await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            DTE dte = await package.GetServiceAsync(typeof(DTE)) as DTE;
            Instance = new CppReferenceDocsPanelCommand(package, dte, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _ = _package.JoinableTaskFactory.RunAsync(async delegate
            {
                CppReferenceDocsPanel window =
                    await _package.ShowToolWindowAsync(typeof(CppReferenceDocsPanel), 0, true, _package.DisposalToken)
                        as CppReferenceDocsPanel;

                if (window?.Frame == null)
                    _log.Error($"{nameof(CppReferenceDocsPanelCommand)}: Cannot create tool window");
            });
        }
    }
}
