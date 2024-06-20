using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Serilog;
using Serilog.Events;

namespace CppReferenceDocsExtension.Settings
{
    [Export(typeof(IWebBrowserSettings))]
    public sealed class WebBrowserSettings : IWebBrowserSettings
    {
        private const string DefaultHomePage = "https://www.cppreference.com/";
        private const string SettingsKey = nameof(WebBrowserSettings);

        private readonly ILogger _log = Log.Logger;
        private readonly WritableSettingsStore _settingsStore;

        [ImportingConstructor]
        public WebBrowserSettings(SVsServiceProvider vsServiceProvider)
        {
            try
            {
                ShellSettingsManager manager = new ShellSettingsManager(vsServiceProvider);
                _settingsStore = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
                if (_settingsStore == null)
                    _log.Error(
                        $"{nameof(WebBrowserSettings)} Constructor: could not retrieve an instance of {nameof(WritableSettingsStore)}");
            }
            catch (Exception ex)
            {
                _log.Error(ex,
                    $"{nameof(WebBrowserSettings)} Constructor: could not retrieve an instance of {nameof(WritableSettingsStore)}");
            }

            // Defaults
            _homePage = DefaultHomePage;
            _minimumLogLevel = LogEventLevel.Verbose;

            Load();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string _homePage;

        public string HomePage
        {
            get => _homePage;
            set => Set(ref _homePage, value);
        }

        private LogEventLevel _minimumLogLevel;

        public LogEventLevel MinimumLogLevel
        {
            get => _minimumLogLevel;
            set => Set(ref _minimumLogLevel, value);
        }

        public void Load()
        {
            try
            {
                // NB: the stored url can be intentionally empty, meaning the user wished to open the browser with a blank page
                HomePage = _settingsStore.GetString(SettingsKey, nameof(HomePage), DefaultHomePage) ?? "";
                MinimumLogLevel = (LogEventLevel)_settingsStore.GetInt32(SettingsKey, nameof(MinimumLogLevel),
                    (int)LogEventLevel.Information);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"{nameof(WebBrowserSettings)}: Failed to load settings");
            }
        }

        public void Save()
        {
            try
            {
                if (!_settingsStore.CollectionExists(SettingsKey))
                    _settingsStore.CreateCollection(SettingsKey);

                _settingsStore.SetString(SettingsKey, nameof(HomePage), HomePage ?? "");
                _settingsStore.SetInt32(SettingsKey, nameof(MinimumLogLevel), (int)MinimumLogLevel);
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"{nameof(WebBrowserSettings)}: Failed to save settings");
            }
        }

        private bool Set<T>(ref T target, T value, [CallerMemberName] string propName = null)
        {
            if (EqualityComparer<T>.Default.Equals(target, value)) return false;
            target = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            return true;
        }
    }
}
