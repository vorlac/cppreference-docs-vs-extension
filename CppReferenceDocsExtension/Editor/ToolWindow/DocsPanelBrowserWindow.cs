using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using Constants = CppReferenceDocsExtension.Core.Constants;

namespace CppReferenceDocsExtension.Editor.ToolWindow
{
    [Guid("DEADBEEF-FEEE-FEEE-CDCD-000000000001")]
    public sealed class DocsPanelBrowserWindow : ToolWindowPane, IVsWindowFrameNotify2
    {
        private readonly DocsPanelBrowserWindowControl control;
        private readonly WebView2 webView;
        //private readonly DTE dte;

        private readonly ILogger log = Log.Logger;

        public DocsPanelBrowserWindow() : base(null) {
            this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");

            this.Caption = Constants.ExtensionName;
            this.control = new(x => this.Caption = x);

            this.Content = this.control;
            this.webView = this.control.webView;

            ThreadHelper.ThrowIfNotOnUIThread();

            // Retrieve DTE and listen to "Visual Studio Shutdown" event
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
            dte.Events.DTEEvents.OnBeginShutdown += this.OnVisualStudioShutDown;
        }

        public int OnClose(ref uint pgrfSaveOptions) {
            this.log.Debug($"{nameof(DocsPanelBrowserWindow)}: OnClose({pgrfSaveOptions})");
            return VSConstants.S_OK;
        }

        public override void OnToolWindowCreated() {
            base.OnToolWindowCreated();
            this.log.Debug($"{nameof(DocsPanelBrowserWindow)}: OnToolWindowCreated()");
        }

        protected override void Initialize() {
            this.log.Debug($"Initializing {nameof(DocsPanelBrowserWindow)}");
            base.Initialize();
            this.control.Services = this;
            this.log.Debug($"Initialized {nameof(DocsPanelBrowserWindow)}");
        }

        private void OnVisualStudioShutDown() {
            this.log.Debug($"{nameof(DocsPanelBrowserWindow)}: Visual Studio is closing");
            this.CleanupControl();
        }

        private void CleanupControl() {
            this.log.Debug("Cleaning up the Web Browser control instance");
            this.webView?.Dispose();
        }
    }
}
