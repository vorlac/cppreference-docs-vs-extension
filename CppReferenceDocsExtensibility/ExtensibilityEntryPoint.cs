using Microsoft.VisualStudio.Extensibility;

namespace CppReferenceDocsExtensibility
{
    [VisualStudioContribution]
    public class ExtensibilityEntryPoint : Extension
    {
        public override ExtensionConfiguration ExtensionConfiguration => new() {
            RequiresInProcessHosting = true,
        };
    }
}
