using System.ComponentModel;
using CppReferenceDocsExtension.Core.Converters;

namespace CppReferenceDocsExtension.Editor.Settings {
    public enum CppVersion {
        Cpp98,
        Cpp03,
        Cpp11,
        Cpp14,
        Cpp17,
        Cpp20,
        Cpp23,
        Cpp26,
        Latest
    }

    internal class DocumentationOptions : BaseOptionModel<DocumentationOptions> {
        [Category("Documentation")]
        [DisplayName("C/C++ Standard Version")]
        [Description("The C/C++ standard used by default when displaying cppreference documentation")]
        [TypeConverter(typeof(EnumConverter))]
        [DefaultValue(CppVersion.Latest)]
        public CppVersion CppVersion { get; set; } = CppVersion.Latest;

        [Category("My category")]
        [DisplayName("This is a boolean")]
        [Description("The description of the property")]
        [Browsable(false)]
        public bool HiddenProperty { get; set; } = true;
    }
}
