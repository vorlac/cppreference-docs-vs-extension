using System.Windows.Controls;
using CppReferenceDocsExtension.Settings;

namespace CppReferenceDocsExtension
{
    public partial class CppReferenceDocsPanelOptionsControl : UserControl
    {
        public CppReferenceDocsPanelOptionsControl() => InitializeComponent();

        public IDocsBrowserSettings Settings
        {
            get => DataContext as IDocsBrowserSettings;
            set => DataContext = value;
        }
    }
}
