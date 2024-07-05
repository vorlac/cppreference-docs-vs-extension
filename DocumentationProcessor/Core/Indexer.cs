using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Data.SQLite;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DocumentationProcessor.Core
{
    public sealed class Indexer(Uri userDataDir, Uri cppRefDocsDir)
    {
        private SQLiteConnection Database { get; set; }
        private Uri UserDataDir { get; } = userDataDir;
        private Uri DocsRootDir { get; } = cppRefDocsDir;

        public void BuildCppReferenceSymbolIndex() {
            this.ParseAndIndexDocs().Wait();
        }

        private async Task ParseAndIndexDocs() {
            if (this.InitIndexDatabase()) {
                Console.WriteLine(@$"Parsing and indexing offline archive of cppreference: {this.DocsRootDir}");
                DbTransaction transaction = await this.Database.BeginTransactionAsync();

                string sqlScriptPath = Path.Join(AppContext.BaseDirectory, @"SQL\DBSchema.sql");
                SQLiteCommand command = new(await File.ReadAllTextAsync(sqlScriptPath), this.Database);
                await command.ExecuteNonQueryAsync();
                await this.PopulateIndexDB(this.Database);
                await transaction.CommitAsync();
            }

            if (this.Database != null)
                await this.Database.CloseAsync();
        }

        private bool InitIndexDatabase() {
            string dbPath = Path.Join(this.UserDataDir.AbsolutePath, "cppreference.db");
            string connectionString = @$"Data Source = {dbPath}; Version = 3;";

            try {
                SQLiteConnection.CreateFile(dbPath);
                this.Database = new(connectionString);
                this.Database.Open();
            }
            catch (Exception e) {
                Console.WriteLine(@$"Failed to create index database: {dbPath}");
                Console.WriteLine(@$"Error: {e.Message}");
                return false;
            }

            if (this.Database.State != ConnectionState.Open)
                throw new SQLiteException($"Failed to initialize cppreference index DB: {dbPath}");

            return this.Database.State == ConnectionState.Open;
        }

        private async Task PopulateIndexDB(SQLiteConnection connection) {
            await foreach (CppSymbolInfo tag in this.ParseCppReferenceIndexTags())
                tag.InsertTableRecord(connection);
        }

        private async IAsyncEnumerable<CppSymbolInfo> ParseCppReferenceIndexTags() {
            string indexFilePath = Path.Join(
                this.DocsRootDir.AbsolutePath,
                @"cppreference-doxygen-local.tag.xml"
            );

            FileStream stream = new(indexFilePath, FileMode.Open);
            await foreach (XElement elem in StreamElementsAsync(stream, "compound")) {
                CppSymbolInfo compoundTag = new() {
                    Name = elem.Element("name")?.Value,
                    Type = elem.Attribute("kind")?.Value,
                    FileName = elem.Element("filename")?.Value,
                    NameSpace = elem.Attribute("kind")?.Value == "file"
                        ? elem.Element("namespace")?.Value
                        : null
                };

                IEnumerable<XElement> memberElements = elem.Elements("member");
                foreach (XElement memberElem in memberElements) {
                    CppSymbolInfo.Member memberTag = new() {
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
                    CppSymbolInfo.Class classTag = new() {
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
                while (reader.NodeType == XmlNodeType.Element && reader.Name == matchName) {
                    if (await XNode.ReadFromAsync(reader, CancellationToken.None) is XElement elem)
                        yield return elem;
                }
            }
        }
    }
}
