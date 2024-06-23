using System;
using System.ComponentModel;
using CppReferenceDocsExtension.Core.Utils;
using Serilog.Events;

namespace CppReferenceDocsExtension.Settings {
    public interface IWebBrowserSettings : INotifyPropertyChanged {
        string HomePage { get; set; }
        LogEventLevel MinimumLogLevel { get; set; }
        void Save();
        void Load();
    }

    internal static class WebBrowserSettingsExtensions {
        public static Uri GetHomePageUri(this IWebBrowserSettings settings) {
            return UriHelper.MakeUri(settings.HomePage);
        }
    }
}
