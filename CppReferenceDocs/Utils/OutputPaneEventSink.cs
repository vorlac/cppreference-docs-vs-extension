using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace CppReferenceDocsExtension.Utils
{
    internal sealed class OutputPaneEventSink : ILogEventSink
    {
        private static readonly Guid s_paneGuid = new Guid("8851EA3E-AAAA-4C9A-B31B-97D26037E6D3");
        private readonly IVsOutputWindowPane _pane;
        private readonly ITextFormatter _formatter;

        public OutputPaneEventSink(IVsOutputWindow outputWindow, string outputTemplate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _formatter = new MessageTemplateTextFormatter(outputTemplate, null);
            ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(s_paneGuid, "CppReference Docs", 1, 1));
            outputWindow.GetPane(s_paneGuid, out _pane);
        }

        public void Emit(LogEvent logEvent)
        {
            StringWriter sw = new StringWriter();
            _formatter.Format(logEvent, sw);
            string message = sw.ToString();

            ThreadHelper.ThrowIfNotOnUIThread();

            if (_pane is IVsOutputWindowPaneNoPump noPump)
                noPump.OutputStringNoPump(message);
            else
                ErrorHandler.ThrowOnFailure(_pane.OutputStringThreadSafe(message));

            if (logEvent.Level == LogEventLevel.Error)
                _pane.Activate();
        }
    }
}
