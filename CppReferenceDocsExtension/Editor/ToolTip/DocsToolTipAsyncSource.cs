using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace CppReferenceDocsExtension.Editor.ToolTip {
    internal sealed class DocsToolTipAsyncSource : IAsyncQuickInfoSource {
        private static readonly ImageId Icon = KnownMonikers.AbstractCube.ToImageId();
        private readonly ITextBuffer textBuffer;

        public DocsToolTipAsyncSource(ITextBuffer textBuffer) {
            this.textBuffer = textBuffer;
        }

        public Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session, CancellationToken cancellationToken) {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot);

            if (triggerPoint != null) {
                ITextSnapshotLine line = triggerPoint.Value.GetContainingLine();
                int lineNumber = triggerPoint.Value.GetContainingLine().LineNumber;
                ITrackingSpan lineSpan = this.textBuffer.CurrentSnapshot.CreateTrackingSpan(line.Extent, SpanTrackingMode.EdgeInclusive);

                var lineNumberElm = new ContainerElement(
                    ContainerElementStyle.Wrapped,
                    new ImageElement(Icon),
                    new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Keyword, "Line number: "),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Identifier, $"{lineNumber + 1}")
                    )
                );

                var dateElm = new ContainerElement(
                    ContainerElementStyle.Stacked,
                    lineNumberElm,
                    new ClassifiedTextElement(
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.SymbolDefinition, "The current date: "),
                        new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, DateTime.Now.ToShortDateString())
                    )
                );

                return Task.FromResult(new QuickInfoItem(lineSpan, dateElm));
            }

            return Task.FromResult<QuickInfoItem>(null);
        }

        // no cleanup needed
        public void Dispose() { }
    }
}
