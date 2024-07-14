using System;
using System.Collections;
using CppReferenceDocsExtension.Editor.Commands;
using CppReferenceDocsExtension.Editor.Settings;
using CppReferenceDocsExtension.Editor.ToolTip;
using CppReferenceDocsExtension.Editor.ToolWindow;
using static CppReferenceDocsExtension.Editor.ToolTip.DocsToolTipCustomization;

namespace CppReferenceDocsExtension.Core
{
    internal static class Constants
    {
        public const string ExtensionName = @"C/C++ Documentation Panel";

        public static string GUID(Type type) {
            const string typeName = nameof(Type);
            return typeName switch {
                nameof(ExtensionPackage)                       => "DEADBEEF-FEEE-FEEE-CDCD-000000000000",
                nameof(OpenDocsToolWindowCommand)              => "DEADBEEF-FEEE-FEEE-CDCD-000000000001",
                nameof(DialogPageProvider)                     => "DEADBEEF-FEEE-FEEE-CDCD-000000000002",
                nameof(DocsToolTipAsyncSourceProvider)         => "DEADBEEF-FEEE-FEEE-CDCD-000000000004",
                nameof(CompletionTooltipCustomizationProvider) => "DEADBEEF-FEEE-FEEE-CDCD-000000000008",

                null => throw new ArgumentNullException($"GUID lookup failed, {nameof(type)} is null"),
                _ => throw new ArgumentException(
                    $"GUID lookup failed, type {type.Name} missing from GUID map"
                ),
            };
        }
    }
}
