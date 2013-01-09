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
using Signum.Engine.Properties;
using Signum.Engine.Maps;
using System.Globalization;
using Microsoft.SqlServer.Types;


namespace Signum.Engine
{
    internal static class SqlBuilder
    {
        public readonly static SqlDbType PrimaryKeyType = SqlDbType.Int;
        public readonly static string PrimaryKeyName = "Id";

        #region Create Tables
        public static SqlPreCommand CreateTableSql(ITable t)
        {
            return new SqlPreCommandSimple("CREATE TABLE {0}(\r\n{1}\r\n)".Formato(
                t.Name, 
                t.Columns.Values.Select(c => SqlBuilder.CreateField(c)).ToString(",\r\n").Indent(2)));
        }

        public static SqlPreCommand DropTable(ObjectName tableName)
        {
            return new SqlPreCommandSimple("DROP TABLE {0}".Formato(tableName));
        }

        public static SqlPreCommand DropView(ObjectName viewName)
        {
            return new SqlPreCommandSimple("DROP VIEW {0}".Formato(viewName));
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
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP COLUMN {1}".Formato(table.Name, columnName.SqlScape()));
        }

        public static SqlPreCommand AlterTableAddColumn(ITable table, IColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD {1}{2}".Formato(table.Name, CreateField(column), !column.Nullable ? "-- DEFAULT(" + (IsNumber(column.SqlDbType) ? "0" : " ") + ")" : null));
        }

        private static bool IsNumber(SqlDbType sqlDbType)
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

        public static SqlPreCommand AlterTableAlterColumn(ITable table, IColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1}".Formato(table.Name, CreateField(column)));
        }

        public static string CreateField(IColumn c)
        {
            return CreateField(c.Name, c.SqlDbType, c.UdtTypeName, c.Size, c.Scale, c.Nullable, c.PrimaryKey, c.Identity);
        }

        public static string CreateField(string name, SqlDbType type, string udtTypeName, int? size, int? scale, bool nullable, bool primaryKey, bool identity)
        {
            Schema.Current.Settings.FixType(ref type, ref size, ref scale);

            return "{0} {1}{2} {3}{4}{5}".Formato(
                name.SqlScape(),
                type == SqlDbType.Udt ? udtTypeName : type.ToString().ToUpper(),
                GetSizeScale(size, scale),
                identity ? "IDENTITY " : "",
                nullable ? "NULL" : "NOT NULL",
                primaryKey ? " PRIMARY KEY" : "");
        }

        public static string GetSizeScale(int? size, int? scale)
        {
            if (size == null)
                return "";

            if (size == int.MaxValue)
                return "(MAX)";

            if (scale == null)
                return "({0})".Formato(size);

            return "({0},{1})".Formato(size, scale);
        }

        public static SqlPreCommand AlterTableForeignKeys(ITable t)
        {
            return t.Columns.Values.Select(c =>
                c.ReferenceTable == null ? null : SqlBuilder.AlterTableAddConstraintForeignKey(t, c.Name, c.ReferenceTable)).Combine(Spacing.Simple);
        }

        public static SqlPreCommand CreateAllIndices(ITable t)
        {
            return CreateAllIndices(t, t.GeneratUniqueIndexes());
        }

        public static SqlPreCommand CreateAllIndices(ITable t, IEnumerable<UniqueIndex> tableIndexes)
        {
            var uniqueIndices = tableIndexes.Select(ix => CreateUniqueIndex(ix)).Combine(Spacing.Simple);

            var freeIndexes = t.Columns.Values.Where(c => c.ReferenceTable != null).Select(c => CreateFreeIndex(t, c)).Combine(Spacing.Simple);

            return new[] { uniqueIndices, freeIndexes }.Combine(Spacing.Simple);
        }

        public static SqlPreCommand DropIndex(ObjectName tableName, DiffIndex index)
        {
            if (index.ViewName == null)
                return DropIndex(tableName, index.IndexName);
            else
                return DropViewIndex(new ObjectName(tableName.Schema, index.ViewName), index.IndexName);
        }

        static SqlPreCommand DropIndex(ObjectName objectName, string indexName)
        {
            return new SqlPreCommandSimple("DROP INDEX {0}.{1}".Formato(objectName, indexName.SqlScape()));
        }

        public static SqlPreCommand ReCreateFreeIndex(ITable table, DiffIndex index, string oldTable, Dictionary<string, string> tableReplacements)
        {
            if (index.IsControlledIndex)
                throw new InvalidOperationException("The Index is not a free index");

            var onlyColumn = index.Columns.Only();

            string indexName = onlyColumn != null && index.IndexName.StartsWith("FIX_") ? "FIX_{0}_{1}".Formato(table.Name.Name, (tableReplacements.TryGetC(onlyColumn) ?? onlyColumn)) :
                tableReplacements == null ? index.IndexName.Replace(oldTable, table.Name.Name) :
                index.IndexName.Replace(tableReplacements).Replace(oldTable, table.Name.Name);

            string columns = index.Columns.ToString(c => (tableReplacements.TryGetC(c) ?? c).SqlScape(), ", ");

            return new SqlPreCommandSimple("CREATE INDEX {0} ON {1}({2})".Formato(
                 indexName.SqlScape(),
                 table.Name,
                 columns));
        }

        public static SqlPreCommand CreateFreeIndex(ITable table, IColumn column)
        {
            string indexName = "FIX_{0}_{1}".Formato(table.Name.Name, column.Name);

            return new SqlPreCommandSimple("CREATE INDEX {0} ON {1}({2})".Formato(
                 indexName.SqlScape(),
                 table.Name,
                 column.Name.SqlScape()));
        }

