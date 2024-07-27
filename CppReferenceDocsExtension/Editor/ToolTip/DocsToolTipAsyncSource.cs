using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace CppReferenceDocsExtension.Editor.ToolTip
{
    public class CodeElementTag : TextMarkerTag
    {
        // TODO: unecessary?.. remove?
        public CodeElementTag() : base("test") { }
    }

    internal sealed class DocsToolTipAsyncSource
        : IAsyncQuickInfoSource, ITagger<CodeElementTag>
    {
        private static ImageId Icon { get; } =
            KnownMonikers.AbstractCube.ToImageId();

        private readonly object mutex = new();
        private ITextBuffer2 TextBuffer { get; set; }
        private ITextView2 TextView { get; set; }
        private SnapshotSpan? CurrentWord { get; set; }
        private SnapshotPoint RequestedPoint { get; set; }
        private Document CurrentDocument { get; set; }

        public DocsToolTipAsyncSource(ITextBuffer2 textBuffer) {
            this.CurrentWord = null;
            this.CurrentDocument = null;
            this.TextBuffer = textBuffer;
            this.TextView = null;
        }

        public void Dispose() { }

        public async Task<QuickInfoItem> GetQuickInfoItemAsync(
            IAsyncQuickInfoSession session, CancellationToken token) {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(this.TextBuffer.CurrentSnapshot);
            if (!triggerPoint.HasValue)
                return await Task.FromResult<QuickInfoItem>(null);

            if (this.TextView == null && session.TextView is ITextView2) {
                this.TextView = session.TextView as ITextView2;
                // hook up event handlers for layout changes and cursor updates
                this.TextView.Caret.PositionChanged += this.CaretPositionChanged;
                this.TextView.LayoutChanged += this.ViewLayoutChanged;
            }

            ITextSnapshotLine lineSnapshot = triggerPoint.Value.GetContainingLine();
            ITrackingSpan lineSpan = this.TextBuffer.CurrentSnapshot.CreateTrackingSpan(
                lineSnapshot.Extent,
                SpanTrackingMode.EdgeInclusive
            );

            int line = triggerPoint.Value.GetContainingLine().LineNumber + 1;
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            List<NativeSymbol> codeElements = await EditorUtils.GetActiveDocumentCodeElementsAsync();

            IContentType contentType = this.TextBuffer.ContentType;
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
                        $"{line}"
                    )
                )
            );

            ContainerElement codeContainer = new(
                ContainerElementStyle.Wrapped,
                new ImageElement(DocsToolTipAsyncSource.Icon),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Text,
                        contentType.DisplayName
                    ),
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Identifier,
                        $"{this.CurrentWord}"
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
                this.UpdateAtCaretPosition(this.TextView.Caret.Position);
        }

        private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e) {
            this.UpdateAtCaretPosition(e.NewPosition);
        }

        private void UpdateAtCaretPosition(CaretPosition pos) {
            SnapshotPoint? point = pos.Point.GetPoint(this.TextBuffer, pos.Affinity);
            if (point.HasValue) {
                // If the new cursor position is still within the current
                // word (and on the same snapshot) we don't need to check it
                if (this.CurrentWord.HasValue
                 && this.CurrentWord.Value.Snapshot == this.TextView.TextSnapshot
                 && point.Value >= this.CurrentWord.Value.Start
                 && point.Value <= this.CurrentWord.Value.End) {
                    return;
                }

                this.RequestedPoint = point.Value;
                ThreadPool.QueueUserWorkItem(UpdateWordAdornments);
            }
        }

        class DebugTestDebugTestFixme : ITextStructureNavigator
        {
            public TextExtent GetExtentOfWord(SnapshotPoint currentPosition) {
                throw new NotImplementedException();
            }

            public SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan) {
                throw new NotImplementedException();
            }

            public SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan) {
                throw new NotImplementedException();
            }

            public SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan) {
                throw new NotImplementedException();
            }

            public SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan) {
                throw new NotImplementedException();
            }

            public IContentType ContentType { get; set; }
        }

        private ITextStructureNavigator TextStructureNavigator { get; set; } = new DebugTestDebugTestFixme();
        private NormalizedSnapshotSpanCollection WordSpans { get; set; }
        private ITextSearchService TextSearchService { get; set; }

        private void UpdateWordAdornments(object threadContext) {
            SnapshotPoint currentRequest = this.RequestedPoint;
            List<SnapshotSpan> wordSpans = [];
            bool foundWord = true;

            //
            //
            // TODO: figure out where to get an instance of TextStructureNavigator
            //       or see if it's easier to just scan the text span manually
            //
            // Find all words in the buffer like the one the caret is on
            TextExtent word = TextStructureNavigator.GetExtentOfWord(currentRequest);

            // If we've selected something not worth highlighting,
            // we might have missed a "word" by a little bit
            if (!DocsToolTipAsyncSource.WordExtentIsValid(currentRequest, word)) {
                // Before we retry, make sure it is worthwhile
                if (word.Span.Start != currentRequest
                 || currentRequest == currentRequest.GetContainingLine().Start
                 || char.IsWhiteSpace((currentRequest - 1).GetChar())) {
                    foundWord = false;
                }
                else {
                    // Try again, one character previous.  If the caret is at the end
                    // of a word, then this will pick up the word we are at the end of.
                    word = this.TextStructureNavigator.GetExtentOfWord(currentRequest - 1);
                    // If we still aren't valid the second time around, we're done
                    if (!DocsToolTipAsyncSource.WordExtentIsValid(currentRequest, word))
                        foundWord = false;
                }
            }

            if (!foundWord) {
                // If we couldn't find a word,
                // clear out the existing markers
                this.SynchronousUpdate(currentRequest, [], null);
                return;
            }

            SnapshotSpan currentWord = word.Span;
            if (currentWord == CurrentWord)
                return;

            // Find the new spans
            FindData findData = new(currentWord.GetText(), currentWord.Snapshot) {
                FindOptions = FindOptions.WholeWord | FindOptions.MatchCase
            };

            wordSpans.AddRange(this.TextSearchService.FindAll(findData));

            // we are still up-to-date (another change
            // hasn't happened yet), do a real update
            if (currentRequest == this.RequestedPoint) {
                this.SynchronousUpdate(
                    currentRequest,
                    new(wordSpans),
                    currentWord
                );
            }
        }

        // Determine if a given "word" should be highlighted
        private static bool WordExtentIsValid(SnapshotPoint currentRequest, TextExtent word) {
            return word.IsSignificant
                && currentRequest.Snapshot.GetText(word.Span)
                                 .Any(c => char.IsLetter(c));
        }

        // Perform a synchronous update, in case multiple background threads are running
        private void SynchronousUpdate(
            SnapshotPoint currentRequest,
            NormalizedSnapshotSpanCollection newSpans,
            SnapshotSpan? newCurrentWord) {
            lock (mutex) {
                if (currentRequest != RequestedPoint)
                    return;

                WordSpans = newSpans;
                CurrentWord = newCurrentWord;

                EventHandler<SnapshotSpanEventArgs> tempEvent = TagsChanged;
                tempEvent?.Invoke(
                    this,
                    new SnapshotSpanEventArgs(
                        new SnapshotSpan(
                            this.TextBuffer.CurrentSnapshot,
                            0,
                            this.TextBuffer.CurrentSnapshot.Length
                        )
                    )
                );
            }
        }

        /// Find every instance of CurrentWord in the given span
        public IEnumerable<ITagSpan<CodeElementTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
            if (CurrentWord == null)
                yield break;

            // Hold on to a "snapshot" of the word spans and current word,
            // so that we maintain the same collection throughout
            SnapshotSpan currentWord = CurrentWord.Value;
            NormalizedSnapshotSpanCollection wordSpans = WordSpans;

            if (spans.Count == 0 || WordSpans.Count == 0)
                yield break;

            // If the requested snapshot isn't the same as the one our words are on, translate our spans
            // to the expected snapshot
            if (spans[0].Snapshot != wordSpans[0].Snapshot) {
                wordSpans = new NormalizedSnapshotSpanCollection(
                    wordSpans.Select(span => span.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive))
                );

                currentWord = currentWord.TranslateTo(spans[0].Snapshot, SpanTrackingMode.EdgeExclusive);
            }

            // First, yield back the word the cursor is under (if it overlaps)
            // Note that we'll yield back the same word again in the wordspans collection;
            // the duplication here is expected.
            if (spans.OverlapsWith(new NormalizedSnapshotSpanCollection(currentWord)))
                yield return new TagSpan<CodeElementTag>(currentWord, new CodeElementTag());

            // Second, yield all the other words in the file
            foreach (SnapshotSpan span in NormalizedSnapshotSpanCollection.Overlap(spans, wordSpans)) {
                yield return new TagSpan<CodeElementTag>(span, new CodeElementTag());
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
