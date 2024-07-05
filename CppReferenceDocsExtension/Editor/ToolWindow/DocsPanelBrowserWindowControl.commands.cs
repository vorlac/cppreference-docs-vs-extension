using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CppReferenceDocsExtension.Core.Utils;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace CppReferenceDocsExtension.Editor.ToolWindow
{
    public partial class DocsPanelBrowserWindowControl
    {
        public DocsPanelBrowserWindowControl(Action<string> setTitleAction)
            : this() {
            this.SetTitleAction = setTitleAction;
        }

        private void BrowseBackCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.isNavigating && (this.webView?.CoreWebView2?.CanGoBack ?? false);
        }

        private void BrowseForwardCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.isNavigating && (this.webView?.CoreWebView2?.CanGoForward ?? false);
        }

        private void RefreshCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.isNavigating && this.webView?.CoreWebView2 != null;
        }

        private void BrowseHomeCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.isNavigating && this.webView?.CoreWebView2 != null;
        }

        private void GoToPageCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.isNavigating && this.webView?.CoreWebView2 != null;
        }

        private async Task<T> GetCoreWebView2ValueAsync<T>(Func<CoreWebView2, T> func) {
            await this.webView.EnsureCoreWebView2Async();
            return func(this.webView.CoreWebView2);
        }

        private void BrowseBackCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            Log.Verbose($"Navigating Backward");
            try {
                this.webView.CoreWebView2.GoBack();
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private void BrowseForwardCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            Log.Verbose($"Navigating Forward");
            try {
                this.webView.CoreWebView2.GoForward();
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private void RefreshCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            Log.Verbose($"Reloading Current Page");
            try {
                this.webView.CoreWebView2.Reload();
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private async void BrowseHomeCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            Log.Verbose($"Navigating to Home Page");
            try {
                await this.NavigateToHomeAsync();
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private async void GoToPageCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            Log.Verbose($"Navigating to '{e.Parameter ?? "<null>"}'");
            try {
                Uri uri = UriHelper.MakeUri((string)e.Parameter);
                await this.NavigateToAsync(uri);
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }
    }
}
