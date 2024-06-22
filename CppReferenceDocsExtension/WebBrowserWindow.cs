using Constants = CppReferenceDocsExtension.Core.Constants;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using EnvDTE;

namespace CppReferenceDocsExtension {
    [Guid("8ab2cef3-7c52-4e4a-8d07-1dd7f9f90a1c")]
    public sealed class WebBrowserWindow : ToolWindowPane, IVsWindowFrameNotify2 {
        private readonly ILogger log = Log.Logger;
        private readonly WebBrowserWindowControl control;
        private readonly WebView2 webView;
        private readonly DTE dte;

        public WebBrowserWindow() : base(null) {
            this.Caption = Constants.ExtensionName;
            this.control = new WebBrowserWindowControl { SetTitleAction = x => this.Caption = x };

            this.Content = this.control;
            this.webView = this.control.webView;

            ThreadHelper.ThrowIfNotOnUIThread();

            // Retrieve DTE and listen to "Visual Studio Shutdown" event
            this.dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
            this.dte.Events.DTEEvents.OnBeginShutdown += this.OnVisualStudioShutDown;
        }

        public int OnClose(ref uint pgrfSaveOptions) {
            this.log.Debug($"{nameof(WebBrowserWindow)}: OnClose({pgrfSaveOptions})");

            return VSConstants.S_OK;
        }

        public override void OnToolWindowCreated() {
            base.OnToolWindowCreated();
            this.log.Debug($"{nameof(WebBrowserWindow)}: OnToolWindowCreated()");
        }

        protected override void Initialize() {
            this.log.Verbose($"Initializing {nameof(WebBrowserWindow)}");
            base.Initialize();
            this.control.Services = this;
            this.log.Verbose($"Initialized {nameof(WebBrowserWindow)}");
        }

        private void OnVisualStudioShutDown() {
            this.log.Debug($"{nameof(WebBrowserWindow)}: Visual Studio is closing");
            this.CleanupControl();
        }

        private void CleanupControl() {
            this.log.Debug("Cleaning up the Web Browser control instance");
            this.webView?.Dispose();
        }
    }
}
