using System.Collections.Generic;
using System.Data.SQLite;

namespace DocumentationProcessor.Core {
    public class CppSymbolInfo {
        public string Type { get; init; }
        public string Name { get; init; }
        public string FileName { get; init; }
        public string NameSpace { get; set; }
        public List<Member> Members { get; } = [];
        public List<Class> Classes { get; } = [];

        public class Member {
            public string Type { get; init; }
            public string Name { get; init; }
            public string AnchorFile { get; init; }
            public string Anchor { get; init; }
            public string ArgList { get; init; }
        }

        public class Class {
            public string Type { get; init; }
            public string Name { get; init; }
        }

        public void InsertTableRecord(SQLiteConnection connection) {
            SQLiteCommand symbolInsert = new(
                $"""
                    insert into symbols (name, type, filename, namespace) 
                    values ('{this.Name}', '{this.Type}', 
                            '{this.FileName}', '{this.NameSpace}');
                """,
                connection
            );

            if (symbolInsert.ExecuteNonQuery() > 0) {
                // the ID of the table record inserted above.
                // necessary to fill in valid values for any
                // foreign key relationships across tables.
                long symbolID = connection.LastInsertRowId;

                foreach (Member member in this.Members) {
                    string tableName = this.Type switch {
                        "function" => "functions",
                        "variable" => "variables",
                        _          => null
                    };

                    var memberInsert = new SQLiteCommand(
                        $"""
                            insert into {tableName} (name, anchor_file, anchor, arg_list, type, parent_symbol) 
                            values ('{member.Name}', '{member.AnchorFile}', '{member.Anchor}', 
                                    '{member.ArgList}', '{member.Type}', '{symbolID}');
                        """,
                        connection
                    );

                    memberInsert.ExecuteNonQuery();
                }

                foreach (Class cls in this.Classes) {
                    SQLiteCommand classInsert = new(
                        $"""
                            insert into classes (name, parent_symbol) 
                            values ('{cls.Name}', '{symbolID}');
                        """,
                        connection
                    );

                    classInsert.ExecuteNonQuery();
                }
            }
        }
    }
}
