using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace CppReferenceDocsExtension.Core.Utils
{
    internal sealed class OutputPaneEventSink : ILogEventSink
    {
        private static readonly Guid SPaneGuid = new Guid("8851EA3E-6283-4C9A-B31B-97D26037E6D3");

        private readonly IVsOutputWindowPane pane;
        private readonly ITextFormatter formatter;

        public OutputPaneEventSink(IVsOutputWindow outputWindow, string outputTemplate) {
            ThreadHelper.ThrowIfNotOnUIThread();
            this.formatter = new MessageTemplateTextFormatter(outputTemplate);
            _ = ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(SPaneGuid, Constants.ExtensionName, 1, 1));
            _ = outputWindow.GetPane(SPaneGuid, out this.pane);
        }

        public void Emit(LogEvent logEvent) {
            StringWriter sw = new StringWriter();
            this.formatter.Format(logEvent, sw);
            string message = sw.ToString();

            ThreadHelper.ThrowIfNotOnUIThread();
            if (this.pane is IVsOutputWindowPaneNoPump noPump)
                noPump.OutputStringNoPump(message);
            else
                ErrorHandler.ThrowOnFailure(this.pane.OutputStringThreadSafe(message));

            if (logEvent.Level == LogEventLevel.Error)
                _ = this.pane.Activate();
        }
    }
}
