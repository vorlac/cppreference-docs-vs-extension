using Constants = CppReferenceDocsExtension.Core.Constants;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using EnvDTE;

namespace CppReferenceDocsExtension
{
    [Guid("8ab2cef3-7c52-4e4a-8d07-1dd7f9f90a1c")]
    public sealed class WebBrowserWindow : ToolWindowPane, IVsWindowFrameNotify2
    {
        private readonly ILogger _log = Log.Logger;
        private readonly WebBrowserWindowControl _control;
        private readonly WebView2 _webView;
        private readonly DTE _dte;

        public WebBrowserWindow() : base(null)
        {
            Caption = Constants.ExtensionName;
            _control = new WebBrowserWindowControl { SetTitleAction = x => Caption = x };

            Content = _control;
            _webView = _control.webView;

            ThreadHelper.ThrowIfNotOnUIThread();

            // Retrieve DTE and listen to "Visual Studio Shutdown" event
            _dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
            _dte.Events.DTEEvents.OnBeginShutdown += OnVisualStudioShutDown;
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            _log.Debug($"{nameof(WebBrowserWindow)}: OnClose({pgrfSaveOptions})");
            return VSConstants.S_OK;
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            _log.Debug($"{nameof(WebBrowserWindow)}: OnToolWindowCreated()");
        }

        protected override void Initialize()
        {
            _log.Verbose($"Initializing {nameof(WebBrowserWindow)}");
            base.Initialize();
            _control.Services = this;
            _log.Verbose($"Initialized {nameof(WebBrowserWindow)}");
        }

        private void OnVisualStudioShutDown()
        {
            _log.Debug($"{nameof(WebBrowserWindow)}: Visual Studio is closing");
            CleanupControl();
        }

        private void CleanupControl()
        {
            _log.Debug("Cleaning up the Web Browser control instance");
            _webView?.Dispose();
        }
    }
}
