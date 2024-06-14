using CppReferenceDocs.ToolWindows;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;

namespace CppReferenceDocs.Commands
{
    [VisualStudioContribution]
    public class ToolWindow1Command : Command
    {
        public override CommandConfiguration CommandConfiguration =>
            new(displayName: "%CppReferenceDocs.ShowDocsToolWindowCommand.DisplayName%")
            {
                // Use this object initializer to set optional parameters for the command. The required parameter,
                // displayName, is set above. To localize the displayName, add an entry in .vsextension\string-resources.json
                // and reference it here by passing "%CppReferenceDocs.ToolWindow1Command.DisplayName%" as a constructor parameter.
                Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
                Icon = new CommandIconConfiguration(
                    ImageMoniker.KnownValues.Extension,
                    IconSettings.IconAndText
                )
            };

        public override async Task ExecuteCommandAsync(
            IClientContext context,
            CancellationToken cancellationToken)
        {
            await Extensibility.Shell().ShowToolWindowAsync<CppReferenceToolWindow>(activate: true, cancellationToken);
        }
    }
}
