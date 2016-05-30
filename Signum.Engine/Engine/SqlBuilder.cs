﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Engine.Maps;
using System.Globalization;
using Microsoft.SqlServer.Types;


namespace Signum.Engine
{
    public static class SqlBuilder
    {
        public static List<string> SystemSchemas = new List<string>()
        {
            "dbo",
            "guest",
            "INFORMATION_SCHEMA",
            "sys",
            "db_owner",
            "db_accessadmin",
            "db_securityadmin",
            "db_ddladmin",
            "db_backupoperator",
            "db_datareader",
            "db_datawriter",
            "db_denydatareader",
            "db_denydatawriter"
        };

        #region Create Tables
        public static SqlPreCommandSimple CreateTableSql(ITable t)
        {
            return new SqlPreCommandSimple("CREATE TABLE {0}(\r\n{1}\r\n)".FormatWith(
                t.Name, 
                t.Columns.Values.Select(c => SqlBuilder.CreateColumn(c)).ToString(",\r\n").Indent(2)));
        }

        public static SqlPreCommand DropTable(ObjectName tableName)
        {
            return new SqlPreCommandSimple("DROP TABLE {0}".FormatWith(tableName));
        }

        public static SqlPreCommand DropView(ObjectName viewName)
        {
            return new SqlPreCommandSimple("DROP VIEW {0}".FormatWith(viewName));
        }

        static SqlPreCommand DropViewIndex(ObjectName viewName, string index)
        {
            return new[]{
                 DropIndex(viewName, index),
                 DropView(viewName)
            }.Combine(Spacing.Simple);
        }

        public static SqlPreCommand AlterTableDropColumn(ITable table, string columnName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP COLUMN {1}".FormatWith(table.Name, columnName.SqlEscape()));
        }

        public static SqlPreCommand AlterTableAddColumn(ITable table, IColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD {1}".FormatWith(table.Name, CreateColumn(column)));
        }

        public static bool IsNumber(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.BigInt:
                case SqlDbType.Float:
                case SqlDbType.Decimal:
                case SqlDbType.Int:
                case SqlDbType.Bit:
                case SqlDbType.Money:
                case SqlDbType.Real:
                case SqlDbType.TinyInt:
                case SqlDbType.SmallInt:
                case SqlDbType.SmallMoney:
                    return true;
            }

            return false;
        }


        public static bool IsString(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.NText:
                case SqlDbType.NVarChar:
                case SqlDbType.Text:
                case SqlDbType.VarChar:
                    return true;
            }

