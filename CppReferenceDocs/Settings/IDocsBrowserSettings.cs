using System;
using System.ComponentModel;
using CppReferenceDocsExtension.Utils;
using Serilog.Events;

namespace CppReferenceDocsExtension.Settings
{
    public interface IDocsBrowserSettings : INotifyPropertyChanged
    {
        string HomePage { get; set; }
        LogEventLevel MinimumLogLevel { get; set; }
        void Save();
        void Load();
    }

    internal static class CppReferenceDocsSettingsExtensions
    {
        public static Uri GetHomePageUri(this IDocsBrowserSettings settings) => UriHelper.MakeUri(settings.HomePage);
    }
}
