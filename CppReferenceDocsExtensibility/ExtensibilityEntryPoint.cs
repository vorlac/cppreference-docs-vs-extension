using Microsoft.VisualStudio.Extensibility;

namespace CppReferenceDocsExtensibility
{
    [VisualStudioContribution]
    public class OOPExtensibilityExtension : Extension
    {
        public override ExtensionConfiguration ExtensionConfiguration => new() {
            RequiresInProcessHosting = true,
        };
    }
}
