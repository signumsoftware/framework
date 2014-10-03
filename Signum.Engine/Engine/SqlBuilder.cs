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
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP COLUMN {1}".Formato(table.Name, columnName.SqlEscape()));
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
            Connector.Current.FixType(ref type, ref size, ref scale);

            return "{0} {1}{2} {3}{4}{5}".Formato(
                name.SqlEscape(),
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

                return new SqlPreCommandSimple("DROP INDEX {0}.{1}".Formato(objectName, indexName.SqlEscape()));

            else
                return new SqlPreCommandSimple("EXEC {2}.dbo.sp_executesql N'DROP INDEX {0}.{1}'".Formato(objectName.OnDatabase(null).ToString(), indexName.SqlEscape(), objectName.Schema.Database.ToString()));
        }

        public static SqlPreCommand ReCreateFreeIndex(ITable table, DiffIndex index, string oldTable, Dictionary<string, string> tableReplacements)
        {
            if (index.IsControlledIndex)
                throw new InvalidOperationException("The Index is not a free index");

            var onlyColumn = index.Columns.Only();

            string indexName = onlyColumn != null && index.IndexName.StartsWith("FIX_") ? "FIX_{0}_{1}".Formato(table.Name.Name, (tableReplacements.TryGetC(onlyColumn) ?? onlyColumn)) :
                tableReplacements == null ? index.IndexName.Replace(oldTable, table.Name.Name) :
                index.IndexName.Replace(tableReplacements).Replace(oldTable, table.Name.Name);

            string columns = index.Columns.ToString(c => (tableReplacements.TryGetC(c) ?? c).SqlEscape(), ", ");

            return new SqlPreCommandSimple("CREATE INDEX {0} ON {1}({2})".Formato(
                 indexName.SqlEscape(),
                 table.Name,
                 columns));
        }

        public static SqlPreCommand CreateIndex(Index index)
        {
            string columns = index.Columns.ToString(c => c.Name.SqlEscape(), ", ");

            if (!(index is UniqueIndex))
            {
                return new SqlPreCommandSimple("CREATE INDEX {0} ON {1}({2})".Formato(
                  index.IndexName,
                  index.Table.Name,
                  columns));

            }
            else
            {
                var uIndex = (UniqueIndex)index;

                if (string.IsNullOrEmpty(uIndex.Where))
                {
                    return new SqlPreCommandSimple("CREATE {0}INDEX {1} ON {2}({3})".Formato(
                        uIndex is UniqueIndex ? "UNIQUE " : null,
                        uIndex.IndexName,
                        uIndex.Table.Name,
                        columns));
                }

                if (uIndex.ViewName != null)
                {
                    ObjectName viewName = new ObjectName(uIndex.Table.Name.Schema, uIndex.ViewName);

                    SqlPreCommandSimple viewSql = new SqlPreCommandSimple(@"CREATE VIEW {0} WITH SCHEMABINDING AS SELECT {1} FROM {2} WHERE {3}"
                        .Formato(viewName, columns, uIndex.Table.Name.ToStringDbo(), uIndex.Where)) { AddGo = true };

                    SqlPreCommandSimple indexSql = new SqlPreCommandSimple(@"CREATE UNIQUE CLUSTERED INDEX {0} ON {1}({2})"
                        .Formato(uIndex.IndexName, viewName, uIndex.Columns.ToString(c => c.Name.SqlEscape(), ", ")));

                    return SqlPreCommand.Combine(Spacing.Simple, viewSql, indexSql);
                }
                else
                {
                    return new SqlPreCommandSimple("CREATE UNIQUE INDEX {0} ON {1}({2}) WHERE {3}".Formato(
                          uIndex.IndexName,
                          uIndex.Table.Name,
                          columns, uIndex.Where));
                }
            }
        }

        public static SqlPreCommand AlterTableDropConstraint(ObjectName tableName, string constraintName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP CONSTRAINT {1}".Formato(
                tableName,
                constraintName.SqlEscape())) { AddGo = true };
        }

        public static SqlPreCommand AlterTableAddDefaultConstraint(ObjectName tableName, string column, string constraintName, string definition)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} DEFAULT {2} FOR {3}"
                        .Formato(tableName, constraintName.SqlEscape(), definition, column.SqlEscape()));
        }

        public static SqlPreCommand AlterTableAddConstraintForeignKey(ITable table, string fieldName, ITable foreignTable)
        {
            if(!object.Equals(table.Name.Schema.Database, foreignTable.Name.Schema.Database))
                return null;

            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3}({4})".Formato(
                table.Name,
                ForeignKeyName(table.Name.Name, fieldName),
                fieldName.SqlEscape(),
                foreignTable.Name,
                foreignTable.PrimaryKey.Name.SqlEscape()));
        }

        public static string ForeignKeyName(string table, string fieldName)
        {
            return "FK_{0}_{1}".Formato(table, fieldName).SqlEscape();
        }

        public static SqlPreCommand RenameForeignKey(SchemaName schema, string oldName, string newName)
        {
            return SP_RENAME(schema, oldName, newName, "OBJECT");
        }

        internal static SqlPreCommandSimple SP_RENAME(SchemaName schema, string oldName, string newName, string objectType)
        {
            return new SqlPreCommandSimple("EXEC {0}SP_RENAME '{1}' , '{2}'{3}".Formato(
                schema.IsDefault() ? null : schema.ToString() + ".",
                oldName,
                newName,
                objectType == null ? null : ", '{0}'".Formato(objectType)
                ));
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
                   columnNames.ToString(a => a.SqlEscape(), ", "),
                   columnNames.ToString(a => "[table]." + a.SqlEscape(), ", ")));

            return SqlPreCommand.Combine(Spacing.Simple,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} ON".Formato(newTable)),
                command,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} OFF".Formato(newTable)));
        }

        public static SqlPreCommand RenameTable(ObjectName oldName, string newName)
        {
            return SP_RENAME(oldName.Schema, oldName.Name, newName, null);
        }

        public static SqlPreCommandSimple AlterSchema(ObjectName oldName, SchemaName schemaName)
        {
            return new SqlPreCommandSimple("ALTER SCHEMA {0} TRANSFER {1};".Formato(schemaName.Name.SqlEscape(), oldName));
        }

        public static SqlPreCommand RenameColumn(ITable table, string oldName, string newName)
        {
            return SP_RENAME(table.Name.Schema, table.Name.Name + "." + oldName, newName, "COLUMN");
        }

        public static SqlPreCommand RenameIndex(ITable table, string oldName, string newName)
        {
            return SP_RENAME(table.Name.Schema, table.Name.Name + "." + oldName, newName, "INDEX");
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

        public static SqlPreCommandSimple DisableForeignKey(ObjectName tableName, string foreignKey)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} NOCHECK CONSTRAINT {1}".Formato(tableName, foreignKey));
        }

        public static SqlPreCommandSimple EnableForeignKey(ObjectName tableName, string foreignKey)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} WITH CHECK CHECK CONSTRAINT {1}".Formato(tableName, foreignKey));
        }

        public static SqlPreCommandSimple DisableIndex(ObjectName tableName, string indexName)
        {
            return new SqlPreCommandSimple("ALTER INDEX [{0}] ON {1} DISABLE".Formato(indexName, tableName));
        }

        public static SqlPreCommandSimple EnableIndex(ObjectName tableName, string indexName)
        {
            return new SqlPreCommandSimple("ALTER INDEX [{0}] ON {1} REBUILD".Formato(indexName, tableName));
        }

        internal static SqlPreCommand DropStatistics(string tn, List<DiffStats> list)
        {
            if (list.IsEmpty())
                return null;

            return new SqlPreCommandSimple("DROP STATISTICS " + list.ToString(s => tn.SqlEscape() + "." + s.StatsName.SqlEscape(), ",\r\n"));
        }

     
    }
}
