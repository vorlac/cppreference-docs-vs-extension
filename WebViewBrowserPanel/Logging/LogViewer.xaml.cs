using System;
using System.Windows;
using System.Windows.Controls;

namespace WebViewBrowserPanel.Logging
{
    public sealed partial class LogViewer : UserControl, IDisposable
    {
        private readonly WpfLogViewer _logViewer;

        public LogViewer()
        {
            InitializeComponent();

            _logViewer = new WpfLogViewer
            {
                Name = "logViewer",
                IsEnabled = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            AddChild(_logViewer);
        }

        public void Dispose() => _logViewer.Dispose();
    }
}
