using CppReferenceDocsExtension.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CppReferenceDocsExtension.Settings;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using SD = System.Drawing;
using Serilog;
using WebViewAlignment = Microsoft.Web.WebView2.Core.CoreWebView2DefaultDownloadDialogCornerAlignment;

namespace CppReferenceDocsExtension
{
    public partial class WebBrowserWindowControl : UserControl
    {
        private readonly ILogger _log = Log.Logger;
        private readonly List<CoreWebView2Frame> _webViewFrames = new List<CoreWebView2Frame>();
        private CoreWebView2Environment _environment;
        private bool _isNavigating = false;
        private bool _isFirstTimeLoad = true;

        public WebBrowserWindowControl()
        {
            try
            {
                InitializeComponent();
                InitializeAddressBar();
                InitializeWebView();
                AttachControlEventHandlers(webView);

                Loaded += WebBrowserWindowControl_Loaded;
                Unloaded += WebBrowserWindowControl_Unloaded;
            }
            catch (Exception ex)
            {
                HandleError("Constructor", ex);
            }
        }

        ////public Uri HomePageUri { get; set; }
        public IServiceProvider Services { get; set; }
        public Action<string> SetTitleAction { get; set; }

        private void InitializeAddressBar()
        {
            try
            {
                addressBar.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    if (addressBar.IsKeyboardFocusWithin)
                        return;

                    // If the textbox is not yet focused, give it focus
                    // and stop further processing of this click event.
                    _ = addressBar.Focus();
                    e.Handled = true;
                };

                addressBar.GotKeyboardFocus += (s, e) => addressBar.SelectAll();
                addressBar.MouseDoubleClick += (s, e) => addressBar.SelectAll();
            }
            catch (Exception ex)
            {
                HandleError(nameof(InitializeAddressBar), ex);
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                // See https://github.com/MicrosoftEdge/WebView2Feedback/issues/271
                string userDataFolder =
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "VS2022WebBrowserExtension");
                _environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                await webView.EnsureCoreWebView2Async(_environment);
            }
            catch (Exception ex)
            {
                HandleError(nameof(InitializeWebView), ex);
            }
        }

        private void AttachControlEventHandlers(WebView2 control)
        {
            control.NavigationStarting += OnNavigationStarting;
            control.NavigationCompleted += OnNavigationCompleted;
            control.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Log.Verbose($"{e.NavigationId} - Navigation Started. Uri: {e.Uri}, User Initiated: {e.IsUserInitiated}, Redirected: {e.IsRedirected}");
            _isNavigating = true;
            RequeryCommands();
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            string status = e.HttpStatusCode.ToString();
            if (e.WebErrorStatus != CoreWebView2WebErrorStatus.Unknown)
                status += $" ({e.WebErrorStatus})";

            Log.Verbose($"{e.NavigationId} - Navigation Completed. Status: {status}");
            _isNavigating = false;
            RequeryCommands();
        }

        private void OnCoreWebView2InitializationCompleted(object sender,
            CoreWebView2InitializationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                HandleError($"WebView2 creation failed: {e.InitializationException.Message}",
                    e.InitializationException);
                return;
            }

            webView.CoreWebView2.DocumentTitleChanged += OnWebViewDocumentTitleChanged;
            webView.CoreWebView2.FrameCreated += OnWebViewHandleIFrames;
            SetDefaultDownloadDialogPosition();
        }

        private void OnWebViewDocumentTitleChanged(object sender, object e) =>
            SetTitleAction?.Invoke(webView.CoreWebView2.DocumentTitle);

        private void OnWebViewHandleIFrames(object sender, CoreWebView2FrameCreatedEventArgs args)
        {
            _webViewFrames.Add(args.Frame);
            args.Frame.Destroyed += (frameDestroyedSender, frameDestroyedArgs) =>
            {
                CoreWebView2Frame frameToRemove = _webViewFrames.SingleOrDefault(r => r.IsDestroyed() == 1);
                if (frameToRemove != null)
                    _ = _webViewFrames.Remove(frameToRemove);
            };
        }

        private void SetDefaultDownloadDialogPosition()
        {
            try
            {
                const int defaultMarginX = 75, defaultMarginY = 0;
                CoreWebView2DefaultDownloadDialogCornerAlignment cornerAlignment =
                    CoreWebView2DefaultDownloadDialogCornerAlignment.TopLeft;
                SD.Point margin = new SD.Point(defaultMarginX, defaultMarginY);
                webView.CoreWebView2.DefaultDownloadDialogCornerAlignment = cornerAlignment;
                webView.CoreWebView2.DefaultDownloadDialogMargin = margin;
            }
            catch (NotImplementedException ex)
            {
                Log.Verbose(ex,
                    $"In {nameof(SetDefaultDownloadDialogPosition)}, encountered {nameof(NotImplementedException)}: {ex.Message}");
            }
        }

        private static void RequeryCommands() => CommandManager.InvalidateRequerySuggested();

        private static void HandleError(string message, Exception exception = null) =>
            Log.Error(exception, $"{nameof(WebBrowserWindowControl)} - {message}");

        private void WebBrowserWindowControl_Unloaded(object sender, RoutedEventArgs e) =>
            Log.Verbose("Unloaded Event Handler");

        private async void WebBrowserWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            Log.Verbose("Loaded Event Handler");
            try
            {
                // Forcing a size change to make the web view correct its position on init.
                rightFiller.Width = Math.Abs(rightFiller.Width - 1.0) < 0.001 ? 0.0 : 1.0;
                if (_isFirstTimeLoad)
                {
                    Log.Verbose($"First time Load: navigate to Home Page");
                    _isFirstTimeLoad = false;
                    await NavigateToHomeAsync();
                }
            }
            catch (Exception ex)
            {
                HandleError("Inside Loaded", ex);
            }
        }

        private async Task NavigateToAsync(Uri uri)
        {
            await webView.EnsureCoreWebView2Async();
            // Setting webView.Source will not trigger a navigation
            // if the Source is the same as the previous Source.
            // CoreWebView.Navigate() will always trigger a navigation.
            webView.CoreWebView2.Navigate(uri.ToString());
            Log.Verbose($"Initiated Navigation to '{uri}'");
        }

        private async Task NavigateToHomeAsync()
        {
            try
            {
                IWebBrowserSettings settings = GetService<Settings.IWebBrowserSettings>();
                Uri homepage = settings.GetHomePageUri();
                Log.Verbose($"Home Page Uri is '{homepage}'");
                await NavigateToAsync(homepage);
            }
            catch (Exception ex)
            {
                HandleError("Failed to navigate to Home Uri", ex);
            }
        }

        private T GetService<T>() where T : class => Services.GetService<T>();
    }
}
