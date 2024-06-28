using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

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
