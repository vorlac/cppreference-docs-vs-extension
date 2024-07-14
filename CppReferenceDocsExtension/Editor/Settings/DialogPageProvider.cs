using System;
using System.Runtime.InteropServices;
using CppReferenceDocsExtension.Core;
using stdole;

namespace CppReferenceDocsExtension.Editor.Settings
{
    [Guid(guid: DialogPageProvider.GUID)]
    internal abstract class DialogPageProvider
    {
        private const string GUID = "DEADBEEF-FEEE-FEEE-CDCD-000000000004";
        public class General : BaseOptionPage<GeneralOptions> { }
        public class Other : BaseOptionPage<DocumentationOptions> { }
    }
}
