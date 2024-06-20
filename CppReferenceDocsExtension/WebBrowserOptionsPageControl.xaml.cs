using System.Windows.Controls;
using CppReferenceDocsExtension.Settings;

namespace CppReferenceDocsExtension
{
    public partial class WebBrowserOptionsPageControl : UserControl
    {
        public WebBrowserOptionsPageControl() => InitializeComponent();

        public IWebBrowserSettings Settings
        {
            get => DataContext as IWebBrowserSettings;
            set => DataContext = value;
        }
    }
}
