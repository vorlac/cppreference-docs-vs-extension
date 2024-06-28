using DocumentationProcessor.Properties;
using System.IO;
using System;
using System.Collections.Generic;

namespace DocumentationProcessor.Core {
    public static class Utils {
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

                string[] files = [];
                try {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex) {
                    Console.Error.WriteLine(ex);
                }

                foreach (string t in files)
                    yield return t;
            }
        }

        private static bool CreateDirectory(Uri dirPath) {
            string tempDownloadDir = dirPath.ToString();
            Console.WriteLine(@$"Creating directory: {tempDownloadDir}");
            Directory.CreateDirectory(tempDownloadDir);
            return Directory.Exists(tempDownloadDir);
        }

        public static bool ValidateDirectoryPath(Uri uri) {
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
    }
}
