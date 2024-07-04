namespace CppReferenceDocsExtension.Editor.Settings {
    internal abstract class DialogPageProvider {
        public class General : BaseOptionPage<GeneralOptions> { }
        public class Other : BaseOptionPage<DocumentationOptions> { }
    }
}
