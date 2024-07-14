using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DocumentationProcessor.Properties;

namespace DocumentationProcessor.Core
{
    internal static class Downloader
    {
        public static readonly Uri TempDataDir =
            new(Resources.TempDataDir ?? Path.GetTempPath());
        public static readonly Uri UserDataDir =
            new(Resources.UserDataDir);
        public static readonly Uri CppReferenceDocsMetadataFile =
            new(Path.Join(Resources.TempDataDir, Resources.CppRefDocsReleaseMetadata));
        public static readonly Uri CppReferenceDocsArchiveFile =
            new(Path.Join(Resources.TempDataDir, Resources.CppRefDocsArchive));
        public static readonly Uri CppReferenceDocsExtractPath =
            new(Path.Join(Resources.UserDataDir, Resources.CppRefDocsDir));

        public static readonly Uri CppReferenceDocReleasesUri =
            new(Resources.CppRefDocsReleasesAPI);

        public static bool DownloadContent(Uri downloadUri, Uri saveFileUri) {
            using HttpClient client = new() { DefaultRequestHeaders = { { "User-Agent", "none" } } };
            using Task<Stream> s = client.GetStreamAsync(downloadUri);
            using FileStream fs = new(saveFileUri.AbsolutePath, FileMode.OpenOrCreate);
            s.Result.CopyTo(fs);

            return File.Exists(saveFileUri.AbsolutePath);
        }
    }
}
