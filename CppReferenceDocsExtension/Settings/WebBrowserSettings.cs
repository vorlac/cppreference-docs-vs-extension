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

namespace CppReferenceDocsExtension.Settings {
    [Export(typeof(IWebBrowserSettings))]
    public sealed class WebBrowserSettings : IWebBrowserSettings {
        private const string DefaultHomePage = @"https://www.cppreference.com/";
        private const string SettingsKey = nameof(WebBrowserSettings);

        private readonly ILogger log = Log.Logger;

        private readonly WritableSettingsStore settingsStore;
        private LogEventLevel minimumLogLevel;
        private string homePage;

        [ImportingConstructor]
        public WebBrowserSettings(SVsServiceProvider vsServiceProvider) {
            try {
                var manager = new ShellSettingsManager(vsServiceProvider);
                this.settingsStore = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
                if (this.settingsStore == null) {
                    this.log.Error(
                        $"{nameof(WebBrowserSettings)} Constructor: "
                      + $"could not retrieve an instance of {nameof(WritableSettingsStore)}"
                    );
                }
            }
            catch (Exception ex) {
                this.log.Error(
                    ex,
                    $"{nameof(WebBrowserSettings)} Constructor: "
                  + $"could not retrieve an instance of {nameof(WritableSettingsStore)}"
                );
            }

            this.homePage = DefaultHomePage;
            this.minimumLogLevel = LogEventLevel.Verbose;
            this.Load();
        }

        public string HomePage {
            get => this.homePage;
            set => this.Set(ref this.homePage, value);
        }

        public LogEventLevel MinimumLogLevel {
            get => this.minimumLogLevel;
            set => this.Set(ref this.minimumLogLevel, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void Load() {
            try {
                this.HomePage = this.settingsStore.GetString(SettingsKey, nameof(this.HomePage), DefaultHomePage) ?? "";
                int logEventLevel = this.settingsStore.GetInt32(
                    SettingsKey,
                    nameof(this.MinimumLogLevel),
                    (int)LogEventLevel.Information
                );

                this.MinimumLogLevel = (LogEventLevel)logEventLevel;
            }
            catch (Exception ex) {
                this.log.Error(ex, $"{nameof(WebBrowserSettings)}: Failed to load settings");
            }
        }

        public void Save() {
            try {
                if (!this.settingsStore.CollectionExists(SettingsKey))
                    this.settingsStore.CreateCollection(SettingsKey);

                this.settingsStore.SetString(SettingsKey, nameof(this.HomePage), this.HomePage ?? "");
                this.settingsStore.SetInt32(SettingsKey, nameof(this.MinimumLogLevel), (int)this.MinimumLogLevel);
            }
            catch (Exception ex) {
                this.log.Error(ex, $"{nameof(WebBrowserSettings)}: Failed to save settings");
            }
        }

        private bool Set<T>(ref T target, T value, [CallerMemberName] string propName = null) {
            if (EqualityComparer<T>.Default.Equals(target, value))
                return false;

            target = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            return true;
        }
    }
}
