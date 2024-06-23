using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentationProcessor.Properties;

namespace DocumentationProcessor.Core {
    internal static class Downloader {
        // TODO: move default paths into a configurable settings manager
        public static readonly Uri TempDataDir = new(Path.GetTempPath());
        public static readonly Uri UserDataDir = new(@"C:/temp/");

        public static readonly Uri CppReferenceDocsMetadataFile =
            new(Path.Join(TempDataDir.AbsolutePath, @"cppref-docs-releases.json"));
        public static readonly Uri CppReferenceDocsArchiveFile =
            new(Path.Join(TempDataDir.AbsolutePath, @"cppref-docs-html-book.zip"));
        public static readonly Uri CppReferenceDocsExtractPath =
            new(Path.Join(UserDataDir.AbsolutePath, @"cppreference-docs/"));

        public static readonly Uri CppReferenceDocReleasesUri =
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

            if (Directory.Exists(uri.AbsolutePath))
                Console.WriteLine(Resources.DownloadDir, uri.ToString());
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

        public static bool DownloadContent(Uri downloadUri, Uri savefileUri) {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "none");

            using Task<Stream> s = client.GetStreamAsync(downloadUri);
            using FileStream fs = new(savefileUri.AbsolutePath, FileMode.OpenOrCreate);
            s.Result.CopyTo(fs);

            return File.Exists(savefileUri.AbsolutePath);
        }
    }
}
