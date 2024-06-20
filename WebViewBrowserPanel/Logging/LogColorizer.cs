using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using NLog;

namespace WebViewBrowserPanel.Logging
{
    internal sealed class LogLineInfo
    {
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public LogLevel Level { get; set; }
    }

    internal sealed class LogLineStyle
    {
        public Brush BackgroundBrush { get; set; }
        public Brush ForegroundBrush { get; set; }
        public FontStyle? FontStyle { get; set; }
        public FontWeight? FontWeight { get; set; }

        public void ApplyTo(VisualLineElement element)
        {
            if (BackgroundBrush != null) element.TextRunProperties.SetForegroundBrush(BackgroundBrush);
            if (ForegroundBrush != null) element.TextRunProperties.SetForegroundBrush(ForegroundBrush);

            if (FontStyle != null || FontWeight != null)
            {
                Typeface face = element.TextRunProperties.Typeface;
                element.TextRunProperties.SetTypeface(new Typeface(
                    face.FontFamily,
                    FontStyle ?? face.Style,
                    FontWeight ?? face.Weight,
                    face.Stretch
                ));
            }
        }
    }

    internal sealed class LogColorizer : DocumentColorizingTransformer
    {
        private readonly List<int> _startOffsets = new List<int>();
        private readonly Dictionary<int, LogLineInfo> _dictionary = new Dictionary<int, LogLineInfo>();
        private readonly Func<LogLevel, LogLineStyle> _getStyle;

        public LogColorizer(Func<LogLevel, LogLineStyle> getLogLineStyle) => _getStyle = getLogLineStyle;

        public void Clear()
        {
            _startOffsets.Clear();
            _dictionary.Clear();
        }

        public void ClearOldData(int nbrOfLineToDelete)
        {
            _startOffsets.RemoveRange(0, nbrOfLineToDelete);

            // Ugly, but it works...
            int test = 0;
            List<int> keys = _dictionary.Keys.Cast<int>().ToList();
            foreach (int key in keys)
            {
                _ = _dictionary.Remove(key);
                test++;
                if (test == nbrOfLineToDelete)
                    break;
            }
        }

        public void AddLogLineInfo(LogLineInfo info)
        {
            if (_dictionary.ContainsKey(info.StartOffset))
                _dictionary[info.StartOffset] = info;
            else
            {
                _startOffsets.Add(info.StartOffset);
                _dictionary.Add(info.StartOffset, info);
            }
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (line == null || line.Length == 0)
                return;

            LogLineInfo info = FindLineInfo(line);
            if (info == null)
                return;

            int start = line.Offset > info.StartOffset ? line.Offset : info.StartOffset;
            int end = info.EndOffset > line.EndOffset ? line.EndOffset : info.EndOffset;

            LogLineStyle style = null;
            if (_getStyle != null) style = _getStyle(info.Level);
            if (style != null)
                ChangeLinePart(start, end, element => style.ApplyTo(element));
        }

        private LogLineInfo FindLineInfo(DocumentLine line)
        {
            int? offset = FindNearestOffset(line.Offset);
            return offset.HasValue && _dictionary.ContainsKey(offset.Value) ? _dictionary[offset.Value] : null;
        }

        private int? FindNearestOffset(int offset)
        {
            int index = _startOffsets.FindLastIndex(o => o <= offset);
            return index == -1 ? null : (int?)_startOffsets[index];
        }
    }
}
