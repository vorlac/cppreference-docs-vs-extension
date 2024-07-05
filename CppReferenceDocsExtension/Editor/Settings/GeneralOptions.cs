using System;
using System.ComponentModel;
using CppReferenceDocsExtension.Core.Utils;
using Serilog.Events;

namespace CppReferenceDocsExtension.Editor.Settings
{
    internal class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        [Category("General")]
        [DisplayName("Logging Level")]
        [Description("Controls the extension's logging verbosity")]
        [TypeConverter(typeof(EnumConverter))]
        [DefaultValue(LogEventLevel.Verbose)]
        public LogEventLevel MinimumLoggingLevel { get; set; } = LogEventLevel.Verbose;

        [Category("Browser Panel")]
        [DisplayName("Docs Browser Home Page")]
        [Description("Defines the default home page for the docs browser (when browser mode is enabled)")]
        [DefaultValue("https://www.cppreference.com")]
        public string HomePage { get; set; } = "https://www.cppreference.com";
    }

    internal static class WebBrowserSettingsExtensions
    {
        public static Uri GetHomePageUri(this GeneralOptions settings) {
            return UriHelper.MakeUri(settings.HomePage);
        }
    }
}
