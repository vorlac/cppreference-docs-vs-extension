using System;
using Microsoft;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Serilog;

namespace CppReferenceDocsExtension.Core.Utils {
    internal static class ServiceHelper {
        private static readonly ILogger SLog = Log.Logger;

        public static T GetService<T>(this IServiceProvider sp) where T : class {
            try {
                ThreadHelper.ThrowIfNotOnUIThread();
                IComponentModel componentModel = sp.GetService<SComponentModel, IComponentModel>();
                var result = componentModel.GetService<T>();
                Assumes.Present(result);
                return result;
            }
            catch (Exception ex) {
                SLog.Error(
                    ex,
                    $"Could not retrieve an instance of Service {typeof(T)}"
                );

                throw;
            }
        }
    }
}
