using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace CppReferenceDocsExtension.Editor {
    [Export(typeof(IAsyncQuickInfoSourceProvider))]
    [Name("Line Async Quick Info Provider")] [Order]
    [ContentType("any")] [SupportsStandaloneFiles(true)]
    internal sealed class DocsToolTipAsyncSourceProvider : IAsyncQuickInfoSourceProvider {
        public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) {
            return textBuffer.Properties.GetOrCreateSingletonProperty(
                () => new DocsToolTipAsyncSource(textBuffer)
            );
        }
    }
}
