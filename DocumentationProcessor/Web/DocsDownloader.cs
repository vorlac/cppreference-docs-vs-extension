using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentationProcessor.Properties;

namespace DocumentationProcessor.Web {
    internal static class DocsDownloader {
        private static readonly Uri CppReferenceOfflineDocsUri =
            new(@"https://github.com/PeterFeicht/cppreference-doc/releases/latest");
        private static readonly Uri CppReferenceDocReleasesUri =
            new(@"https://api.github.com/repos/PeterFeicht/cppreference-doc/releases");

        public static bool ValidateDownloadDirectory(Uri uri) {
            if (uri.LocalPath.Length == 0) {
                uri = new Uri(Resources.DownloadDir);
                Console.WriteLine(@$"[1] The download path isn't a valid local path: {uri.LocalPath}");
                Console.WriteLine(@$"[1] Setting temp download path to: {uri.LocalPath}");
            }

            if (uri.LocalPath.Length == 0) {
                Console.WriteLine(@$"[2] The download path isn't a valid local path: {uri.LocalPath}");
                string path = Path.GetTempPath();
                uri = new Uri(path);
            }

            if (uri.LocalPath.Length == 0)
                Console.WriteLine(@$"[3] The download path isn't a valid local path: {uri.LocalPath}");

            if (!uri.IsAbsoluteUri) {
                string absPath = Path.Join(@"file://", uri.ToString());
                uri = new Uri(absPath);
            }

            if (Directory.Exists(uri.AbsolutePath)) {
                Console.WriteLine(Resources.DownloadDir, uri.ToString());
            }
            else {
                Console.WriteLine(@$"[4] Creating Directory: {uri.LocalPath}");
                Console.WriteLine(
                    !CreateDirectory(uri)
                        ? @$"[5] Failed to create temp dir: {uri.AbsolutePath}"
                        : @$"[5] Successfully Created: {uri.LocalPath}"
                );
            }

            return Directory.Exists(uri.AbsolutePath);
        }

        private static bool CreateDirectory(Uri dirPath) {
            string tempDownloadDir = dirPath.ToString();
            Console.WriteLine(@$"Creating directory: {tempDownloadDir}");
            Directory.CreateDirectory(tempDownloadDir);

            return Directory.Exists(tempDownloadDir);
        }

        public static void FetchCppRefDocs(Uri uri) {
            using HttpClient client = new HttpClient();
            using Task<Stream> s = client.GetStreamAsync(CppReferenceDocReleasesUri);
            using FileStream fs = new FileStream(uri.AbsolutePath + "cppref-docs-releases.txt", FileMode.OpenOrCreate);
            s.Result.CopyTo(fs);
        }
    }
}
