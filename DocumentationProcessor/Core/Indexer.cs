using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Data.SQLite;
using System.Data;
using System.Threading.Tasks;

namespace DocumentationProcessor.Core {
    public sealed class Indexer(Uri userDataDir, Uri cppRefDocsDir) {
        private SQLiteConnection Database { get; set; }
        private Uri UserDataDir { get; } = userDataDir;
        private Uri DocsRootDir { get; } = cppRefDocsDir;

        [Serializable]
        public class Compound {
            public class Member {
                public string Type { get; init; }
                public string Name { get; init; }
                public string AnchorFile { get; init; }
                public string Anchor { get; init; }
                public string ArgList { get; init; }

                public async void InsertTableRecord(SQLiteConnection connection, long parentID) {
                    string tableName = this.Type switch {
                        "function" => "functions",
                        "variable" => "variables",
                        _          => null
                    };

                    string insertStatement = $"""
                            insert into {tableName} (
                                name,
                                anchor_file,
                                anchor,
                                arg_list,
                                type,
                                parent_symbol
                            ) 
                            values (
                                '{this.Name}', 
                                '{this.AnchorFile}', 
                                '{this.Anchor}', 
                                '{this.ArgList}', 
                                '{this.Type}', 
                                '{parentID}'
                            );
                        """;

                    SQLiteCommand command = new(insertStatement, connection);
                    await command.ExecuteNonQueryAsync();
                }
            }

            public class Class {
                public string Type { get; init; }
                public string Name { get; init; }

                public async void InsertTableRecord(SQLiteConnection connection, long parentID) {
                    string insertStatement = $"""
                            insert into classes (
                                name, 
                                parent_symbol
                            ) 
                            values (
                                '{this.Name}', 
                                '{parentID}'
                            );
                        """;

                    SQLiteCommand command = new(insertStatement, connection);
                    await command.ExecuteNonQueryAsync();
                }
            }

            public string Type { get; init; }
            public string Name { get; init; }
            public string FileName { get; init; }
            public string NameSpace { get; init; }
            public List<Member> Members { get; } = [];
            public List<Class> Classes { get; } = [];

            public void InsertTableRecord(SQLiteConnection connection) {
                string insertStatement = $"""
                        insert into symbols (
                            type, 
                            filename, 
                            namespace
                        )
                        values (
                            '{this.Type}', 
                            '{this.FileName}', 
                            '{this.NameSpace}'
                        );
                    """;

                SQLiteCommand command = new(insertStatement, connection);
                if (command.ExecuteNonQuery() > 0) {
                    long symbolID = connection.LastInsertRowId;

                    foreach (Member member in this.Members)
                        member.InsertTableRecord(connection, symbolID);

                    foreach (Class member in this.Classes)
                        member.InsertTableRecord(connection, symbolID);
                }
            }

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

        public async void ParseAndIndexDocs() {
            if (this.InitIndexDatabase()) {
                Console.WriteLine(@$"Parsing and indexing offline archive of cppreference: {this.DocsRootDir}");
                SQLiteTransaction transaction = this.Database.BeginTransaction();
                await this.PopulateIndexDB(this.Database);
                transaction.Commit();
            }

            this.Database?.Close();
        }

        private bool InitIndexDatabase() {
            string dbPath = Path.Join(this.UserDataDir.AbsolutePath, "cppreference.db");
            string connectionString = @$"Data Source = {dbPath}; Version = 3;";

            try {
                SQLiteConnection.CreateFile(dbPath);
                this.Database = new SQLiteConnection(connectionString);
                this.Database.Open();
            }
            catch (Exception e) {
                Console.WriteLine(@$"Failed to create index database: {dbPath}");
                Console.WriteLine(@$"Error: {e.Message}");
                return false;
            }

            if (this.Database.State != ConnectionState.Open)
                throw new SQLiteException($"Failed to initialize cppreference index DB: {dbPath}");

            string sqlScriptPath = Path.Join(AppContext.BaseDirectory, @"SQL\DBSchema.sql");
            SQLiteCommand command = new(File.ReadAllText(sqlScriptPath), this.Database);
            command.ExecuteNonQuery();

            return true;
        }

        private async Task PopulateIndexDB(SQLiteConnection connection) {
            IAsyncEnumerable<Compound> tags = this.ParseCppReferenceIndexTags();
            await foreach (Compound tag in tags)
                tag.InsertTableRecord(connection);
        }

        private async IAsyncEnumerable<Compound> ParseCppReferenceIndexTags() {
            string indexFilePath = Path.Join(
                this.DocsRootDir.AbsolutePath,
                @"cppreference-doxygen-local.tag.xml"
            );

            FileStream stream = new(indexFilePath, FileMode.Open);
            IAsyncEnumerable<XElement> compoundElements = StreamElementsAsync(stream, "compound");
            await foreach (XElement elem in compoundElements) {
                Compound compoundTag = new() {
                    Name = elem.Element("name")?.Value,
                    Type = elem.Attribute("kind")?.Value,
                    FileName = elem.Element("filename")?.Value,
                    NameSpace = elem.Element("namespace")?.Value
                };

                IEnumerable<XElement> memberElements = elem.Elements("member");
                foreach (XElement memberElem in memberElements) {
                    Compound.Member memberTag = new() {
                        Type = memberElem.Attribute(@"kind")?.Value,
                        Anchor = memberElem.Element(@"anchor")?.Value,
                        Name = memberElem.Element(@"name")?.Value,
                        AnchorFile = memberElem.Element(@"anchorfile")?.Value,
                        ArgList = memberElem.Element(@"arglist")?.Value
                    };

                    compoundTag.Members.Add(memberTag);
                }

                IEnumerable<XElement> classElements = elem.Elements("class");
                foreach (XElement classElem in classElements) {
                    Compound.Class classTag = new() {
                        Type = classElem.Attribute("kind")?.Value,
                        Name = classElem.Value
                    };

                    compoundTag.Classes.Add(classTag);
                }

                yield return compoundTag;
            }
        }

        private static async IAsyncEnumerable<XElement> StreamElementsAsync(Stream stream, string matchName) {
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
