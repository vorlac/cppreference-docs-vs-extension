using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core.Raw;
using System.Threading.Tasks;

namespace CppReferenceDocs
{
    public partial class ToolWindow1Control : UserControl
    {
        public ToolWindow1Control()
        {
            InitializeComponent();
            InitializeAsync();

            this.UrlTextBox.Width = this.WindowBodyPanel.ActualWidth - this.SearchBtn.ActualWidth;
        }

        public async void InitializeAsync()
        {
            string installPath = @"C:\Program Files (x86)\Microsoft\EdgeWebView\Application\125.0.2535.92";
            var options = new CoreWebView2EnvironmentOptions();
            options.ExclusiveUserDataFolderAccess = true;
            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, @"C:\Temp", options);
            await this.WebView.EnsureCoreWebView2Async(webView2Environment);
            // if (WebViewCreationEnv == null)
            //     WebViewCreationEnv = await WebView2.CreationProperties.CreateEnvironmentAsync();
            // if (this.WebViewEnvironment == null)
            //     WebViewEnvironment = await WebView2.CreationProperties.CreateEnvironmentAsync();
        }

        private void OnSearchBtnPressed(object sender, RoutedEventArgs e)
        {
            var key = e as KeyboardEventArgs;
            if (key != null && key.KeyboardDevice.IsKeyDown(Key.Enter))
            {
                if (!this.WebView.IsInitialized)
                    this.WebView.EnsureCoreWebView2Async(null).Wait();

                if (WebView != null && WebView.CoreWebView2 != null)
                    WebView.CoreWebView2.Navigate(UrlTextBox.Text);
            }
        }

        private void OnWindowResize(object sender, EventArgs e)
        {
            UrlTextBox.Width = this.ActualWidth - this.SearchBtn.ActualWidth;
        }

        private void OnWebViewSourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            Uri uri = new Uri(e.ToString());
            this.WebView.Source = new Uri(
                uri.IsAbsoluteUri
                    ? uri.AbsoluteUri
                    : $"https://{uri.Host}"
            );
        }
    }
}
