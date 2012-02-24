using System;
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


namespace Signum.Engine
{
    internal static class SqlBuilder
    {
        public readonly static SqlDbType PrimaryKeyType = SqlDbType.Int;
        public readonly static string PrimaryKeyName = "Id";

        public readonly static SqlDbType TicksType = SqlDbType.BigInt;
        public readonly static string TicksName = "Ticks";

        public readonly static SqlDbType ToStrType = SqlDbType.NVarChar;
        public readonly static string ToStrName = "ToStr";

        #region Create Tables
        public static SqlPreCommand CreateTable(string table, List<string> campos)
        {
            return new SqlPreCommandSimple("CREATE TABLE {0}(\r\n{1}\r\n)".Formato(table.SqlScape(), campos.ToString(",\r\n").Indent(2)));
        }

        public static SqlPreCommand CreateTableSql(ITable t)
        {
            return CreateTable(t.Name, t.Columns.Values.Select(c => SqlBuilder.CreateField(c)).ToList());
        }

        internal static SqlPreCommand DropTable(string table)
        {
            return new SqlPreCommandSimple("DROP TABLE {0}".Formato(table.SqlScape()));
        }

        internal static SqlPreCommand DropView(string view)
        {
            return new SqlPreCommandSimple("DROP VIEW {0}".Formato(view.SqlScape()));
        }

        public static SqlPreCommand DropViewIndex(string view, string index)
        {
            return new[]{
                 DropIndex(view, index),
                 DropView(view)}.Combine(Spacing.Simple);
        }

        internal static SqlPreCommand AlterTableDropColumn(string table, string columnName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP COLUMN {1}".Formato(table.SqlScape(), columnName.SqlScape()));
        }

        internal static SqlPreCommand AlterTableAddColumn(string table, IColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD {1}{2}".Formato(table, CreateField(column), !column.Nullable ? "-- DEFAULT(" + (IsNumber(column.SqlDbType) ? "0" : " ") + ")" : null));
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

        internal static SqlPreCommand AlterTableAlterColumn(string table, IColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1}".Formato(table.SqlScape(), CreateField(column)));
        }

        public static string CreateField(IColumn c)
        {
            return SqlBuilder.CreateField(c.Name, c.SqlDbType, c.Size, c.Scale, c.Nullable, c.PrimaryKey, c.Identity);
        }

        public static string CreatePrimaryKeyField(bool identity)
        {
            return CreateField(PrimaryKeyName, PrimaryKeyType, null, null, false, true, identity);
        }

        public static string CreateReferenceField(string name, bool nullable)
        {
            return CreateField(name, PrimaryKeyType, null, null, nullable, false, false);
        }

        public static string CreateField(string name, SqlDbType type, int? size, int? scale, bool nullable, bool primaryKey, bool identity)
        {
            Schema.Current.Settings.FixType(ref type, ref size, ref scale);

            return "{0} {1}{2} {3}{4}{5}".Formato(
                name.SqlScape(),
                type.ToString().ToUpper(CultureInfo.InvariantCulture),
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
                c.ReferenceTable == null? null : SqlBuilder.AlterTableAddConstraintForeignKey(t.Name, c.Name, c.ReferenceTable.Name)).Combine(Spacing.Simple);
        }

        public static SqlPreCommand CreateAllIndices(ITable t)
        {
            return CreateAllIndices(t, t.GeneratUniqueIndexes()); 
        }

        public static SqlPreCommand CreateAllIndices(ITable t, IEnumerable<UniqueIndex> tableIndexes)
        {
            var uniqueIndices = tableIndexes.Select(ix => CreateUniqueIndex(ix)).Combine(Spacing.Simple); 

            var freeIndexes = t.Columns.Values.Where(c=>c.ReferenceTable != null).Select(c=>CreateMultipleIndex(t, c)).Combine(Spacing.Simple); 

            return new []{uniqueIndices, freeIndexes}.Combine(Spacing.Simple); 
        }

        internal static SqlPreCommand DropIndex(string table, string indexName)
        {
            return new SqlPreCommandSimple("DROP INDEX {0}.{1}".Formato(table.SqlScape(), indexName.SqlScape()));
        }

        internal static SqlPreCommand DropIndexCommented(string table, string indexName)
        {
            return new SqlPreCommandSimple("-- DROP INDEX {0}.{1}".Formato(table.SqlScape(), indexName.SqlScape()));
        }
        
