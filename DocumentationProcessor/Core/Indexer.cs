using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace DocumentationProcessor.Core {
    public static class Indexer {
        // TODO: move into a configurable settings manager
        private static readonly Uri DefaultDocsPath =
            Downloader.CppReferenceDocsExtractPath;

        public static readonly string[] IndexFiles = [
            Path.Join(
                DefaultDocsPath.AbsolutePath,
                @"cppreference-doxygen-local.tag.xml"
            ),
            Path.Join(
                DefaultDocsPath.AbsolutePath,
                @"cppreference-doxygen-web.tag.xml"
            )
        ];

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

        [Serializable]
        public class Compound {
            public class Member {
                public string Type { get; set; }
                public string Name { get; set; }
                public string AnchorFile { get; set; }
                public string Anchor { get; set; }
                public string ArgList { get; set; }
            }

            public class Class {
                public string Type { get; set; }
                public string Name { get; set; }
            }

            public string Type { get; set; }
            public string Name { get; set; }
            public string FileName { get; set; }
            public string NameSpace { get; set; }
            public List<Member> Members { get; set; } = [];
            public List<Class> Classes { get; set; } = [];

            public void Print() {
                Console.WriteLine(@"{");
                Console.WriteLine(@$"  Name: {this.Name}");
                Console.WriteLine(@$"  Type: {this.Type}");
                Console.WriteLine(@$"  FileName: {this.FileName}");
                Console.WriteLine(@$"  NameSpace: {this.NameSpace}");

                if (this.Members.Count > 0) {
                    Console.WriteLine(@"  Members: [");
                    foreach (Member member in this.Members) {
                        Console.WriteLine(@"    {");
                        Console.WriteLine(@$"      Type: {member.Type}");
                        Console.WriteLine(@$"      Name: {member.Name}");
                        Console.WriteLine(@$"      ArgList: {member.ArgList}");
                        Console.WriteLine(@$"      AnchorFile: {member.AnchorFile}");
                        Console.WriteLine(@$"      Anchor: {member.Anchor}");
                        Console.WriteLine(@"    },");
                    }

                    Console.WriteLine(@"  ],");
                }

                if (this.Classes.Count > 0) {
                    Console.WriteLine(@"  Classes: [");
                    foreach (Class classTag in this.Classes) {
                        Console.WriteLine(@"    {");
                        Console.WriteLine(@$"      Type: {classTag.Type}");
                        Console.WriteLine(@$"      Name: {classTag.Name}");
                        Console.WriteLine(@"    },");
                    }

                    Console.WriteLine(@"  ]");
                }

                Console.WriteLine(@"},");
            }
        };

        public static async IAsyncEnumerable<Compound> ParseCppReferenceIndexTags() {
            foreach (string filePath in IndexFiles) {
                FileStream stream = new(filePath, FileMode.Open);
                IAsyncEnumerable<XElement> compoundElements = StreamElementsAsync(stream, "compound");
                await foreach (XElement elem in compoundElements) {
                    Compound compoundTag = new();
                    compoundTag.Name = elem.Element("name")?.Value;
                    compoundTag.Type = elem.Attribute("kind")?.Value;
                    compoundTag.FileName = elem.Element("filename")?.Value;
                    compoundTag.NameSpace = elem.Element("namespace")?.Value;

                    IEnumerable<XElement> memberElements = elem.Elements("member");
                    foreach (XElement memberElem in memberElements) {
                        Compound.Member memberTag = new();
                        memberTag.Type = memberElem.Attribute(@"kind")?.Value;
                        memberTag.Anchor = memberElem.Element(@"anchor")?.Value;
                        memberTag.Name = memberElem.Element(@"name")?.Value;
                        memberTag.AnchorFile = memberElem.Element(@"anchorfile")?.Value;
                        memberTag.Name = memberElem.Element(@"name")?.Value;
                        memberTag.ArgList = memberElem.Element(@"arglist")?.Value;
                        compoundTag.Members.Add(memberTag);
                    }

                    IEnumerable<XElement> classElements = elem.Elements("class");
                    foreach (XElement classElem in classElements) {
                        Compound.Class classTag = new();
                        classTag.Type = classElem.Attribute("kind")?.Value;
                        classTag.Name = classElem.Value;
                        compoundTag.Classes.Add(classTag);
                    }

                    yield return compoundTag;
                }
            }
        }

        public static async IAsyncEnumerable<XElement> StreamElementsAsync(Stream stream, string matchName) {
            XmlReaderSettings settings = new() {
                Async = true,
                IgnoreWhitespace = true
            };

            using XmlReader reader = XmlReader.Create(stream, settings);
            while (await reader.ReadAsync()) {
                while (reader.ReadToFollowing(matchName))
                    if (XNode.ReadFrom(reader) is XElement elem)
                        yield return elem;
            }
        }
    }
}
