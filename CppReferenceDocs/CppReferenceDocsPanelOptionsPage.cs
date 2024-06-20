using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using CppReferenceDocsExtension.Settings;
using CppReferenceDocsExtension.Utils;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace CppReferenceDocsExtension
{
    [ComVisible(true)]
    public sealed class CppReferenceDocsPanelOptionsPage : UIElementDialogPage
    {
        private readonly ILogger _log = Log.Logger;
        private CppReferenceDocsPanelOptionsControl _control;

        protected override UIElement Child => _control ?? (_control = new CppReferenceDocsPanelOptionsControl());

        protected override void OnActivate(CancelEventArgs e)
        {
            _log.Debug($"{nameof(CppReferenceDocsPanelOptionsPage)}: OnActivate(Cancel: {e.Cancel})");
            base.OnActivate(e);
            _control.Settings = Site.GetService<IDocsBrowserSettings>();
        }

        public override void LoadSettingsFromStorage()
        {
            _log.Debug($"{nameof(CppReferenceDocsPanelOptionsPage)}: LoadSettingFromStorage()");
            base.LoadSettingsFromStorage();
            _control?.Settings?.Load();
        }

        public override void SaveSettingsToStorage()
        {
            _log.Debug($"{nameof(CppReferenceDocsPanelOptionsPage)}: SaveSettingsToStorage()");
            base.SaveSettingsToStorage();
            _control?.Settings?.Save();
        }
    }
}
