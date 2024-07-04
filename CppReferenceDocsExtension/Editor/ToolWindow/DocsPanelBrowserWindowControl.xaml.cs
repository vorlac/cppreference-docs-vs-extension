using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CppReferenceDocsExtension.Core.Utils;
using CppReferenceDocsExtension.Editor.Settings;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using SD = System.Drawing;
using WebViewInitCompletedEventArgs = Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs;
using WebViewNavCompletedEventArgs = Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs;
using WebViewCornerPlacement = Microsoft.Web.WebView2.Core.CoreWebView2DefaultDownloadDialogCornerAlignment;

namespace CppReferenceDocsExtension.Editor.ToolWindow {
    public partial class DocsPanelBrowserWindowControl : UserControl {
        private readonly ILogger log = Log.Logger;
        private readonly List<CoreWebView2Frame> webViewFrames = new List<CoreWebView2Frame>();
        private CoreWebView2Environment environment;

        private bool isNavigating = false;
        private bool isFirstTimeLoad = true;

        public IServiceProvider Services { get; set; }
        public Action<string> SetTitleAction { get; set; }

        public DocsPanelBrowserWindowControl() {
            try {
                this.InitializeComponent();
                this.InitializeAddressBar();
                this.InitializeWebView();
                this.AttachControlEventHandlers(this.webView);

                this.Loaded += this.WebBrowserWindowControl_Loaded;
                this.Unloaded += this.WebBrowserWindowControl_Unloaded;
            }
            catch (Exception ex) {
                HandleError("Constructor", ex);
            }
        }

        private void InitializeAddressBar() {
            try {
                this.addressBar.PreviewMouseLeftButtonDown += (s, e) => {
                    if (this.addressBar.IsKeyboardFocusWithin)
                        return;

                    // If the textbox is not yet focused, give it focus
                    // and stop further processing of this click event.
                    _ = this.addressBar.Focus();
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

        private void AttachControlEventHandlers(WebView2 control) {
            control.NavigationStarting += this.OnNavigationStarting;
            control.NavigationCompleted += this.OnNavigationCompleted;
            control.CoreWebView2InitializationCompleted += this.OnCoreWebView2InitializationCompleted;
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e) {
            Log.Verbose(
                $"{e.NavigationId} - "
              + $"Navigation Started. Uri: {e.Uri}, "
              + $"User Initiated: {e.IsUserInitiated}, "
              + $"Redirected: {e.IsRedirected}"
            );

            this.isNavigating = true;
            RequeryCommands();
        }

        private void OnNavigationCompleted(object sender, WebViewNavCompletedEventArgs e) {
            string status = e.HttpStatusCode.ToString();
            if (e.WebErrorStatus != CoreWebView2WebErrorStatus.Unknown) {
                status += $" ({e.WebErrorStatus})";
            }

            Log.Verbose($"{e.NavigationId} - Navigation Completed. Status: {status}");
            this.isNavigating = false;
            RequeryCommands();
        }

        private void OnCoreWebView2InitializationCompleted(object sender,
                                                           WebViewInitCompletedEventArgs e) {
            if (!e.IsSuccess) {
                HandleError(
                    $"WebView2 creation failed: {e.InitializationException.Message}",
                    e.InitializationException
                );

                return;
            }

            this.webView.CoreWebView2.DocumentTitleChanged += this.OnWebViewDocumentTitleChanged;
            this.webView.CoreWebView2.FrameCreated += this.OnWebViewHandleIFrames;
            this.SetDefaultDownloadDialogPosition();
        }

        private void OnWebViewDocumentTitleChanged(object sender, object e) {
            this.SetTitleAction?.Invoke(this.webView.CoreWebView2.DocumentTitle);
        }

        private void OnWebViewHandleIFrames(object sender, CoreWebView2FrameCreatedEventArgs args) {
            this.webViewFrames.Add(args.Frame);
            args.Frame.Destroyed += (frameDestroyedSender, frameDestroyedArgs) => {
                CoreWebView2Frame frameToRemove = this.webViewFrames.SingleOrDefault(r => r.IsDestroyed() == 1);

                if (frameToRemove != null) {
                    _ = this.webViewFrames.Remove(frameToRemove);
                }
            };
        }

        private void SetDefaultDownloadDialogPosition() {
            try {
                const int defaultMarginX = 75;
                const int defaultMarginY = 0;
                const WebViewCornerPlacement cornerAlignment = WebViewCornerPlacement.TopLeft;

                var margin = new SD.Point(defaultMarginX, defaultMarginY);
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

        private static void RequeryCommands() {
            CommandManager.InvalidateRequerySuggested();
        }

        private static void HandleError(string message, Exception exception = null) {
            Log.Error(exception, $"{nameof(DocsPanelBrowserWindowControl)} - {message}");
        }

        private void WebBrowserWindowControl_Unloaded(object sender, RoutedEventArgs e) {
            Log.Verbose("Unloaded Event Handler");
        }

        private async void WebBrowserWindowControl_Loaded(object sender, RoutedEventArgs e) {
            Log.Verbose("Loaded Event Handler");
            try {
                // Forcing a size change to make the web view correct its position on init.
                this.rightFiller.Width = Math.Abs(this.rightFiller.Width - 1.0) < 0.001 ? 0.0 : 1.0;
                if (this.isFirstTimeLoad) {
                    Log.Verbose($"First time Load: navigate to Home Page");
                    this.isFirstTimeLoad = false;
                    await this.NavigateToHomeAsync();
                }
            }
            catch (Exception ex) {
                HandleError("Inside Loaded", ex);
            }
        }

        private async Task NavigateToAsync(Uri uri) {
            await this.webView.EnsureCoreWebView2Async();
            // Setting webView.Source will not trigger a navigation
            // if the Source is the same as the previous Source.
            // CoreWebView.Navigate() will always trigger a navigation.
            this.webView.CoreWebView2.Navigate(uri.ToString());
            Log.Verbose($"Initiated Navigation to '{uri}'");
        }

        private async Task NavigateToHomeAsync() {
            try {
                var settings = await GeneralOptions.GetLiveInstanceAsync();
                Uri homepage = settings.GetHomePageUri();
                Log.Verbose($"Home Page Uri is '{homepage}'");
                await this.NavigateToAsync(homepage);
            }
            catch (Exception ex) {
                HandleError("Failed to navigate to Home Uri", ex);
            }
        }

        private T GetService<T>() where T : class {
            return this.Services.GetService<T>();
        }
    }
}
