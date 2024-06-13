using System.Diagnostics;
using Microsoft;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.Extensibility.Shell;

namespace CppReferenceDocs
{
    /// <summary>
    /// ShowDocsPanelCommand handler.
    /// </summary>
    [VisualStudioContribution]
    internal class ShowDocsPanelCommand : Command
    {
        private readonly TraceSource _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShowDocsPanelCommand"/> class.
        /// </summary>
        /// <param name="traceSource">Trace source instance to utilize.</param>
        public ShowDocsPanelCommand(TraceSource traceSource)
        {
            // This optional TraceSource can be used for logging in the command. You can use dependency injection to access
            // other services here as well.
            this._logger = Requires.NotNull(traceSource, nameof(traceSource));
        }

        public override CommandConfiguration CommandConfiguration =>
            new("%CppReferenceDocs.ShowDocsPanelCommand.DisplayName%")
            {
                // Use this object initializer to set optional parameters for the command. The required parameter,
                // displayName, is set above. DisplayName is localized and references an entry in .vsextension\string-resources.json.
                Icon = new(ImageMoniker.KnownValues.Extension, IconSettings.IconAndText),
                Placements = [CommandPlacement.KnownPlacements.ExtensionsMenu],
            };

        public override Task InitializeAsync(CancellationToken cancellationToken)
        {
            // Use InitializeAsync for any one-time setup or initialization.
            return base.InitializeAsync(cancellationToken);
        }

        public override async Task ExecuteCommandAsync(IClientContext context, CancellationToken cancellationToken)
        {
            await this.Extensibility.Shell().ShowPromptAsync("AAAAAAAAAAAAAA", PromptOptions.OK, cancellationToken);
        }
    }
}
