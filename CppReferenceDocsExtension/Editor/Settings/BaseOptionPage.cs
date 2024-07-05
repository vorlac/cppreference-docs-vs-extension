using Microsoft.VisualStudio.Shell;

namespace CppReferenceDocsExtension.Editor.Settings
{
    internal class BaseOptionPage<T> : DialogPage
        where T : BaseOptionModel<T>, new()
    {
        private readonly BaseOptionModel<T> model;

        public BaseOptionPage() {
            this.model = ThreadHelper.JoinableTaskFactory.Run(BaseOptionModel<T>.CreateAsync);
        }

        public override object AutomationObject => this.model;

        public override void LoadSettingsFromStorage() {
            this.model.Load();
        }

        public override void SaveSettingsToStorage() {
            this.model.Save();
        }
    }
}
