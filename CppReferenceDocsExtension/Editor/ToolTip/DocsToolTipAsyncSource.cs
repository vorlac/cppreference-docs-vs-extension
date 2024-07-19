using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;
using Serilog;

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

        public async Task<QuickInfoItem>
            GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken token) {
            SnapshotPoint? triggerPoint = session.GetTriggerPoint(this.textBuffer.CurrentSnapshot);
            if (triggerPoint == null)
                return await Task.FromResult<QuickInfoItem>(null).ConfigureAwait(false);

            ReadOnlyCollection<KeyValuePair<object, object>> props = session.Properties.PropertyList;
            foreach (KeyValuePair<object, object> prop in props) {
                string propsStr = prop.ToString();
            }

            ITextSnapshotLine lineSnapshot = triggerPoint.Value.GetContainingLine();
            ITrackingSpan lineSpan = this.textBuffer.CurrentSnapshot.CreateTrackingSpan(
                lineSnapshot.Extent,
                SpanTrackingMode.EdgeInclusive
            );

            SnapshotPoint val = triggerPoint.Value;
            string varStr = val.ToString();
            int diffToLineSpanStart = val.Difference(lineSnapshot.Start);
            int line = triggerPoint.Value.GetContainingLine().LineNumber;
            int pos = triggerPoint.Value.Position; // - triggerPoint.Value.GetContainingLine().Start;
            int start = triggerPoint.Value.GetContainingLine().Start;
            int end = triggerPoint.Value.GetContainingLine().End;
            char charAtTriggerPoint = triggerPoint.Value.GetChar();

            int startPos = lineSpan.GetStartPoint(this.textBuffer.CurrentSnapshot);
            int endPos = lineSpan.GetEndPoint(this.textBuffer.CurrentSnapshot);
            await EditorUtils.Package.JoinableTaskFactory.SwitchToMainThreadAsync();
            Document activeDocument = await EditorUtils.GetActiveDocumentAsync();
            DocumentLocation loc = new() {
                Filename = activeDocument.FullName,
                Line = line + 1,
                Column = pos
            };

            //EditorUtils.FindCodeElementAtLocationAsync(loc);
            List<NativeSymbol> codeElements = await EditorUtils.GetActiveDocumentCodeElementsAsync();

            IContentType contentType = this.textBuffer.ContentType;
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
                        $"{line + 1}"
                    )
                )
            );
            ContainerElement codeContainerElement = new(
                ContainerElementStyle.Wrapped,
                new ImageElement(DocsToolTipAsyncSource.Icon),
                new ClassifiedTextElement(
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Text,
                        contentType.DisplayName
                    ),
                    new ClassifiedTextRun(
                        PredefinedClassificationTypeNames.Identifier,
                        $"{line}"
                    )
                )
            );
            ContainerElement dateElm = new(
                ContainerElementStyle.Stacked,
                lineNumberElm,
                codeContainerElement,
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

            return await Task.FromResult(new QuickInfoItem(lineSpan, dateElm)).ConfigureAwait(false);
        }

        public void Dispose() {
            this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");
        }
    }
}