        public static SqlPreCommand CreateMultipleIndex(ITable table, IColumn column)
        {
            string indexName = "FIX_{0}_{1}".Formato(table.Name, column.Name);

            return new SqlPreCommandSimple("CREATE INDEX {0} ON {1}({2})".Formato(
                 indexName,
                 table.Name.SqlScape(),
                 column.Name.SqlScape()));
        }

        public static SqlPreCommand CreateUniqueIndex(UniqueIndex index)
        {
            string columns = index.Columns.ToString(c => c.Name.SqlScape(), ", ");

            if (string.IsNullOrEmpty(index.Where))
            {
                return new SqlPreCommandSimple("CREATE UNIQUE INDEX {0} ON {1}({2})".Formato(
                    index.IndexName,
                    index.Table.Name.SqlScape(),
                    columns));
            }

            if (index.ViewName != null)
            {
                string viewName = index.ViewName;

                SqlPreCommandSimple viewSql = new SqlPreCommandSimple(@"CREATE VIEW {0} WITH SCHEMABINDING AS SELECT {1} FROM dbo.{2} WHERE {3}"
                    .Formato(viewName.SqlScape(), columns, index.Table.Name.SqlScape(), index.Where));

                SqlPreCommandSimple indexSql = new SqlPreCommandSimple(@"CREATE UNIQUE CLUSTERED INDEX {0} ON {1}({2})"
                    .Formato(index.IndexName, viewName.SqlScape(), index.Columns.ToString(c => c.Name.SqlScape(), ", ")));

                return SqlPreCommand.Combine(Spacing.Simple , viewSql, indexSql);
            }
            else
            {
                return new SqlPreCommandSimple("CREATE UNIQUE INDEX {0} ON {1}({2}) WHERE {3}".Formato(
                      index.IndexName,
                      index.Table.Name.SqlScape(),
                      columns, index.Where));
            }
        }

        public static SqlPreCommand AlterTableDropConstraint(string table, string constraintName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP CONSTRAINT {1} ".Formato(
                table.SqlScape(),
                constraintName.SqlScape()));
        }

        public static SqlPreCommand AlterTableAddConstraintForeignKey(string table, string fieldName, string foreignTable)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3}({4})".Formato(
                table.SqlScape(),
                ForeignKeyName(table, fieldName),
                fieldName.SqlScape(),
                foreignTable.SqlScape(),
                PrimaryKeyName.SqlScape()));
        }

        public static string ForeignKeyName(string table, string fieldName)
        {
            return "FK_{0}_{1}".Formato(table, fieldName).SqlScape();
        }

        internal static SqlPreCommand RenameTable(string oldName, string newName)
        {
            return new SqlPreCommandSimple("EXEC SP_RENAME '{0}' , '{1}'".Formato(oldName, newName));
        }

        internal static SqlPreCommand RenameColumn(string tableName, string oldName, string newName)
        {
            return new SqlPreCommandSimple("EXEC SP_RENAME '{0}.{1}' , '{2}', 'COLUMN' ".Formato(tableName, oldName, newName));
        }

        internal static SqlPreCommand RenameIndex(string tableName, string oldName, string newName)
        {
            return new SqlPreCommandSimple("EXEC SP_RENAME '{0}.{1}' , '{2}', 'INDEX' ".Formato(tableName, oldName, newName));
        }
        #endregion

        internal static SqlPreCommandSimple SetIdentityInsert(string table, bool value)
        {
            return new SqlPreCommandSimple("SET IDENTITY_INSERT {0} {1}".Formato(
                table.SqlScape(), value ? "ON" : "OFF"));
        }

        internal static SqlPreCommandSimple SetSnapshotIsolation(string schemaName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET ALLOW_SNAPSHOT_ISOLATION {1}".Formato(schemaName, value ? "ON" : "OFF"));
        }

        internal static SqlPreCommandSimple MakeSnapshotIsolationDefault(string schemaName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET READ_COMMITTED_SNAPSHOT {1}".Formato(schemaName, value ? "ON" : "OFF"));
        }

        internal static SqlPreCommandSimple SelectRowCount()
        {
            return new SqlPreCommandSimple("select @@rowcount;"); 
        }
    }
}
