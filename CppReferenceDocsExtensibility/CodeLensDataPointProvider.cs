using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace CppReferenceDocsExtensibility
{
    using CodeLensContext = CodeLensDescriptorContext;

    [Name(CodeLensDataPointProvider.Id)] [ContentType("code")]
    [Export(typeof(IAsyncCodeLensDataPointProvider))] [Priority(210)]
    [LocalizedName(type: typeof(Resources.Strings), CodeLensDataPointProvider.Id)]
    public class CodeLensDataPointProvider : IAsyncCodeLensDataPointProvider
    {
        private const string Id = "CustomCodeLensProvider";

        public Task<bool> CanCreateDataPointAsync(CodeLensDescriptor desc, CodeLensContext ctx, CancellationToken tok) {
            bool methodsOnly = desc.Kind == CodeElementKinds.Method;
            return Task.FromResult(methodsOnly);
        }

        public Task<IAsyncCodeLensDataPoint> CreateDataPointAsync(
            CodeLensDescriptor descriptor, CodeLensContext context, CancellationToken token) {
            return Task.FromResult<IAsyncCodeLensDataPoint>(new CodeLensDataPoint(descriptor));
        }
    }
}
