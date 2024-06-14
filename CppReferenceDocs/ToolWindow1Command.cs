using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Core;
using Task = System.Threading.Tasks.Task;

namespace CppReferenceDocs
{
    internal sealed class ToolWindow1Command
    {
        private const int CommandId = 0x0100;
        private static readonly Guid CommandSet = new Guid("69e083f1-9cee-40c2-ac81-6459cc89a19e");
        private readonly AsyncPackage _package;

        private ToolWindow1Command(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static ToolWindow1Command Instance { get; private set; }

        private IAsyncServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Switch to the main thread - the call to AddCommand in
            // ToolWindow1Command's constructor requires the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
            var cmdSrv = await package.GetServiceAsync(typeof(IMenuCommandService));
            Instance = new ToolWindow1Command(package, cmdSrv as OleMenuCommandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ToolWindowPane window = _package.FindToolWindow(typeof(ToolWindow1), 0, true);
            if (window?.Frame == null)
                throw new NotSupportedException("Cannot create tool window");

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