        public static SqlPreCommand CreateUniqueIndex(UniqueIndex index)
        {
            string columns = index.Columns.ToString(c => c.Name.SqlScape(), ", ");

            if (string.IsNullOrEmpty(index.Where))
            {
                return new SqlPreCommandSimple("CREATE UNIQUE INDEX {0} ON {1}({2})".Formato(
                    index.IndexName,
                    index.Table.Name,
                    columns));
            }

            if (index.ViewName != null)
            {
                ObjectName viewName = new ObjectName(index.Table.Name.Schema, index.ViewName);

                SqlPreCommandSimple viewSql = new SqlPreCommandSimple(@"CREATE VIEW {0} WITH SCHEMABINDING AS SELECT {1} FROM {2} WHERE {3}"
                    .Formato(viewName, columns, index.Table.Name, index.Where)) { AddGo = true };

                SqlPreCommandSimple indexSql = new SqlPreCommandSimple(@"CREATE UNIQUE CLUSTERED INDEX {0} ON {1}({2})"
                    .Formato(index.IndexName, viewName, index.Columns.ToString(c => c.Name.SqlScape(), ", ")));

                return SqlPreCommand.Combine(Spacing.Simple, viewSql, indexSql);
            }
            else
            {
                return new SqlPreCommandSimple("CREATE UNIQUE INDEX {0} ON {1}({2}) WHERE {3}".Formato(
                      index.IndexName,
                      index.Table.Name,
                      columns, index.Where));
            }
        }

        public static SqlPreCommand AlterTableDropConstraint(ObjectName tableName, string constraintName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP CONSTRAINT {1}".Formato(
                tableName,
                constraintName.SqlScape())) { AddGo = true };
        }

        public static SqlPreCommand AlterTableAddDefaultConstraint(ObjectName tableName, string column, string constraintName, string definition)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} DEFAULT {2} FOR {3}"
                        .Formato(tableName, constraintName.SqlScape(), definition, column.SqlScape()));
        }

        public static SqlPreCommand AlterTableAddConstraintForeignKey(ITable table, string fieldName, ITable foreignTable)
        {
            if(!object.Equals(table.Name.Schema.Database, foreignTable.Name.Schema.Database))
                return null;

            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3}({4})".Formato(
                table.Name,
                ForeignKeyName(table.Name.Name, fieldName),
                fieldName.SqlScape(),
                foreignTable.Name,
                PrimaryKeyName.SqlScape()));
        }

        public static string ForeignKeyName(string table, string fieldName)
        {
            return "FK_{0}_{1}".Formato(table, fieldName).SqlScape();
        }

        public static SqlPreCommand RenameOrMove(DiffTable oldTable, ITable newTable)
        {
            if (object.Equals(oldTable.Name.Schema, newTable.Name.Schema))
                return RenameTable(oldTable.Name, newTable.Name.Name);

            if (object.Equals(oldTable.Name.Schema.Database, newTable.Name.Schema.Database))
                return SqlPreCommand.Combine(Spacing.Simple,
                    AlterSchema(oldTable.Name, newTable.Name.Schema),
                    oldTable.Name == newTable.Name ? null : RenameTable(new ObjectName(newTable.Name.Schema, oldTable.Name.Name), newTable.Name.Name));

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
FROM {1} as [table]".Formato(
                   newTable,
                   oldTable,
                   columnNames.ToString(a => a.SqlScape(), ", "),
                   columnNames.ToString(a => "[table]." + a.SqlScape(), ", ")));

            return SqlPreCommand.Combine(Spacing.Simple,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} ON".Formato(newTable)),
                command,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} OFF".Formato(newTable)));
        }

        public static SqlPreCommand RenameTable(ObjectName oldName, string newName)
        {
            return new SqlPreCommandSimple("EXEC SP_RENAME '{0}' , '{1}'".Formato(oldName, newName.SqlScape()));
        }

        public static SqlPreCommandSimple AlterSchema(ObjectName oldName, SchemaName schemaName)
        {
            return new SqlPreCommandSimple("ALTER SCHEMA {0} TRANSFER {1};".Formato(schemaName.Name.SqlScape(), oldName));
        }

        public static SqlPreCommand RenameColumn(ITable table, string oldName, string newName)
        {
            return new SqlPreCommandSimple("EXEC SP_RENAME '{0}.{1}' , '{2}', 'COLUMN' ".Formato(table.Name, oldName, newName));
        }

        public static SqlPreCommand RenameIndex(ITable table, string oldName, string newName)
        {
            return new SqlPreCommandSimple("EXEC SP_RENAME '{0}.{1}' , '{2}', 'INDEX' ".Formato(table.Name, oldName, newName));
        }
        #endregion

        public static SqlPreCommandSimple SetIdentityInsert(ObjectName tableName, bool value)
        {
            return new SqlPreCommandSimple("SET IDENTITY_INSERT {0} {1}".Formato(
                tableName, value ? "ON" : "OFF"));
        }

        public static SqlPreCommandSimple SetSnapshotIsolation(string databaseName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET ALLOW_SNAPSHOT_ISOLATION {1}".Formato(databaseName, value ? "ON" : "OFF"));
        }

        public static SqlPreCommandSimple MakeSnapshotIsolationDefault(string databaseName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET READ_COMMITTED_SNAPSHOT {1}".Formato(databaseName, value ? "ON" : "OFF"));
        }

        public static SqlPreCommandSimple SelectRowCount()
        {
            return new SqlPreCommandSimple("select @@rowcount;");
        }

        public static SqlPreCommand CreateSchema(SchemaName schemaName)
        {
            return new SqlPreCommandSimple("CREATE SCHEMA {0}".Formato(schemaName));
        }

        public static SqlPreCommand DropSchema(SchemaName schemaName)
        {
            return new SqlPreCommandSimple("DROP SCHEMA {0}".Formato(schemaName));
        }
    }
}
