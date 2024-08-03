using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CppReferenceDocsExtension.Core.Utils;
using CppReferenceDocsExtension.Editor.Settings;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
//
using SD = System.Drawing;
using WebViewInitCompletedEventArgs = Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs;
using WebViewNavCompletedEventArgs = Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs;
using WebViewCornerPlacement = Microsoft.Web.WebView2.Core.CoreWebView2DefaultDownloadDialogCornerAlignment;

namespace CppReferenceDocsExtension.Editor.ToolWindow
{
    public partial class DocsPanelBrowserWindowControl
    {
        private CoreWebView2Environment environment;
        private readonly List<CoreWebView2Frame> webViewFrames = [];

        public IServiceProvider Services { get; set; }
        private Action<string> SetTitleAction { get; }

        private bool IsNavigating { get; set; } = false;
        private bool IsFirstTimeLoad { get; set; } = true;

        public DocsPanelBrowserWindowControl(Action<string> setTitleAction)
            : this() {
            this.SetTitleAction = setTitleAction;
        }

        private DocsPanelBrowserWindowControl() {
            try {
                this.InitializeComponent();
                this.InitializeAddressBar();
                this.InitializeWebView();

                this.AttachControlEventHandlers(this.webView);
                this.Loaded += this.DocsWindowControlLoaded;
                this.Unloaded += this.DocsWindowControlUnloaded;
            }
            catch (Exception ex) {
                HandleError("Constructor", ex);
            }
        }

        private void AttachControlEventHandlers(WebView2 control) {
            control.NavigationStarting += this.OnNavigationStarting;
            control.NavigationCompleted += this.OnNavigationCompleted;
            control.CoreWebView2InitializationCompleted += this.OnWebViewInitCompleted;
        }

        private void OnWebViewInitCompleted(object sender, WebViewInitCompletedEventArgs e) {
            if (!e.IsSuccess)
                HandleError($"WebView creation failed: {e.InitializationException.Message}", e.InitializationException);
            else {
                this.webView.CoreWebView2.DocumentTitleChanged += this.OnWebViewDocumentTitleChanged;
                this.webView.CoreWebView2.FrameCreated += this.OnWebViewHandleIFrames;
                this.SetDefaultDownloadDialogPosition();
            }
        }

        private async Task NavigateToAsync(Uri uri) {
            await this.webView.EnsureCoreWebView2Async();
            this.webView.CoreWebView2.Navigate(uri.ToString());
            Log.Verbose($"Initiated Navigation to '{uri}'");
        }

        private void OnWebViewHandleIFrames(object sender, CoreWebView2FrameCreatedEventArgs args) {
            this.webViewFrames.Add(args.Frame);
            args.Frame.Destroyed += (frameDestroyedSender, frameDestroyedArgs) => {
                CoreWebView2Frame frameToRemove = this.webViewFrames.SingleOrDefault(
                    r => r.IsDestroyed() == 1
                );

                if (frameToRemove != null)
                    this.webViewFrames.Remove(frameToRemove);
            };
        }

        private async void GoToPageCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            try {
                Log.Verbose($"Navigating to '{e.Parameter ?? "<null>"}'");
                Uri uri = Core.Utils.Helpers.MakeUri((string)e.Parameter);
                await this.NavigateToAsync(uri);
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private void InitializeAddressBar() {
            try {
                this.addressBar.PreviewMouseLeftButtonDown += (s, e) => {
                    if (this.addressBar.IsKeyboardFocusWithin)
                        return;

                    // If the textbox is not yet focused, give it focus
                    // and stop further processing of this click event.
                    this.addressBar.Focus();
                    e.Handled = true;
                };

                this.addressBar.GotKeyboardFocus += (s, e) => this.addressBar.SelectAll();
                this.addressBar.MouseDoubleClick += (s, e) => this.addressBar.SelectAll();
            }
            catch (Exception ex) {
                HandleError(nameof(this.InitializeAddressBar), ex);
            }
        }

        private async void InitializeWebView() {
            try {
                // See https://github.com/MicrosoftEdge/WebView2Feedback/issues/271
                string userDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "VS2022CppRefDocsExtension"
                );

                this.environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await this.webView.EnsureCoreWebView2Async(this.environment);
            }
            catch (Exception ex) {
                HandleError(nameof(this.InitializeWebView), ex);
            }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e) {
            Log.Verbose(
                $"{e.NavigationId} - "
              + $"Navigation Started. Uri: {e.Uri}, "
              + $"User Initiated: {e.IsUserInitiated}, "
              + $"Redirected: {e.IsRedirected}"
            );

            this.IsNavigating = true;
            RequeryCommands();
        }

