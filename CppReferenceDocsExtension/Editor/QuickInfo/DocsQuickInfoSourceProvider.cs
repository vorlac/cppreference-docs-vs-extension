using System.ComponentModel.Composition;
using CppReferenceDocsExtension.Core.Package;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Serilog;

namespace CppReferenceDocsExtension.Editor.QuickInfo
{
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name(nameof(DocsQuickInfoSourceProvider))] [Order(After = "Default")]
    [ContentType(Constants.Content.CPlusPlus)] [SupportsStandaloneFiles(true)]
    internal sealed class DocsQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
    {
        [Import]
        internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        [Import]
        private IBufferTagAggregatorFactoryService AggService { get; set; }

        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new DocsQuickInfoSource(
                    this,
                    textBuffer as ITextBuffer2,
                    this.AggService.CreateTagAggregator<ClassificationTag>(textBuffer)
                )
            );
        }
    }
}
