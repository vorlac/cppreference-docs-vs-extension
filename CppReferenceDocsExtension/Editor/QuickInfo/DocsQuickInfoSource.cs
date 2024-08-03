using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CppReferenceDocsExtension.Core;
using CppReferenceDocsExtension.Core.Lang;
using CppReferenceDocsExtension.Core.Utils;
using Microsoft;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace CppReferenceDocsExtension.Editor.QuickInfo
{
    using Images = KnownMonikers;

    internal sealed class DocsQuickInfoSource : IAsyncQuickInfoSource
    {
        private bool disposed = false;
        private readonly Mutex mutex = new();
        private static readonly ImageId CubeIcon = Images.AbstractCube.ToImageId();
        private static readonly ImageId MSIcon = Images.Microsoft.ToImageId();

        private ITextView2 textView;
        private readonly SnapshotSpan? currentWord;
        private readonly ITagAggregator<ClassificationTag> aggregator;
        private IAsyncQuickInfoSourceProvider quickInfoProvider;
        private readonly ITextBuffer2 textBuffer;
        private SnapshotPoint requestedPoint;

        public DocsQuickInfoSource(
            IAsyncQuickInfoSourceProvider provider, ITextBuffer2 textBuffer,
            ITagAggregator<ClassificationTag> aggregator) {
            this.quickInfoProvider = provider;
            this.textBuffer = textBuffer;
            this.aggregator = aggregator;
            this.currentWord = null;
            this.textView = null;
        }

        public void Dispose() {
            if (!this.disposed) {
                lock (this.mutex) {
                    this.disposed = true;
                }
            }
        }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session, CancellationToken token) {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
                return await Task.FromResult<QuickInfoItem>(null);

            if (this.textView == null && session.TextView is ITextView2) {
                this.textView = session.TextView as ITextView2;
                Assumes.NotNull(this.textView);
                //toolTipProvider.NavigatorService.GetTextStructureNavigator(textBuffer);
                // hook up event handlers for layout changes and cursor updates
                this.textView.Caret.PositionChanged += this.CaretPositionChanged;
                this.textView.LayoutChanged += this.ViewLayoutChanged;
            }

            ITextSnapshotLine lineSnapshot = triggerPoint.Value.GetContainingLine();
            ITrackingSpan lineSpan = this.textBuffer.CurrentSnapshot.CreateTrackingSpan(
                lineSnapshot.Extent,
                SpanTrackingMode.EdgeInclusive
            );

            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            int line = triggerPoint.Value.GetContainingLine().LineNumber + 1;

            string test = "";
            List<NativeSymbol> codeElements = await EditorUtils.GetActiveDocumentCodeElementsAsync();
            foreach (NativeSymbol elem in codeElements)
                test += $"{elem.ToStringRecursive()}";

            IContentType contentType = this.textBuffer.ContentType;
            ContainerElement lineNumberElm = new(
                ContainerElementStyle.Wrapped,
                new ImageElement(DocsQuickInfoSource.MSIcon),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Keyword,
                        "Line number: "
                    ),
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Identifier,
                        $"{line}"
                    )
                )
            );

            ContainerElement codeContainer = new(
                ContainerElementStyle.Wrapped,
                new ImageElement(DocsQuickInfoSource.CubeIcon),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Text,
                        contentType.DisplayName
                    ),
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Identifier,
                        $"{test}"
                    )
                )
            );

            ContainerElement dateContainer = new(
                ContainerElementStyle.Stacked,
                lineNumberElm,
                codeContainer,
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

            return await Task.FromResult(
                new QuickInfoItem(lineSpan, dateContainer)
            );
        }

        private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e) {
            // ignore if we didn't get a change in snapshot
            if (e.NewViewState.EditSnapshot != e.OldViewState.EditSnapshot)
                this.UpdateAtCaretPosition(this.textView.Caret.Position);
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e) {
            this.UpdateAtCaretPosition(e.NewPosition);
        }

        private void UpdateAtCaretPosition(CaretPosition pos) {
            SnapshotPoint? point = pos.Point.GetPoint(this.textBuffer, pos.Affinity);
            if (point.HasValue) {
                // If the new cursor position is still within the current
                // word (and on the same snapshot) we don't need to check it
                if (this.currentWord.HasValue
                 && this.currentWord.Value.Snapshot == this.textView.TextSnapshot
                 && point.Value >= this.currentWord.Value.Start
                 && point.Value <= this.currentWord.Value.End) {
                    return;
                }

                this.requestedPoint = point.Value;
            }
        }
    }
}