        private void OnNavigationCompleted(object sender, WebViewNavCompletedEventArgs e) {
            string status = e.HttpStatusCode.ToString();
            if (e.WebErrorStatus != CoreWebView2WebErrorStatus.Unknown)
                status += $" ({e.WebErrorStatus})";

            Log.Verbose($"{e.NavigationId} - Navigation Completed. Status: {status}");
            this.IsNavigating = false;
            RequeryCommands();
        }

        private void SetDefaultDownloadDialogPosition() {
            try {
                const int defaultMarginX = 75;
                const int defaultMarginY = 0;
                const WebViewCornerPlacement cornerAlignment = WebViewCornerPlacement.TopLeft;

                SD.Point margin = new(defaultMarginX, defaultMarginY);
                this.webView.CoreWebView2.DefaultDownloadDialogCornerAlignment = cornerAlignment;
                this.webView.CoreWebView2.DefaultDownloadDialogMargin = margin;
            }
            catch (NotImplementedException ex) {
                Log.Verbose(
                    ex,
                    $"In {nameof(this.SetDefaultDownloadDialogPosition)}, "
                  + $"encountered {nameof(NotImplementedException)}: {ex.Message}"
                );
            }
        }

        private async void DocsWindowControlLoaded(object sender, RoutedEventArgs e) {
            Log.Verbose("Loaded Event Handler");
            try {
                // Forcing a size change to make the web view correct its position on init.
                this.rightFiller.Width = Math.Abs(this.rightFiller.Width - 1.0) < 0.001 ? 0.0 : 1.0;
                if (this.IsFirstTimeLoad) {
                    Log.Verbose($"First time Load: navigate to Home Page");
                    this.IsFirstTimeLoad = false;
                    await this.NavigateToHomeAsync();
                }
            }
            catch (Exception ex) {
                HandleError("Inside Loaded", ex);
            }
        }

        private void DocsWindowControlUnloaded(object sender, RoutedEventArgs e) {
            Log.Verbose("Unloaded Event Handler");
        }

        private void OnWebViewDocumentTitleChanged(object sender, object e) {
            this.SetTitleAction?.Invoke(this.webView.CoreWebView2.DocumentTitle);
        }

        private static void RequeryCommands() {
            CommandManager.InvalidateRequerySuggested();
        }

        private static void HandleError(string message, Exception exception = null) {
            Log.Error(exception, $"{nameof(DocsPanelBrowserWindowControl)} - {message}");
        }

        private async Task NavigateToHomeAsync() {
            try {
                GeneralOptions settings = await GeneralOptions.GetLiveInstanceAsync();
                Uri homepage = settings.GetHomePageUri();
                Log.Verbose($"Home Page Uri is '{homepage}'");
                await this.NavigateToAsync(homepage);
            }
            catch (Exception ex) {
                HandleError("Failed to navigate to Home Uri", ex);
            }
        }

        private void BrowseBackCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            try {
                Log.Verbose("Navigating Backward");
                this.webView.CoreWebView2.GoBack();
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private void BrowseForwardCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            try {
                Log.Verbose("Navigating Forward");
                this.webView.CoreWebView2.GoForward();
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private void RefreshCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            try {
                Log.Verbose("Reloading Current Page");
                this.webView.CoreWebView2.Reload();
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private async void BrowseHomeCmdExecuted(object target, ExecutedRoutedEventArgs e) {
            try {
                Log.Verbose("Navigating to Home Page");
                await this.NavigateToHomeAsync();
            }
            catch (Exception ex) {
                HandleError(nameof(this.GoToPageCmdExecuted), ex);
            }
        }

        private void CanExecuteBrowseBackCmd(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.IsNavigating && (this.webView?.CoreWebView2?.CanGoBack ?? false);
        }

        private void CanExecuteBrowseForwardCmd(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.IsNavigating && (this.webView?.CoreWebView2?.CanGoForward ?? false);
        }

        private void CanExecuteRefreshCmd(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.IsNavigating && this.webView?.CoreWebView2 != null;
        }

        private void CanExecuteBrowseHomeCmd(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.IsNavigating && this.webView?.CoreWebView2 != null;
        }

        private void CanExecuteGoToPageCmd(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = !this.IsNavigating && this.webView?.CoreWebView2 != null;
        }
    }
}
