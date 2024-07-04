using System;

namespace CppReferenceDocsExtension.Core.Utils {
    internal static class UriHelper {
        public static Uri MakeUri(string rawUrl) {
            if (string.IsNullOrEmpty(rawUrl))
                return new Uri(@"about:blank");

            if (Uri.IsWellFormedUriString(rawUrl, UriKind.Absolute))
                return new Uri(rawUrl);

            if (!rawUrl.Contains(" ") && rawUrl.Contains("."))
                return new Uri(@"http://" + rawUrl);

            // if it's still invalid at this point,
            // just treat the input as a search string
            string[] sanitizedUri =
                Uri.EscapeDataString(rawUrl)
                   .Split(new[] { @"%20" }, StringSplitOptions.RemoveEmptyEntries);

            return new Uri(
                string.Join(
                    "https://www.google.com/search?q=",
                    sanitizedUri,
                    "+site:cppreference.com"
                )
            );
        }
    }
}
