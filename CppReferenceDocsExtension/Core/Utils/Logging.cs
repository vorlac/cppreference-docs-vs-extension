﻿using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Constants = CppReferenceDocsExtension.Core.Package.Constants;

namespace CppReferenceDocsExtension.Core.Utils
{
    internal sealed class Logging : ILogEventSink
    {
        private static Guid PaneGuid { get; } = new("DEADBEEF-FEEE-FEEE-CDCD-100000000000");

        private readonly IVsOutputWindowPane pane;
        private readonly ITextFormatter formatter;

        public Logging(IVsOutputWindow outputWindow, string outputTemplate) {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.formatter = new MessageTemplateTextFormatter(outputTemplate);

            ErrorHandler.ThrowOnFailure(
                outputWindow.CreatePane(
                    rguidPane: Logging.PaneGuid,
                    pszPaneName: Constants.ExtensionName,
                    fInitVisible: 1,
                    fClearWithSolution: 1
                )
            );

            outputWindow.GetPane(
                rguidPane: Logging.PaneGuid,
                ppPane: out this.pane
            );
        }

        public void Emit(LogEvent logEvent) {
            StringWriter sw = new();
            this.formatter.Format(logEvent: logEvent, output: sw);
            string message = sw.ToString();

            ThreadHelper.ThrowIfNotOnUIThread();
            if (this.pane is IVsOutputWindowPaneNoPump noPump)
                noPump.OutputStringNoPump(message);
            else
                ErrorHandler.ThrowOnFailure(this.pane.OutputStringThreadSafe(message));

            if (logEvent.Level == LogEventLevel.Error)
                this.pane.Activate();
        }
    }
}
