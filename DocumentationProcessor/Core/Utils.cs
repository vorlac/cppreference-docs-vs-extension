using System.IO;
using System;
using System.Collections.Generic;
using DocumentationProcessor.Properties;

namespace DocumentationProcessor.Core {
    public static class Utils {
        private static bool CreateDirectory(Uri dirPath) {
            string tempDownloadDir = dirPath.ToString();
            Console.WriteLine(@$"Creating directory: {tempDownloadDir}");
            Directory.CreateDirectory(tempDownloadDir);
            return Directory.Exists(tempDownloadDir);
        }

        public static IEnumerable<string> GetFileListing(string path) {
            Queue<string> queue = new();
            queue.Enqueue(path);

            while (queue.Count > 0) {
                path = queue.Dequeue();

                try {
                    string[] subDirs = Directory.GetDirectories(path);
                    foreach (string subDir in subDirs)
                        queue.Enqueue(subDir);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex);
                }

                string[] filePaths = Directory.GetFiles(path);
                foreach (string file in filePaths)
                    yield return file;
            }
        }

        public static bool ValidateDirectoryPath(Uri uri, bool createIfMissing = true) {
            if (uri.LocalPath.Length == 0) {
                Console.WriteLine(@$"[1] The download path isn't a valid local path: {uri.LocalPath}");
                Console.WriteLine(@$"[1] Setting temp download path to: {uri.LocalPath}");
            }

            if (Directory.Exists(uri.AbsolutePath))
                Console.WriteLine(Resources.TempDataDir, uri.AbsoluteUri);
            else if (createIfMissing) {
                Console.WriteLine(@$"[4] Creating Directory: {uri.LocalPath}");
                Console.WriteLine(
                    !CreateDirectory(uri)
                        ? @$"[5] Failed to create temp dir: {uri.AbsolutePath}"
                        : @$"[5] Successfully Created: {uri.LocalPath}"
                );
            }

            return Directory.Exists(uri.AbsolutePath);
        }
    }
}
