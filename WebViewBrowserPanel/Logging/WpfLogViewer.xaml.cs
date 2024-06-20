using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using NLog;
using NLog.Config;
using NLog.Layouts;
using WebViewBrowserPanel.Utils;

namespace WebViewBrowserPanel.Logging
{
    public sealed partial class WpfLogViewer : UserControl, IDisposable
    {
        private const string FormatString =
            "${time}|${pad:padding=-5:inner=${level:uppercase=true}}|${pad:padding=-20:fixedLength=True:alignmentOnTruncation=right:inner=${logger:shortName=true}}|${message}${onexception:inner=${newline}${exception:format=tostring}}";

        private static readonly ILogger s_log = LogManager.GetCurrentClassLogger();
        private static readonly SimpleLayout s_layout = new SimpleLayout(FormatString);
        private readonly LogEventMemoryTarget _logTarget;
        private readonly LogColorizer _colorizer;
        private LogLevel _thresholdLogLevel = LogLevel.Trace;

        private const int MaxLineDisplayed = 100000;
        private const int NbrOfLineToDeleteWhenLimitIsReached = 50000;

        public WpfLogViewer()
        {
            InitializeComponent();

            debugBar.Visibility = Visibility.Collapsed;

            _colorizer = CreateColorizer();
            logBox.TextArea.TextView.LineTransformers.Add(_colorizer);

            _logTarget = new LogEventMemoryTarget();
            _logTarget.EventReceived += info => DispatchLog(info);

            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, _logTarget));
            LogManager.ReconfigExistingLoggers();

            foreach (LogLevel level in LogLevel.AllLevels.OrderBy(l => l.Ordinal))
                _ = levelBox.Items.Add(level);

            levelBox.SelectedItem = _thresholdLogLevel;
            levelBox.SelectionChanged += (s, _) => _thresholdLogLevel = SelectedLogLevel;

            ClearCommand = new RelayCommand(() =>
            {
                if (_colorizer != null) _colorizer.Clear();
                logBox.Document.Text = string.Empty;
            }, () => !string.IsNullOrEmpty(logBox.Document.Text));

            CopyAllCommand = new RelayCommand(() =>
            {
                try
                {
                    string text = logBox.Document.Text;
                    DataObject data = new DataObject(text);
                    string html = HtmlClipboard.CreateHtmlFragment(
                        logBox.Document, null, null, new HtmlOptions(logBox.Options));
                    HtmlClipboard.SetHtml(data, html);
                    Clipboard.SetDataObject(data, true);
                }
                catch (Exception ex)
                {
                    // There was a problem while writing to the clipboard... let's log it!
                    s_log.Error(ex);
                }
            }, () => !string.IsNullOrEmpty(logBox.Document.Text));
        }

        public ICommand ClearCommand { get; }
        public ICommand CopyAllCommand { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => _logTarget.Dispose();

        /// <summary>
        /// Gets or sets the selected log level.
        /// </summary>
        /// <value>The selected log level.</value>
        public LogLevel SelectedLogLevel => (LogLevel)levelBox.SelectedItem;

        private void DispatchLog(LogEventInfo entry)
        {
            if (!Dispatcher.CheckAccess())
                _ = Dispatcher.BeginInvoke(
                    DispatcherPriority.Normal, new Action<LogEventInfo>(e => LogEntryToTextBox(e)), entry);
            else LogEntryToTextBox(entry);
        }

        private void LogEntryToTextBox(LogEventInfo entry)
        {
            if (_thresholdLogLevel.Ordinal > entry.Level.Ordinal)
                return;

            int start = logBox.Document.TextLength;
            logBox.AppendText(s_layout.Render(entry));
            logBox.AppendText("\r");
            int end = logBox.Document.TextLength;
            _colorizer.AddLogLineInfo(new LogLineInfo { StartOffset = start, EndOffset = end, Level = entry.Level });

            if (logBox.Document.LineCount > MaxLineDisplayed)
            {
                // Delete old lines in textbox
                DocumentLine line = logBox.Document.GetLineByNumber(NbrOfLineToDeleteWhenLimitIsReached);
                int endOffset = line.EndOffset + line.DelimiterLength;
                logBox.Document.Remove(0, endOffset);

                // Delete old data in colorizer
                if (_colorizer != null)
                    _colorizer.ClearOldData(NbrOfLineToDeleteWhenLimitIsReached);
            }

            logBox.ScrollToEnd();
        }

        private static LogColorizer CreateColorizer()
        {
            Dictionary<LogLevel, LogLineStyle> logLinesStyle = new Dictionary<LogLevel, LogLineStyle>
            {
                [LogLevel.Fatal] =
                    new LogLineStyle { ForegroundBrush = Brushes.Red, FontWeight = FontWeights.Bold },
                [LogLevel.Error] = new LogLineStyle { ForegroundBrush = Brushes.Red },
                [LogLevel.Warn] =
                    new LogLineStyle { ForegroundBrush = Brushes.Orange, FontWeight = FontWeights.Bold },
                [LogLevel.Info] = new LogLineStyle { FontWeight = FontWeights.Bold },
                [LogLevel.Debug] = new LogLineStyle { ForegroundBrush = Brushes.Blue },
                [LogLevel.Trace] = new LogLineStyle()
            };

            return new LogColorizer(ll => logLinesStyle.ContainsKey(ll) ? logLinesStyle[ll] : null);
        }

        // Debug Bar

        private static int s_counter;

        private void FatalButton_Click(object sender, RoutedEventArgs e)
        {
            s_counter++;
            s_log.Fatal($"Fatal #{s_counter}");
        }

        private void ErrorButton_Click(object sender, RoutedEventArgs e)
        {
            s_counter++;
            s_log.Error($"Error #{s_counter}");
        }

        private void WarningButton_Click(object sender, RoutedEventArgs e)
        {
            s_counter++;
            s_log.Warn($"Warning #{s_counter}");
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            s_counter++;
            s_log.Info($"Info #{s_counter}");
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            s_counter++;
            s_log.Debug($"Debug #{s_counter}");
        }

        private void TraceButton_Click(object sender, RoutedEventArgs e)
        {
            s_counter++;
            s_log.Debug($"Trace #{s_counter}");
        }
    }
}
