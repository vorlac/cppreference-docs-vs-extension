using System.ComponentModel.Composition;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;
using Serilog;

namespace CppReferenceDocsExtension.Editor.ToolTip
{
    internal class DocsToolTipCustomization : TextBlock
    {
        private readonly ILogger log = Log.Logger;

        [Name(name: nameof(CompletionTooltipCustomizationProvider))]
        [Export(contractType: typeof(IUIElementProvider<QuickInfoItem, INavigableSymbolSource>))]
        [ContentType(name: "code")] /*[Order(Before = "RoslynToolTipProvider")]*/
        internal class CompletionTooltipCustomizationProvider
            : IUIElementProvider<QuickInfoItem, INavigableSymbolSource>
        {
            private readonly ILogger log = Log.Logger;

            public UIElement GetUIElement(QuickInfoItem item, INavigableSymbolSource context, UIElementType type) {
                this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");
                return type == UIElementType.Tooltip
                    ? new DocsToolTipCustomization(item)
                    : null;
            }
        }

        private DocsToolTipCustomization(QuickInfoItem completion) {
            this.log.Debug($"{this.GetType().Name}:{MethodBase.GetCurrentMethod()?.Name}");
            // Custom constructor enables us to modify the text values of the tooltip.
            // In this case, we are just modifying the font style and size
            this.Text = string.Format(
                CultureInfo.CurrentCulture,
                $"ZZZ {completion.ApplicableToSpan}"
            );

            this.FontSize = 24;
            this.FontStyle = FontStyles.Italic;

            string a = "";
            foreach (Inline v in this.Inlines)
                a = v.ToString();
        }
    }
}
