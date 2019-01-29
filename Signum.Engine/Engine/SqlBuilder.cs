using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Signum.Utilities;
using Signum.Engine.Maps;

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
            var primaryKeyConstraint = t.PrimaryKey == null ? null : "CONSTRAINT {0} PRIMARY KEY CLUSTERED ({1} ASC)".FormatWith(PrimaryClusteredIndex.GetPrimaryKeyName(t.Name), t.PrimaryKey.Name.SqlEscape());

            var systemPeriod = t.SystemVersioned == null ? null : Period(t.SystemVersioned);

            var columns = t.Columns.Values.Select(c => SqlBuilder.CreateColumn(c, GetDefaultConstaint(t, c)))
                .And(primaryKeyConstraint)
                .And(systemPeriod)
                .NotNull()
                .ToString(",\r\n");

            var systemVersioning = t.SystemVersioned == null ? null :
                $"\r\nWITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {t.SystemVersioned.TableName}))";

            return new SqlPreCommandSimple($"CREATE TABLE {t.Name}(\r\n{columns}\r\n)" + systemVersioning);
        }

        public static SqlPreCommand DropTable(DiffTable diffTable)
        {
            if (diffTable.TemporalTableName == null)
                return DropTable(diffTable.Name);

            return SqlPreCommandConcat.Combine(Spacing.Simple,
                AlterTableDisableSystemVersioning(diffTable.Name),
                DropTable(diffTable.Name),
                DropTable(diffTable.TemporalTableName)
            );
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

        public static SqlPreCommand AlterTableAddPeriod(ITable table)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {table.Name} ADD {Period(table.SystemVersioned)}");
        }

        static string Period(SystemVersionedInfo sv) {

            if (!Connector.Current.SupportsTemporalTables)
                throw new InvalidOperationException($"The current connector '{Connector.Current}' does not support Temporal Tables");

            return $"PERIOD FOR SYSTEM_TIME ({sv.StartColumnName.SqlEscape()}, {sv.EndColumnName.SqlEscape()})";
        }

        public static SqlPreCommand AlterTableDropPeriod(ITable table)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {table.Name} DROP PERIOD FOR SYSTEM_TIME");
        }

        public static SqlPreCommand AlterTableEnableSystemVersioning(ITable table)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {table.Name} SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {table.SystemVersioned.TableName}))");
        }

        public static SqlPreCommandSimple AlterTableDisableSystemVersioning(ObjectName tableName)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {tableName} SET (SYSTEM_VERSIONING = OFF)");
        }

        public static SqlPreCommand AlterTableDropColumn(ITable table, string columnName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP COLUMN {1}".FormatWith(table.Name, columnName.SqlEscape()));
        }

        public static SqlPreCommand AlterTableAddColumn(ITable table, IColumn column, SqlBuilder.DefaultConstraint tempDefault = null)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD {1}".FormatWith(table.Name, CreateColumn(column, tempDefault ?? GetDefaultConstaint(table, column))));
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

        public static SqlPreCommand AlterTableAlterColumn(ITable table, IColumn column, string defaultConstraintName = null, ObjectName forceTableName = null)
        {
            var alterColumn = new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1}".FormatWith(forceTableName ?? table.Name, CreateColumn(column, null)));

            if (column.Default == null)
                return alterColumn;

            var defCons = GetDefaultConstaint(table, column);

            return SqlPreCommand.Combine(Spacing.Simple,
                AlterTableDropConstraint(table.Name, defaultConstraintName ?? defCons.Name),
                alterColumn,
                AlterTableAddDefaultConstraint(table.Name, defCons)
            );
        }

        public static DefaultConstraint GetDefaultConstaint(ITable t, IColumn c)
        {
            if (c.Default == null)
                return null;

            return new DefaultConstraint { ColumnName = c.Name, Name = $"DF_{t.Name.Name}_{c.Name}", QuotedDefinition = Quote(c.SqlDbType, c.Default) };
        }

        public class DefaultConstraint
        {
            public string ColumnName;
            public string Name;
            public string QuotedDefinition;
        }


        public static string CreateColumn(IColumn c, DefaultConstraint constraint)
        {
            string fullType = GetColumnType(c);

            var generatedAlways = c is SystemVersionedInfo.Column svc ?
                $"GENERATED ALWAYS AS ROW {(svc.SystemVersionColumnType == SystemVersionedInfo.ColumnType.Start ? "START" : "END")} HIDDEN" :
                null;

            var defaultConstraint = constraint != null ? $"CONSTRAINT {constraint.Name} DEFAULT " + constraint.QuotedDefinition : null;

            return $" ".CombineIfNotEmpty(
                c.Name.SqlEscape(),
                fullType,
                c.Identity ? "IDENTITY " : null,
                generatedAlways,
                c.Collation != null ? ("COLLATE " + c.Collation) : null,
                c.Nullable.ToBool() ? "NULL" : "NOT NULL",
                defaultConstraint
                );
        }

        public static string GetColumnType(IColumn c)
        {
            return (c.SqlDbType == SqlDbType.Udt ? c.UserDefinedTypeName : c.SqlDbType.ToString().ToUpper()) + GetSizeScale(c.Size, c.Scale);
        }

        public static string Quote(SqlDbType type, string @default)
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


        public static SqlPreCommand DropIndex(ObjectName tableName, DiffIndex index)
        {
            if (index.IsPrimary)
                return AlterTableDropConstraint(tableName, new ObjectName(tableName.Schema, index.IndexName));

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

        public static SqlPreCommand CreateIndex(Index index, Replacements checkUnique)
        {
            if (index is PrimaryClusteredIndex)
            {
                var columns = index.Columns.ToString(c => c.Name.SqlEscape(), ", ");

                return new SqlPreCommandSimple($"ALTER TABLE {index.Table.Name} ADD CONSTRAINT {index.IndexName} PRIMARY KEY CLUSTERED({columns})");
            }

            if (index is UniqueIndex uIndex)
            {
                if (uIndex.ViewName != null)
                {
                    ObjectName viewName = new ObjectName(uIndex.Table.Name.Schema, uIndex.ViewName);

                    var columns = index.Columns.ToString(c => c.Name.SqlEscape(), ", ");

                    SqlPreCommandSimple viewSql = new SqlPreCommandSimple($"CREATE VIEW {viewName} WITH SCHEMABINDING AS SELECT {columns} FROM {uIndex.Table.Name.ToString()} WHERE {uIndex.Where}")
                    { GoBefore = true, GoAfter = true };

                    SqlPreCommandSimple indexSql = new SqlPreCommandSimple($"CREATE UNIQUE CLUSTERED INDEX {uIndex.IndexName} ON {viewName}({columns})");

                    return SqlPreCommand.Combine(Spacing.Simple, checkUnique!=null ? RemoveDuplicatesIfNecessary(uIndex, checkUnique) : null, viewSql, indexSql);
                }
                else
                {
                    return SqlPreCommand.Combine(Spacing.Double,
                        checkUnique != null ? RemoveDuplicatesIfNecessary(uIndex, checkUnique) : null,
                        CreateIndexBasic(index, false));
                }
            }
            else
            {
                return CreateIndexBasic(index, forHistoryTable: false);
            }
        }

        public static int DuplicateCount(UniqueIndex uniqueIndex, Replacements rep)
        {
            var primaryKey = uniqueIndex.Table.Columns.Values.Where(a => a.PrimaryKey).Only();

            if (primaryKey == null)
                throw new InvalidOperationException("No primary key found"); ;

            var oldTableName = rep.Apply(Replacements.KeyTablesInverse, uniqueIndex.Table.Name.ToString());

            var columnReplacement = rep.TryGetC(Replacements.KeyColumnsForTable(uniqueIndex.Table.Name.ToString()))?.Inverse() ?? new Dictionary<string, string>();

            var oldColumns = uniqueIndex.Columns.ToString(c => (columnReplacement.TryGetC(c.Name) ?? c.Name).SqlEscape(), ", ");

            var oldPrimaryKey = columnReplacement.TryGetC(primaryKey.Name) ?? primaryKey.Name;

            return (int)Executor.ExecuteScalar(
$@"SELECT Count(*) FROM {oldTableName}
WHERE {oldPrimaryKey} NOT IN
(
    SELECT MIN({oldPrimaryKey})
    FROM {oldTableName}
    {(string.IsNullOrWhiteSpace(uniqueIndex.Where) ? "" : "WHERE " + uniqueIndex.Where.Replace(columnReplacement))}
    GROUP BY {oldColumns}
){(string.IsNullOrWhiteSpace(uniqueIndex.Where) ? "" : "AND " + uniqueIndex.Where.Replace(columnReplacement))}");
        }

        public static SqlPreCommand RemoveDuplicatesIfNecessary(UniqueIndex uniqueIndex, Replacements rep)
        {
            try
            {
                var primaryKey = uniqueIndex.Table.Columns.Values.Where(a => a.PrimaryKey).Only();

                if (primaryKey == null)
                    return null;


                int count = DuplicateCount(uniqueIndex, rep);

                if (count == 0)
                    return null;

                var columns = uniqueIndex.Columns.ToString(c => c.Name.SqlEscape(), ", ");

                if (rep.Interactive)
                {
                    if (SafeConsole.Ask($"There are {count} rows in {uniqueIndex.Table.Name} with the same {columns}. Generate DELETE duplicates script?"))
                        return RemoveDuplicates(uniqueIndex, primaryKey, columns, commentedOut: false);

                    return null;
                }
                else
                {
                    return RemoveDuplicates(uniqueIndex, primaryKey, columns, commentedOut: true);
                }
            }
            catch (Exception)
            {
                return new SqlPreCommandSimple($"-- Impossible to determine duplicates in new index {uniqueIndex.IndexName}");

            }
        }

        private static SqlPreCommand RemoveDuplicates(UniqueIndex uniqueIndex, IColumn primaryKey, string columns, bool commentedOut)
        {
            return new SqlPreCommandSimple($@"DELETE {uniqueIndex.Table.Name}
WHERE {primaryKey.Name} NOT IN
(
    SELECT MIN({primaryKey.Name})
    FROM {uniqueIndex.Table.Name}
    {(string.IsNullOrWhiteSpace(uniqueIndex.Where) ? "" : "WHERE " + uniqueIndex.Where)}
    GROUP BY {columns}
){(string.IsNullOrWhiteSpace(uniqueIndex.Where) ? "" : " AND " + uniqueIndex.Where)}".Let(txt => commentedOut ? txt.Indent(2, '-') : txt));
        }

        public static SqlPreCommand CreateIndexBasic(Index index, bool forHistoryTable)
        {
            var indexType = index is UniqueIndex ? "UNIQUE INDEX" : "INDEX";
            var columns = index.Columns.ToString(c => c.Name.SqlEscape(), ", ");
            var include = index.IncludeColumns.HasItems() ? $" INCLUDE ({index.IncludeColumns.ToString(c => c.Name.SqlEscape(), ", ")})" : null;
            var where = index.Where.HasText() ? $" WHERE {index.Where}" : "";

            var tableName = forHistoryTable ? index.Table.SystemVersioned.TableName : index.Table.Name;

            return new SqlPreCommandSimple($"CREATE {indexType} {index.IndexName} ON {tableName}({columns}){include}{where}");
        }

        internal static SqlPreCommand UpdateTrim(ITable tab, IColumn tabCol)
        {
            return new SqlPreCommandSimple("UPDATE {0} SET {1} = RTRIM({1})".FormatWith(tab.Name, tabCol.Name));;
        }

        public static SqlPreCommand AlterTableDropConstraint(ObjectName tableName, ObjectName constraintName) =>
            AlterTableDropConstraint(tableName, constraintName.Name);

        public static SqlPreCommand AlterTableDropConstraint(ObjectName tableName, string constraintName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP CONSTRAINT {1}".FormatWith(
                tableName,
                constraintName.SqlEscape()));
        }

        public static SqlPreCommandSimple AlterTableAddDefaultConstraint(ObjectName tableName, DefaultConstraint constraint)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {tableName} ADD CONSTRAINT {constraint.Name} DEFAULT {constraint.QuotedDefinition} FOR {constraint.ColumnName}");
        }

        public static SqlPreCommand AlterTableAddConstraintForeignKey(ITable table, string fieldName, ITable foreignTable)
        {
            return AlterTableAddConstraintForeignKey(table.Name, fieldName, foreignTable.Name, foreignTable.PrimaryKey.Name);
        }

        public static SqlPreCommand AlterTableAddConstraintForeignKey(ObjectName parentTable, string parentColumn, ObjectName targetTable, string targetPrimaryKey)
        {
            if (!object.Equals(parentTable.Schema.Database, targetTable.Schema.Database))
                return null;

            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3}({4})".FormatWith(
               parentTable,
               ForeignKeyName(parentTable.Name, parentColumn),
               parentColumn.SqlEscape(),
               targetTable,
               targetPrimaryKey.SqlEscape()));
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

        public static SqlPreCommand RenameOrChangeSchema(ObjectName oldTableName, ObjectName newTableName)
        {
            if (!object.Equals(oldTableName.Schema.Database, newTableName.Schema.Database))
                throw new InvalidOperationException("Different database");

            if (object.Equals(oldTableName.Schema, newTableName.Schema))
                return RenameTable(oldTableName, newTableName.Name);
            
            var oldNewSchema = oldTableName.OnSchema(newTableName.Schema);

            return SqlPreCommand.Combine(Spacing.Simple,
                AlterSchema(oldTableName, newTableName.Schema),
                oldNewSchema.Equals(newTableName) ? null : RenameTable(oldNewSchema, newTableName.Name));
        }

        public static SqlPreCommand RenameOrMove(DiffTable oldTable, ITable newTable)
        {
            if (object.Equals(oldTable.Name.Schema.Database, newTable.Name.Schema.Database))
                return RenameOrChangeSchema(oldTable.Name, newTable.Name);
            
            return SqlPreCommand.Combine(Spacing.Simple,
              CreateTableSql(newTable),
              MoveRows(oldTable.Name, newTable.Name, newTable.Columns.Keys),
              DropTable(oldTable));
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
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} ON".FormatWith(newTable)) { GoBefore = true },
                command,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} OFF".FormatWith(newTable)) { GoAfter = true });
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

        public static SqlPreCommand RenameIndex(ObjectName tableName, string oldName, string newName)
        {
            return SP_RENAME(tableName.Schema.Database, tableName.OnDatabase(null) + "." + oldName, newName, "INDEX");
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
            if (schemaName.Database == null)
                return new SqlPreCommandSimple("CREATE SCHEMA {0}".FormatWith(schemaName)) { GoAfter = true, GoBefore = true };
            else
                return new SqlPreCommandSimple($"EXEC('use {schemaName.Database}; EXEC sp_executesql N''CREATE SCHEMA {schemaName.Name}'' ')");
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

        public static SqlPreCommandSimple RebuildIndex(ObjectName tableName, string indexName)
        {
            return new SqlPreCommandSimple("ALTER INDEX [{0}] ON {1} REBUILD".FormatWith(indexName, tableName));
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


        internal static SqlPreCommand DropStatistics(string tn, List<DiffStats> list)
        {
            if (list.IsEmpty())
                return null;

            return new SqlPreCommandSimple("DROP STATISTICS " + list.ToString(s => tn.SqlEscape() + "." + s.StatsName.SqlEscape(), ",\r\n"));
        }


        public static SqlPreCommand TruncateTable(ObjectName tableName) => new SqlPreCommandSimple($"TRUNCATE TABLE {tableName}");
    }
}
