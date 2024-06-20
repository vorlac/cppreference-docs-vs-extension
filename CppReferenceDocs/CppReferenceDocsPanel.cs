using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Wpf;
using Serilog;

namespace CppReferenceDocsExtension
{
    [Guid("8ab2cef3-aaaa-4e4a-8d07-1dd7f9f90a1c")]
    public sealed class CppReferenceDocsPanel : ToolWindowPane, IVsWindowFrameNotify2
    {
        private readonly ILogger _log = Log.Logger;
        private readonly CppReferenceDocsPanelControl _control;
        private readonly WebView2 _webView;

        public CppReferenceDocsPanel() : base(null)
        {
            Caption = Constants.ExtensionName;
            _control = new CppReferenceDocsPanelControl { SetTitleAction = x => Caption = x };
            Content = _control;
            _webView = _control.webView;

            // Retrieve DTE and listen to "Visual Studio Shutdown" event
            ThreadHelper.ThrowIfNotOnUIThread();
            DTE dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
            dte.Events.DTEEvents.OnBeginShutdown += OnVisualStudioShutDown;
        }

        public int OnClose(ref uint pgrfSaveOptions)
        {
            _log.Debug($"{nameof(CppReferenceDocsPanel)}: OnClose({pgrfSaveOptions})");
            return VSConstants.S_OK;
        }

        public override void OnToolWindowCreated()
        {
            base.OnToolWindowCreated();
            _log.Debug($"{nameof(CppReferenceDocsPanel)}: OnToolWindowCreated()");
        }

        protected override void Initialize()
        {
            _log.Verbose($"Initializing {nameof(CppReferenceDocsPanel)}");
            base.Initialize();
            _control.Services = this;
            _log.Verbose($"Initialized {nameof(CppReferenceDocsPanel)}");
        }

        private void OnVisualStudioShutDown()
        {
            _log.Debug($"{nameof(CppReferenceDocsPanel)}: Visual Studio is closing");
            CleanupControl();
        }

        // Not sure if this is necessary. It seems VS takes care of the cleaning up
        // Never mind, let's keep it that way for now...
        private void CleanupControl()
        {
            _log.Debug("Cleaning up the Web Browser control instance");
            _webView?.Dispose();
        }
    }
}