            return false;
        }

        public static bool IsDate(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTimeOffset:
                    return true;
            }

            return false;
        }

        public static SqlPreCommand AlterTableAlterColumn(ITable table, IColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1}".FormatWith(table.Name, CreateColumn(column)));
        }

        public static string CreateColumn(IColumn c)
        {
            return CreateColumn(c.Name, c.SqlDbType, c.UserDefinedTypeName, c.Size, c.Scale, c.Nullable, c.PrimaryKey, c.Identity, c.Default);
        }

        public static string CreateColumn(string name, SqlDbType type, string udtTypeName, int? size, int? scale, bool nullable, bool primaryKey, bool identity, string @default)
        {
            Connector.Current.FixType(ref type, ref size, ref scale);

            return "{0} {1}{2} {3}{4}{5}{6}".FormatWith(
                name.SqlEscape(),
                type == SqlDbType.Udt ? udtTypeName : type.ToString().ToUpper(),
                GetSizeScale(size, scale),
                identity ? "IDENTITY " : "",
                nullable ? "NULL" : "NOT NULL",
                @default != null ? " DEFAULT " +  Quote(type, @default) : "",
                primaryKey ? " PRIMARY KEY" : "");
        }

        static string Quote(SqlDbType type, string @default)
        {
            if (IsString(type) && !(@default.StartsWith("'") && @default.StartsWith("'")))
                return "'" + @default + "'";

            return @default;
        }

        public static string GetSizeScale(int? size, int? scale)
        {
            if (size == null)
                return "";

            if (size == int.MaxValue)
                return "(MAX)";

            if (scale == null)
                return "({0})".FormatWith(size);

            return "({0},{1})".FormatWith(size, scale);
        }

        public static SqlPreCommand AlterTableForeignKeys(ITable t)
        {
            return t.Columns.Values.Select(c =>
                (c.ReferenceTable == null || c.AvoidForeignKey) ? null : SqlBuilder.AlterTableAddConstraintForeignKey(t, c.Name, c.ReferenceTable))
                .Combine(Spacing.Simple);
        }

        public static SqlPreCommand CreateAllIndices(ITable t)
        {
            return t.GeneratAllIndexes().Select(CreateIndex).Combine(Spacing.Simple);
        }

        public static SqlPreCommand DropIndex(ObjectName tableName, DiffIndex index)
        {
            if (index.ViewName == null)
                return DropIndex(tableName, index.IndexName);
            else
                return DropViewIndex(new ObjectName(tableName.Schema, index.ViewName), index.IndexName);
        }

        public static SqlPreCommand DropIndex(ObjectName objectName, string indexName)
        {
            if (objectName.Schema.Database == null)
                return new SqlPreCommandSimple("DROP INDEX {0} ON {1}".FormatWith(indexName.SqlEscape(), objectName));

            else
                return new SqlPreCommandSimple("EXEC {0}.dbo.sp_executesql N'DROP INDEX {1} ON {2}'"
                    .FormatWith(objectName.Schema.Database.ToString().SqlEscape(), indexName.SqlEscape(), objectName.OnDatabase(null).ToString()));
        }

        public static SqlPreCommand ReCreateFreeIndex(ITable table, DiffIndex index, string oldTable, Dictionary<string, string> tableReplacements)
        {
            if (index.IsControlledIndex)
                throw new InvalidOperationException("The Index is not a free index");

            var onlyColumn = index.Columns.Only();

            string indexName = onlyColumn != null && index.IndexName.StartsWith("FIX_") ? "FIX_{0}_{1}".FormatWith(table.Name.Name, (tableReplacements.TryGetC(onlyColumn) ?? onlyColumn)) :
                tableReplacements == null ? index.IndexName.Replace(oldTable, table.Name.Name) :
                index.IndexName.Replace(tableReplacements).Replace(oldTable, table.Name.Name);

            string columns = index.Columns.ToString(c => (tableReplacements.TryGetC(c) ?? c).SqlEscape(), ", ");

            return new SqlPreCommandSimple("CREATE INDEX {0} ON {1}({2})".FormatWith(
                 indexName.SqlEscape(),
                 table.Name,
                 columns));
        }

        public static SqlPreCommand CreateIndex(Index index)
        {
            string columns = index.Columns.ToString(c => c.Name.SqlEscape(), ", ");

            if (!(index is UniqueIndex))
            {
                return new SqlPreCommandSimple("CREATE INDEX {0} ON {1}({2})".FormatWith(
                  index.IndexName,
                  index.Table.Name,
                  columns));

            }
            else
            {
                var uIndex = (UniqueIndex)index;

                if (string.IsNullOrEmpty(uIndex.Where))
                {
                    return new SqlPreCommandSimple("CREATE {0}INDEX {1} ON {2}({3})".FormatWith(
                        uIndex is UniqueIndex ? "UNIQUE " : null,
                        uIndex.IndexName,
                        uIndex.Table.Name,
                        columns));
                }

                if (uIndex.ViewName != null)
                {
                    ObjectName viewName = new ObjectName(uIndex.Table.Name.Schema, uIndex.ViewName);

                    SqlPreCommandSimple viewSql = new SqlPreCommandSimple(@"CREATE VIEW {0} WITH SCHEMABINDING AS SELECT {1} FROM {2} WHERE {3}"
                        .FormatWith(viewName, columns, uIndex.Table.Name.ToString(), uIndex.Where)) { GoBefore = true, GoAfter = true };

                    SqlPreCommandSimple indexSql = new SqlPreCommandSimple(@"CREATE UNIQUE CLUSTERED INDEX {0} ON {1}({2})"
                        .FormatWith(uIndex.IndexName, viewName, uIndex.Columns.ToString(c => c.Name.SqlEscape(), ", ")));

                    return SqlPreCommand.Combine(Spacing.Simple, viewSql, indexSql);
                }
                else
                {
                    return new SqlPreCommandSimple("CREATE UNIQUE INDEX {0} ON {1}({2}) WHERE {3}".FormatWith(
                          uIndex.IndexName,
                          uIndex.Table.Name,
                          columns, uIndex.Where));
                }
            }
        }

        public static SqlPreCommand AlterTableDropConstraint(ObjectName tableName, ObjectName constraintName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP CONSTRAINT {1}".FormatWith(
                tableName,
                constraintName.Name.SqlEscape())) { GoAfter = true };
        }

        public static SqlPreCommand AlterTableAddDefaultConstraint(ObjectName tableName, string column, string constraintName, string definition)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} DEFAULT {2} FOR {3}"
                        .FormatWith(tableName, constraintName.SqlEscape(), definition, column.SqlEscape()));
        }

        public static SqlPreCommand AlterTableAddConstraintForeignKey(ITable table, string fieldName, ITable foreignTable)
        {
            if(!object.Equals(table.Name.Schema.Database, foreignTable.Name.Schema.Database))
                return null;

            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3}({4})".FormatWith(
                table.Name,
                ForeignKeyName(table.Name.Name, fieldName),
                fieldName.SqlEscape(),
                foreignTable.Name,
                foreignTable.PrimaryKey.Name.SqlEscape()));
        }

        public static string ForeignKeyName(string table, string fieldName)
        {
            return "FK_{0}_{1}".FormatWith(table, fieldName).SqlEscape();
        }

        public static SqlPreCommand RenameForeignKey(ObjectName foreignKeyName, string newName)
        {
            return SP_RENAME(foreignKeyName.Schema.Database, foreignKeyName.OnDatabase(null).ToString(), newName, "OBJECT");
        }

        public static SqlPreCommandSimple SP_RENAME(DatabaseName database, string oldName, string newName, string objectType)
        {
            return new SqlPreCommandSimple("EXEC {0}SP_RENAME '{1}' , '{2}'{3}".FormatWith(
                database == null ? null: (new SchemaName(database, "dbo").ToString() + "."),
                oldName,
                newName,
                objectType == null ? null : ", '{0}'".FormatWith(objectType)
                ));
        }

        public static SqlPreCommand RenameOrMove(DiffTable oldTable, ITable newTable)
        {
            if (object.Equals(oldTable.Name.Schema, newTable.Name.Schema))
                return RenameTable(oldTable.Name, newTable.Name.Name);

            if (object.Equals(oldTable.Name.Schema.Database, newTable.Name.Schema.Database))
            {
                var oldNewSchema = oldTable.Name.OnSchema(newTable.Name.Schema);

                return SqlPreCommand.Combine(Spacing.Simple,
                    AlterSchema(oldTable.Name, newTable.Name.Schema),
                    oldNewSchema.Equals(newTable.Name) ? null : RenameTable(oldNewSchema, newTable.Name.Name));
            }

            return SqlPreCommand.Combine(Spacing.Simple,
                CreateTableSql(newTable),
                MoveRows(oldTable.Name, newTable.Name, newTable.Columns.Keys),
                DropTable(oldTable.Name));
        }

        public static SqlPreCommand MoveRows(ObjectName oldTable, ObjectName newTable, IEnumerable<string> columnNames)
        {
            SqlPreCommandSimple command = new SqlPreCommandSimple(
@"INSERT INTO {0} ({2})
SELECT {3}
FROM {1} as [table]".FormatWith(
                   newTable,
                   oldTable,
                   columnNames.ToString(a => a.SqlEscape(), ", "),
                   columnNames.ToString(a => "[table]." + a.SqlEscape(), ", ")));

            return SqlPreCommand.Combine(Spacing.Simple,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} ON".FormatWith(newTable)),
                command,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} OFF".FormatWith(newTable)));
        }

        public static SqlPreCommand RenameTable(ObjectName oldName, string newName)
        {
            return SP_RENAME(oldName.Schema.Database, oldName.OnDatabase(null).ToString(), newName, null);
        }

        public static SqlPreCommandSimple AlterSchema(ObjectName oldName, SchemaName schemaName)
        {
            return new SqlPreCommandSimple("ALTER SCHEMA {0} TRANSFER {1};".FormatWith(schemaName.Name.SqlEscape(), oldName));
        }

        public static SqlPreCommand RenameColumn(ITable table, string oldName, string newName)
        {
            return SP_RENAME(table.Name.Schema.Database, table.Name.OnDatabase(null) + "." + oldName, newName, "COLUMN");
        }

        public static SqlPreCommand RenameIndex(ITable table, string oldName, string newName)
        {
            return SP_RENAME(table.Name.Schema.Database, table.Name.OnDatabase(null) + "." + oldName, newName, "INDEX");
        }
        #endregion

        public static SqlPreCommandSimple SetIdentityInsert(ObjectName tableName, bool value)
        {
            return new SqlPreCommandSimple("SET IDENTITY_INSERT {0} {1}".FormatWith(
                tableName, value ? "ON" : "OFF"));
        }

        public static SqlPreCommandSimple SetSingleUser(string databaseName)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;".FormatWith(databaseName));
        }

        public static SqlPreCommandSimple SetMultiUser(string databaseName)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET MULTI_USER;".FormatWith(databaseName));
        }

        public static SqlPreCommandSimple SetSnapshotIsolation(string databaseName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET ALLOW_SNAPSHOT_ISOLATION {1}".FormatWith(databaseName, value ? "ON" : "OFF"));
        }

        public static SqlPreCommandSimple MakeSnapshotIsolationDefault(string databaseName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET READ_COMMITTED_SNAPSHOT {1}".FormatWith(databaseName, value ? "ON" : "OFF"));
        }

        public static SqlPreCommandSimple SelectRowCount()
        {
            return new SqlPreCommandSimple("select @@rowcount;");
        }

        public static SqlPreCommand CreateSchema(SchemaName schemaName)
        {
            return new SqlPreCommandSimple("CREATE SCHEMA {0}".FormatWith(schemaName)) { GoAfter = true, GoBefore = true };
        }

        public static SqlPreCommand DropSchema(SchemaName schemaName)
        {
            return new SqlPreCommandSimple("DROP SCHEMA {0}".FormatWith(schemaName)) { GoAfter = true, GoBefore = true };
        }

        public static SqlPreCommandSimple DisableForeignKey(ObjectName tableName, string foreignKey)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} NOCHECK CONSTRAINT {1}".FormatWith(tableName, foreignKey));
        }

        public static SqlPreCommandSimple EnableForeignKey(ObjectName tableName, string foreignKey)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} WITH CHECK CHECK CONSTRAINT {1}".FormatWith(tableName, foreignKey));
        }

        public static SqlPreCommandSimple DisableIndex(ObjectName tableName, string indexName)
        {
            return new SqlPreCommandSimple("ALTER INDEX [{0}] ON {1} DISABLE".FormatWith(indexName, tableName));
        }

        public static SqlPreCommandSimple EnableIndex(ObjectName tableName, string indexName)
        {
            return new SqlPreCommandSimple("ALTER INDEX [{0}] ON {1} REBUILD".FormatWith(indexName, tableName));
        }

        public static SqlPreCommandSimple DropDefaultConstraint(ObjectName tableName, string columnName)
        {
            DatabaseName db = tableName.Schema.Database;

            var tn = tableName.OnDatabase(null);

            string varName = "Constraint_" + tableName.Name + "_" + columnName;

            string command = @"DECLARE @sql nvarchar(max)
SELECT  @sql = 'ALTER TABLE {Table} DROP CONSTRAINT [' + dc.name  + '];' 
FROM DB.sys.default_constraints dc
JOIN DB.sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE c.object_id = OBJECT_ID('{FullTable}') AND c.name = '{Column}'
EXEC DB.dbo.sp_executesql @sql
"
                .Replace("DB.", db == null ? null : (db.ToString() + "."))
                .Replace("@sql", "@" + varName)
                .Replace("{FullTable}", tableName.ToString())
                .Replace("{Table}", tn.ToString())
                .Replace("{Column}", columnName);

            return new SqlPreCommandSimple(command);
        }

        public static SqlPreCommandSimple DropPrimaryKeyConstraint(ObjectName tableName)
        {
            DatabaseName db = tableName.Schema.Database;

            var tn = tableName.OnDatabase(null);

            string varName = "PrimaryKey_Constraint_" + tn.Name;

            string command = @"
DECLARE @sql nvarchar(max)
SELECT  @sql = 'ALTER TABLE {Table} DROP CONSTRAINT [' + kc.name  + '];' 
FROM DB.sys.key_constraints kc
WHERE kc.parent_object_id = OBJECT_ID('{FullTable}')
EXEC DB.dbo.sp_executesql @sql"
                .Replace("DB.", db == null ? null : (db.ToString() + "."))
                .Replace("@sql", "@" + varName)
                .Replace("{FullTable}", tableName.ToString())
                .Replace("{Table}", tn.ToString());

            return new SqlPreCommandSimple(command);
        }

        public static SqlPreCommandSimple AddDefaultConstraint(ObjectName tableName, string columnName, string definition)
        {
            string constraintName = "DF_{0}_{1}".FormatWith(tableName.Name, columnName);

            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} DEFAULT {2} FOR {3}"
                .FormatWith(tableName, constraintName, definition, columnName));
        }

        internal static SqlPreCommand DropStatistics(string tn, List<DiffStats> list)
        {
            if (list.IsEmpty())
                return null;

            return new SqlPreCommandSimple("DROP STATISTICS " + list.ToString(s => tn.SqlEscape() + "." + s.StatsName.SqlEscape(), ",\r\n"));
        }

     

      
    }
}
