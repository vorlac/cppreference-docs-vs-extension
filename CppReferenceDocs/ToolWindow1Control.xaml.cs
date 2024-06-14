using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace CppReferenceDocs
{
    public partial class ToolWindow1Control : UserControl
    {
        public ToolWindow1Control()
        {
            InitializeComponent();

            UrlTextBox.Width = WindowBodyPanel.ActualWidth - SearchButton.ActualWidth;
            WebView.Source = new Uri("http://www.cppreference.com");
        }

        private void OnSearchButtonPressed(object sender, RoutedEventArgs e)
        {
            var key = e as KeyboardEventArgs;
            if (key != null && key.KeyboardDevice.IsKeyDown(Key.Enter))
            {
                var uri = new Uri(UrlTextBox.Text);
                if (!uri.IsAbsoluteUri)
                    uri = new Uri("https://www." + uri.Host);
                if (!this.WebView.IsInitialized)
                    this.WebView.EnsureCoreWebView2Async().Wait();

                WebView.Source = uri;
                WebView.CoreWebView2.Navigate(uri.ToString());
            }
        }

        private void OnWindowResize(object sender, EventArgs e)
        {
            UrlTextBox.Width = this.ActualWidth - SearchButton.ActualWidth;
        }

        private void OnWebViewSourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (!this.WebView.IsInitialized)
                this.WebView.EnsureCoreWebView2Async().Wait();

            Uri uri = new Uri(e.ToString());
            this.WebView.CoreWebView2.Navigate(uri.ToString());
        }
    }
}
