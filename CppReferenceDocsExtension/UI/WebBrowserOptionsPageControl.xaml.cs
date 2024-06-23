using System.Windows.Controls;
using CppReferenceDocsExtension.Settings;

namespace CppReferenceDocsExtension.UI {
    public partial class WebBrowserOptionsPageControl : UserControl {
        public WebBrowserOptionsPageControl() {
            this.InitializeComponent();
        }

        public IWebBrowserSettings Settings {
            get => this.DataContext as IWebBrowserSettings;
            set => this.DataContext = value;
        }
    }
}
