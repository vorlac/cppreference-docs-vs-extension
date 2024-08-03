using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace CppReferenceDocsExtension.Editor.Settings
{
    internal abstract class BaseOptionModel<T>
        where T : BaseOptionModel<T>, new()
    {
        private static readonly AsyncLazy<T> LiveModel
            = new(CreateAsync, ThreadHelper.JoinableTaskFactory);

        private static readonly AsyncLazy<ShellSettingsManager> SettingsManager
            = new(GetSettingsManagerAsync, ThreadHelper.JoinableTaskFactory);

        protected virtual string CollectionName { get; }
            = typeof(T).FullName;

        public static T Instance {
            get {
                ThreadHelper.ThrowIfNotOnUIThread();
                return ThreadHelper.JoinableTaskFactory.Run(GetLiveInstanceAsync);
            }
        }

        public static Task<T> GetLiveInstanceAsync() {
            return LiveModel.GetValueAsync();
        }

        public static async Task<T> CreateAsync() {
            T instance = new();
            await instance.LoadAsync();
            return instance;
        }

        public virtual void Load() {
            ThreadHelper.JoinableTaskFactory.Run(this.LoadAsync);
        }

        protected virtual async Task LoadAsync() {
            ShellSettingsManager manager = await SettingsManager.GetValueAsync();
            SettingsStore settingsStore =
                manager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(this.CollectionName)) {
                return;
            }

            foreach (PropertyInfo property in this.GetOptionProperties()) {
                try {
                    string serializedProp = settingsStore.GetString(
                        this.CollectionName,
                        property.Name
                    );
                    object value = this.DeserializeValue(serializedProp, property.PropertyType);
                    property.SetValue(this, value);
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.Write(ex);
                }
            }
        }

        public virtual void Save() {
            ThreadHelper.JoinableTaskFactory.Run(this.SaveAsync);
        }

        protected virtual async Task SaveAsync() {
            ShellSettingsManager manager = await SettingsManager.GetValueAsync();
            WritableSettingsStore settingsStore =
                manager.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!settingsStore.CollectionExists(this.CollectionName))
                settingsStore.CreateCollection(this.CollectionName);

            foreach (PropertyInfo property in this.GetOptionProperties()) {
                string output = this.SerializeValue(property.GetValue(this));
                settingsStore.SetString(this.CollectionName, property.Name, output);
            }

            T liveModel = await GetLiveInstanceAsync();
            if (this != liveModel)
                await liveModel.LoadAsync();
        }

        protected virtual string SerializeValue(object value) {
            using (MemoryStream stream = new()) {
                BinaryFormatter formatter = new();
                formatter.Serialize(stream, value);
                stream.Flush();
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        protected virtual object DeserializeValue(string value, Type type) {
            byte[] b = Convert.FromBase64String(value);

            using (MemoryStream stream = new(b)) {
                BinaryFormatter formatter = new();
                return formatter.Deserialize(stream);
            }
        }

        private static async Task<ShellSettingsManager> GetSettingsManagerAsync() {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            IVsSettingsManager svc = await AsyncServiceProvider.GlobalProvider.GetServiceAsync(
                typeof(SVsSettingsManager)
            ) as IVsSettingsManager;

            Assumes.Present(svc);
            return new(svc);
        }

        private IEnumerable<PropertyInfo> GetOptionProperties() {
            return this.GetType()
                       .GetProperties()
                       .Where(
                            p => p.PropertyType is {
                                IsSerializable: true, IsPublic: true
                            }
                        );
        }
    }
}
