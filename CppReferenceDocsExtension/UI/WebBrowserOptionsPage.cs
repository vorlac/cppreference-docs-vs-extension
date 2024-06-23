using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using CppReferenceDocsExtension.Core.Utils;
using CppReferenceDocsExtension.Settings;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace CppReferenceDocsExtension.UI {
    [ComVisible(true)]
    public sealed class WebBrowserOptionsPage : UIElementDialogPage {
        private WebBrowserOptionsPageControl control;
        private readonly ILogger log = Log.Logger;

        public WebBrowserOptionsPage(WebBrowserOptionsPageControl control) {
            this.control = control;
        }

        protected override UIElement Child => this.control ?? (this.control = new WebBrowserOptionsPageControl());

        protected override void OnActivate(CancelEventArgs e) {
            this.log.Debug($"{nameof(WebBrowserOptionsPage)}: OnActivate(Cancel: {e.Cancel})");
            base.OnActivate(e);
            this.control.Settings = this.Site.GetService<IWebBrowserSettings>();
        }

        public override void LoadSettingsFromStorage() {
            this.log.Debug($"{nameof(WebBrowserOptionsPage)}: LoadSettingFromStorage()");
            base.LoadSettingsFromStorage();
            this.control?.Settings?.Load();
        }

        public override void SaveSettingsToStorage() {
            this.log.Debug($"{nameof(WebBrowserOptionsPage)}: SaveSettingsToStorage()");
            base.SaveSettingsToStorage();
            this.control?.Settings?.Save();
        }
    }
}
