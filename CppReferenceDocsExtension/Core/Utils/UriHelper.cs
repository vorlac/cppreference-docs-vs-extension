using System;

namespace CppReferenceDocsExtension.Core.Utils
{
    internal static class UriHelper
    {
        public static Uri MakeUri(string rawUrl) {
            if (string.IsNullOrEmpty(rawUrl))
                return new(@"about:blank");

            if (Uri.IsWellFormedUriString(rawUrl, UriKind.Absolute))
                return new(rawUrl);

            if (!rawUrl.Contains(" ") && rawUrl.Contains("."))
                return new($@"http://{rawUrl}");

            // if it's still invalid at this point,
            // just treat the input as a search string
            string[] sanitizedUri = Uri.EscapeDataString(rawUrl)
                                       .Split(separator: [@"%20"], StringSplitOptions.RemoveEmptyEntries);
            return new(
                $@"https://www.google.com/search?q={
                    string.Join(separator: "+", value: sanitizedUri)
                }+site:cppreference.com"
            );
        }
    }
}
