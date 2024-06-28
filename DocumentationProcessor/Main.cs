using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.IO.Compression;
using DocumentationProcessor.Core;
using DocumentationProcessor.Properties;

namespace DocumentationProcessor {
    internal static class Program {
        private static void Main() {
            Uri docsDownloadUri = null;

            Uri downloadDirPath = Downloader.TempDataDir;
            Uri docsReleasesUri = Downloader.CppReferenceDocReleasesUri;
            Uri cppDocsMetadata = Downloader.CppReferenceDocsMetadataFile;
            Uri cppDocsArchive = Downloader.CppReferenceDocsArchiveFile;
            Uri userDataDirPath = Downloader.CppReferenceDocsExtractPath;

            bool download = false;
            // download offline archive of cppreference.com
            // TODO:
            //   1, Move logic below into Downloader class
            //   2. Error handling
            //   3. Path normalization
            //   4. configurable temp & user data paths
            if (download && !Directory.Exists(Resources.UserDataDir)) {
                if (!Utils.ValidateDirectoryPath(downloadDirPath))
                    Console.WriteLine($@"Failed to validate temp data dir: {downloadDirPath.AbsolutePath}");
                else if (!Downloader.DownloadContent(docsReleasesUri, cppDocsMetadata))
                    Console.WriteLine(@"Failed to download GitHub releases metadata for cppreference.com docs");
                else {
                    string docsReleasesMetadata = File.ReadAllText(cppDocsMetadata.AbsolutePath);
                    JsonDocument docsReleasesJson = JsonDocument.Parse(docsReleasesMetadata);
                    JsonElement? latestReleaseJson = docsReleasesJson?.RootElement[0];
                    JsonElement? releaseInfo = latestReleaseJson?.GetProperty("assets");
                    foreach (JsonElement elem in releaseInfo?.EnumerateArray()) {
                        string name = elem.GetProperty("name").ToString();
                        if (name.StartsWith("html-book") && name.EndsWith(".zip")) {
                            string downloadURL = elem.GetProperty("browser_download_url").ToString();
                            docsDownloadUri = new Uri(downloadURL);
                            if (!docsDownloadUri.IsAbsoluteUri)
                                Console.WriteLine(@"Failed to parse latest release assets json metadata");

                            break;
                        }
                    }
                }

                if (docsDownloadUri == null)
                    Console.WriteLine(@"Failed to retrieve download URL for cppreference offline archive");
                else if (!Downloader.DownloadContent(docsDownloadUri, cppDocsArchive))
                    Console.WriteLine(@$"Failed to download offline archive of cppreference.com from {docsDownloadUri.AbsolutePath}");
                else {
                    Console.WriteLine(@$"Extracting: {cppDocsArchive.AbsolutePath} into {userDataDirPath.AbsolutePath}");
                    ZipFile.ExtractToDirectory(cppDocsArchive.AbsolutePath, userDataDirPath.AbsolutePath);
                    if (Directory.Exists(userDataDirPath.AbsolutePath))
                        Console.WriteLine(@$"Successfully extracted cppref docs into: {userDataDirPath.AbsolutePath}");
                }
            }

            Indexer indexer = new(Downloader.UserDataDir, Downloader.CppReferenceDocsExtractPath);
            indexer.BuildCppReferenceSymbolIndex();

            //Uri docsRootDirUri = Downloader.CppReferenceDocsExtractPath;
            //IEnumerable<string> cpprefFileList = Utils.GetFileListing(docsRootDirUri.AbsolutePath);
            //foreach (string filePath in cpprefFileList)
            //    Console.WriteLine(@$"{nameof(Program)} : {filePath}");
        }
    };
}
