using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Serilog;

// IToolTipPresenter

namespace CppReferenceDocsExtension.Editor.ToolTip
{
    internal sealed class DocsToolTipAsyncSource : IAsyncQuickInfoSource
    {
        private readonly ILogger log = Log.Logger;
        private static readonly ImageId Icon = KnownMonikers.AbstractCube.ToImageId();
        private readonly ITextBuffer textBuffer;

        public DocsToolTipAsyncSource(ITextBuffer textBuffer) {
            this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");
            this.textBuffer = textBuffer;
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken token) {
            this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");

            SnapshotPoint? triggerPoint = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot);
            if (triggerPoint == null)
                return Task.FromResult<QuickInfoItem>(null);

            ITextSnapshotLine line = triggerPoint.Value.GetContainingLine();
            int lineNumber = triggerPoint.Value.GetContainingLine().LineNumber;
            ITrackingSpan lineSpan = this.textBuffer.CurrentSnapshot.CreateTrackingSpan(
                line.Extent,
                SpanTrackingMode.EdgeInclusive
            );

            IContentType contentType = this.textBuffer.ContentType;
            string aa = contentType.DisplayName;
            string bb = contentType.TypeName;
            foreach (IContentType a in contentType.BaseTypes) {
                string dn = a.DisplayName;
            }

            ContainerElement lineNumberElm = new(
                ContainerElementStyle.Wrapped,
                new ImageElement(DocsToolTipAsyncSource.Icon),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Keyword,
                        "Line number: "
                    ),
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Identifier,
                        $"{lineNumber + 1}"
                    )
                )
            );

            ContainerElement dateElm = new(
                ContainerElementStyle.Stacked,
                lineNumberElm,
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.SymbolDefinition,
                        "The current date: "
                    ),
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Comment,
                        DateTime.Now.ToShortDateString()
                    )
                )
            );

            return Task.FromResult(new QuickInfoItem(lineSpan, dateElm));
        }

        // no cleanup needed
        public void Dispose() {
            this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");
        }
    }
}
