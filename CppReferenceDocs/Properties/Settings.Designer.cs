namespace CppReferenceDocs.Properties
{
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.10.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase
    {

        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));

        public static Settings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\temp\\")]
        public string WEBVIEW2_USER_DATA_FOLDER
        {
            get
            {
                return ((string)(this["WEBVIEW2_USER_DATA_FOLDER"]));
            }
            set
            {
                this["WEBVIEW2_USER_DATA_FOLDER"] = value;
            }
        }
    }
}
