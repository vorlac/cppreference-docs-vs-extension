using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using CppReferenceDocsExtension.Settings;
using CppReferenceDocsExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace CppReferenceDocsExtension
{
    [ComVisible(true), Guid("6a23a02d-5801-4562-b257-b58370eb4e32")]
    public sealed class WebBrowserOptionsPage : UIElementDialogPage
    {
        private readonly ILogger _log = Log.Logger;
        private WebBrowserOptionsPageControl _control;

        protected override UIElement Child => _control ?? (_control = new WebBrowserOptionsPageControl());

        protected override void OnActivate(CancelEventArgs e)
        {
            _log.Debug($"{nameof(WebBrowserOptionsPage)}: OnActivate(Cancel: {e.Cancel})");
            base.OnActivate(e);
            _control.Settings = Site.GetService<IWebBrowserSettings>();
        }

        public override void LoadSettingsFromStorage()
        {
            _log.Debug($"{nameof(WebBrowserOptionsPage)}: LoadSettingFromStorage()");
            base.LoadSettingsFromStorage();
            _control?.Settings?.Load();
        }

        public override void SaveSettingsToStorage()
        {
            _log.Debug($"{nameof(WebBrowserOptionsPage)}: SaveSettingsToStorage()");
            base.SaveSettingsToStorage();
            _control?.Settings?.Save();
        }
    }
}
