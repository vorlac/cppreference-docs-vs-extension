using Microsoft.VisualStudio.Language.CodeLens.Remoting;
using Microsoft.VisualStudio.Language.CodeLens;
using Microsoft.VisualStudio.Threading;

namespace CppReferenceDocsExtensibility
{
    public class CodeLensDataPoint(CodeLensDescriptor descriptor) : IAsyncCodeLensDataPoint
    {
        public Task<CodeLensDataPointDescriptor> GetDataAsync(
            CodeLensDescriptorContext context, CancellationToken token) {
            return Task.FromResult(
                result: new CodeLensDataPointDescriptor {
                    Description = "Shows Up Inline",
                    //ImageId = Shows an image next to the Code Lens entry
                    //IntValue = I haven't figured this one out yet!
                    TooltipText = "Shows Up On Hover"
                }
            );
        }

        public Task<CodeLensDetailsDescriptor> GetDetailsAsync(
            CodeLensDescriptorContext context, CancellationToken token) {
            // this is what gets triggered when you click a Code Lens entry
            return Task.FromResult<CodeLensDetailsDescriptor>(null);
        }

        public CodeLensDescriptor Descriptor { get; } = descriptor;
    #pragma warning disable CS0067
        public event AsyncEventHandler InvalidatedAsync;
    #pragma warning restore CS0067
    }
}
