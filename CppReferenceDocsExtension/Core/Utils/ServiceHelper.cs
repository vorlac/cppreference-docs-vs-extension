using System;
using CppReferenceDocsExtension.UI;
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
                T result = componentModel.GetService<T>();
                Assumes.Present(result);

                return result;
            }
            catch (Exception ex) {
                SLog.Error(
                    ex,
                    $"{nameof(WebBrowserOptionsPage)}: "
                  + $"Could not retrieve an instance of Service {typeof(T)}"
                );

                throw;
            }
        }
    }
}
