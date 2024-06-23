using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace DocumentationProcessor.Core {
    public static class Indexer {
        // TODO: move into a configurable settings manager
        private static readonly Uri DefaultDocsPath =
            Downloader.CppReferenceDocsExtractPath;
        public static readonly string[] IndexFiles = {
            Path.Join(
                DefaultDocsPath.AbsolutePath,
                @"cppreference-doxygen-local.tag.xml"
            ),
            Path.Join(
                DefaultDocsPath.AbsolutePath,
                @"cppreference-doxygen-web.tag.xml"
            )
        };

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

        public static async Task ReadCppReferenceDocsTagsXml(Stream stream) {
            XmlReaderSettings settings = new() { Async = true };
            using XmlReader reader = XmlReader.Create(stream, settings);
            while (await reader.ReadAsync()) {
                switch (reader.NodeType) {
                    case XmlNodeType.None:
                    case XmlNodeType.Attribute:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.EntityReference:
                    case XmlNodeType.Entity:
                    case XmlNodeType.ProcessingInstruction:
                    case XmlNodeType.Comment:
                    case XmlNodeType.Document:
                    case XmlNodeType.DocumentType:
                    case XmlNodeType.DocumentFragment:
                    case XmlNodeType.Notation:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.EndEntity:
                    case XmlNodeType.XmlDeclaration:
                    default:
                        Console.WriteLine(@$"Other node {reader.NodeType} with value {reader.Value}");
                        break;
                    case XmlNodeType.Element:
                        Console.WriteLine(@$"Start Element {reader.Name}");
                        break;
                    case XmlNodeType.Text:
                        Console.WriteLine(@$"Text Node: {await reader.GetValueAsync()}");
                        break;
                    case XmlNodeType.EndElement:
                        Console.WriteLine(@$"End Element {reader.Name}");
                        break;
                }
            }
        }
    }
}
