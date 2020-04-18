using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;

namespace Signum.Engine
{
    public class SqlBuilder
    {
        Connector connector;
        bool isPostgres;

        public bool IsPostgres => isPostgres;

        internal SqlBuilder(Connector connector)
        {
            this.connector = connector;
            this.isPostgres = connector.Schema.Settings.IsPostgres;
        }

        public List<string> SystemSchemas = new List<string>()
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

        public SqlPreCommandSimple? UseDatabase(string? databaseName = null)
        {
            if (Schema.Current.Settings.IsPostgres)
                return null;

            return new SqlPreCommandSimple("use {0}".FormatWith((databaseName ?? Connector.Current.DatabaseName()).SqlEscape(Schema.Current.Settings.IsPostgres)));
        }

        #region Create Tables
        public SqlPreCommand CreateTableSql(ITable t, ObjectName? tableName = null, bool avoidSystemVersioning = false)
        {
            var primaryKeyConstraint = t.PrimaryKey == null || t.SystemVersioned != null && tableName != null && t.SystemVersioned.TableName.Equals(tableName) ? null : 
                isPostgres ? 
                "CONSTRAINT {0} PRIMARY KEY ({1})".FormatWith(PrimaryKeyIndex.GetPrimaryKeyName(t.Name).SqlEscape(isPostgres), t.PrimaryKey.Name.SqlEscape(isPostgres)) : 
                "CONSTRAINT {0} PRIMARY KEY CLUSTERED ({1} ASC)".FormatWith(PrimaryKeyIndex.GetPrimaryKeyName(t.Name).SqlEscape(isPostgres), t.PrimaryKey.Name.SqlEscape(isPostgres));

            var systemPeriod = t.SystemVersioned == null || IsPostgres || avoidSystemVersioning ? null : Period(t.SystemVersioned);

            var columns = t.Columns.Values.Select(c => this.ColumnLine(c, GetDefaultConstaint(t, c), isChange: false, forHistoryTable: avoidSystemVersioning))
                .And(primaryKeyConstraint)
                .And(systemPeriod)
                .NotNull()
                .ToString(",\r\n");

            var systemVersioning = t.SystemVersioned == null || avoidSystemVersioning || IsPostgres ? null :
                $"\r\nWITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {t.SystemVersioned.TableName.OnDatabase(null)}))";

            var result = new SqlPreCommandSimple($"CREATE {(IsPostgres && t.Name.IsTemporal ? "TEMPORARY " : "")}TABLE {tableName ?? t.Name}(\r\n{columns}\r\n)" + systemVersioning + ";");

            if (!(IsPostgres && t.SystemVersioned != null))
                return result;

            return new[]
            {
                result,
                new SqlPreCommandSimple($"CREATE TABLE {t.SystemVersioned.TableName}(LIKE {t.Name});"),
                new SqlPreCommandSimple(@$"CREATE TRIGGER versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON {t.Name}
FOR EACH ROW EXECUTE PROCEDURE versioning(
  'sys_period', '{t.SystemVersioned.TableName}', true
);")
            }.Combine(Spacing.Simple)!;

        }

        public SqlPreCommand DropTable(DiffTable diffTable)
        {
            if (diffTable.TemporalTableName == null)
                return DropTable(diffTable.Name);

            return SqlPreCommandConcat.Combine(Spacing.Simple,
                AlterTableDisableSystemVersioning(diffTable.Name),
                DropTable(diffTable.Name)
            )!;
        }

        public SqlPreCommandSimple DropTable(ObjectName tableName)
        {
            return new SqlPreCommandSimple("DROP TABLE {0};".FormatWith(tableName));
        }

        public SqlPreCommandSimple DropView(ObjectName viewName)
        {
            return new SqlPreCommandSimple("DROP VIEW {0};".FormatWith(viewName));
        }

        public SqlPreCommandSimple CreateExtensionIfNotExist(string extensionName)
        {
            return new SqlPreCommandSimple($"CREATE EXTENSION IF NOT EXISTS \"{ extensionName }\";");
        }

        SqlPreCommand DropViewIndex(ObjectName viewName, string index)
        {
            return new[]{
                 DropIndex(viewName, index),
                 DropView(viewName)
            }.Combine(Spacing.Simple)!;
        }

        public SqlPreCommand AlterTableAddPeriod(ITable table)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {table.Name} ADD {Period(table.SystemVersioned!)};");
        }

        string? Period(SystemVersionedInfo sv) {

            if (!Connector.Current.SupportsTemporalTables)
                throw new InvalidOperationException($"The current connector '{Connector.Current}' does not support Temporal Tables");

            return $"PERIOD FOR SYSTEM_TIME ({sv.StartColumnName!.SqlEscape(isPostgres)}, {sv.EndColumnName!.SqlEscape(isPostgres)})";
        }

        public SqlPreCommand AlterTableDropPeriod(ITable table)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {table.Name} DROP PERIOD FOR SYSTEM_TIME;");
        }

        public SqlPreCommand AlterTableEnableSystemVersioning(ITable table)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {table.Name} SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {table.SystemVersioned!.TableName.OnDatabase(null)}));");
        }

        public SqlPreCommandSimple AlterTableDisableSystemVersioning(ObjectName tableName)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {tableName} SET (SYSTEM_VERSIONING = OFF);");
        }

        public SqlPreCommand AlterTableDropColumn(ITable table, string columnName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP COLUMN {1};".FormatWith(table.Name, columnName.SqlEscape(isPostgres)));
        }

        public SqlPreCommand AlterTableAddColumn(ITable table, IColumn column, SqlBuilder.DefaultConstraint? tempDefault = null)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD {1};".FormatWith(table.Name, ColumnLine(column, tempDefault ?? GetDefaultConstaint(table, column), isChange: false)));
        }

        public SqlPreCommand AlterTableAddOldColumn(ITable table, DiffColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD {1};".FormatWith(table.Name, CreateOldColumn(column)));
        }

        public SqlPreCommand AlterTableAlterColumn(ITable table, IColumn column, DiffColumn diffColumn, ObjectName? forceTableName = null)
        {
            var tableName = forceTableName ?? table.Name;

            var alterColumn = !IsPostgres ?
                 new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1};".FormatWith(tableName, this.ColumnLine(column, null, isChange: true))) :
                 new[] 
                 {
                     !diffColumn.DbType.Equals(column.DbType) || diffColumn.Collation != column.Collation || !diffColumn.ScaleEquals(column) || !diffColumn.SizeEquals(column) ? 
                        new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} TYPE {2};".FormatWith(tableName, column.Name.SqlEscape(isPostgres),  GetColumnType(column) + (column.Collation != null ? " COLLATE " + column.Collation : null))) : null, 
                     diffColumn.Nullable &&  !column.Nullable.ToBool()? new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} SET NOT NULL;".FormatWith(tableName, column.Name.SqlEscape(isPostgres))) : null,
                     !diffColumn.Nullable && column.Nullable.ToBool()? new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} DROP NOT NULL;".FormatWith(tableName, column.Name.SqlEscape(isPostgres))) : null,
                 }.Combine(Spacing.Simple) ?? new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} -- UNEXPECTED COLUMN CHANGE!!".FormatWith(tableName, column.Name.SqlEscape(isPostgres)));

            if (column.Default == null)
                return alterColumn;

            var defCons = GetDefaultConstaint(table, column)!;

            return SqlPreCommand.Combine(Spacing.Simple,
                AlterTableDropConstraint(table.Name, diffColumn.DefaultConstraint?.Name ?? defCons.Name),
                alterColumn,
                AlterTableAddDefaultConstraint(table.Name, defCons)
            )!;
        }

        public DefaultConstraint? GetDefaultConstaint(ITable t, IColumn c)
        {
            if (c.Default == null)
                return null;

            return new DefaultConstraint(c.Name, $"DF_{t.Name.Name}_{c.Name}", Quote(c.DbType, c.Default));
        }

        public class DefaultConstraint
        {
            public string ColumnName;
            public string Name;
            public string QuotedDefinition;

            public DefaultConstraint(string columnName, string name, string quotedDefinition)
            {
                ColumnName = columnName;
                Name = name;
                QuotedDefinition = quotedDefinition;
            }
        }

        public string CreateOldColumn(DiffColumn c)
        {
            string fullType = GetColumnType(c);

            var generatedAlways = c.GeneratedAlwaysType != GeneratedAlwaysType.None ?
                $"GENERATED ALWAYS AS ROW {(c.GeneratedAlwaysType == GeneratedAlwaysType.AsRowStart ? "START" : "END")} HIDDEN" :
                null;

            var defaultConstraint = c.DefaultConstraint!= null ? $"CONSTRAINT {c.DefaultConstraint.Name} DEFAULT " + c.DefaultConstraint.Definition : null;

            return $" ".Combine(
                c.Name.SqlEscape(isPostgres),
                fullType,
                c.Identity ? "IDENTITY " : null,
                generatedAlways,
                c.Collation != null ? ("COLLATE " + c.Collation) : null,
                c.Nullable ? "NULL" : "NOT NULL",
                defaultConstraint
                );
        }

        public string ColumnLine(IColumn c, DefaultConstraint? constraint, bool isChange, bool forHistoryTable = false)
        {
            string fullType = GetColumnType(c);

            var generatedAlways = c is SystemVersionedInfo.SqlServerPeriodColumn svc && !forHistoryTable ?
                $"GENERATED ALWAYS AS ROW {(svc.SystemVersionColumnType == SystemVersionedInfo.ColumnType.Start ? "START" : "END")} HIDDEN" :
                null;

            var defaultConstraint = constraint != null ? $"CONSTRAINT {constraint.Name} DEFAULT " + constraint.QuotedDefinition : null;

            return $" ".Combine(
                c.Name.SqlEscape(isPostgres),
                fullType,
                c.Identity && !isChange && !forHistoryTable ? (isPostgres? "GENERATED ALWAYS AS IDENTITY": "IDENTITY") : null, 
                generatedAlways,
                c.Collation != null ? ("COLLATE " + c.Collation) : null,
                c.Nullable.ToBool() ? "NULL" : "NOT NULL",
                defaultConstraint
                );
        }

        public string GetColumnType(IColumn c)
        {
            return c.UserDefinedTypeName ?? (c.DbType.ToString(IsPostgres) + GetSizeScale(c.Size, c.Scale));
        }

        public string GetColumnType(DiffColumn c)
        {
            return c.UserTypeName ?? c.DbType.ToString(IsPostgres) /*+ GetSizeScale(Math.Max(c.Length, c.Precision), c.Scale)*/;
        }

        public string Quote(AbstractDbType type, string @default)
        {
            if (type.IsString() && !(@default.StartsWith("'") && @default.StartsWith("'")))
                return "'" + @default + "'";

            return @default;
        }

        public string GetSizeScale(int? size, int? scale)
        {
            if (size == null)
                return "";

            if (size == int.MaxValue)
                return IsPostgres ? "" : "(MAX)";

            if (scale == null)
                return "({0})".FormatWith(size);

            return "({0},{1})".FormatWith(size, scale);
        }

        public SqlPreCommand? AlterTableForeignKeys(ITable t)
        {
            return t.Columns.Values.Select(c =>
                (c.ReferenceTable == null || c.AvoidForeignKey) ? null : this.AlterTableAddConstraintForeignKey(t, c.Name, c.ReferenceTable))
                .Combine(Spacing.Simple);
        }


        public SqlPreCommand DropIndex(ObjectName tableName, DiffIndex index)
        {
            if (index.IsPrimary)
                return AlterTableDropConstraint(tableName, new ObjectName(tableName.Schema, index.IndexName, isPostgres));

            if (index.ViewName == null)
                return DropIndex(tableName, index.IndexName);
            else
                return DropViewIndex(new ObjectName(tableName.Schema, index.ViewName, isPostgres), index.IndexName);
        }

        public SqlPreCommand DropIndex(ObjectName objectName, string indexName)
        {
            if (objectName.Schema.Database == null)
            {
                if (IsPostgres)
                    return new SqlPreCommandSimple("DROP INDEX {0};".FormatWith(new ObjectName(objectName.Schema, indexName, IsPostgres)));
                else
                    return new SqlPreCommandSimple("DROP INDEX {0} ON {1};".FormatWith(indexName.SqlEscape(isPostgres), objectName));
            }
            else
                return new SqlPreCommandSimple("EXEC {0}.dbo.sp_executesql N'DROP INDEX {1} ON {2}';"
                    .FormatWith(objectName.Schema.Database.ToString().SqlEscape(isPostgres), indexName.SqlEscape(isPostgres), objectName.OnDatabase(null).ToString()));
        }

        public SqlPreCommand CreateIndex(TableIndex index, Replacements? checkUnique)
        {
            if (index is PrimaryKeyIndex)
            {
                var columns = index.Columns.ToString(c => c.Name.SqlEscape(isPostgres), ", ");

                return new SqlPreCommandSimple($"ALTER TABLE {index.Table.Name} ADD CONSTRAINT {index.IndexName.SqlEscape(isPostgres)} PRIMARY KEY CLUSTERED({columns});");
            }

            if (index is UniqueTableIndex uIndex)
            {
                if (uIndex.ViewName != null)
                {
                    ObjectName viewName = new ObjectName(uIndex.Table.Name.Schema, uIndex.ViewName, isPostgres);

                    var columns = index.Columns.ToString(c => c.Name.SqlEscape(isPostgres), ", ");

                    SqlPreCommandSimple viewSql = new SqlPreCommandSimple($"CREATE VIEW {viewName} WITH SCHEMABINDING AS SELECT {columns} FROM {uIndex.Table.Name} WHERE {uIndex.Where};")
                    { GoBefore = true, GoAfter = true };

                    SqlPreCommandSimple indexSql = new SqlPreCommandSimple($"CREATE UNIQUE CLUSTERED INDEX {uIndex.IndexName.SqlEscape(isPostgres)} ON {viewName}({columns});");

                    return SqlPreCommand.Combine(Spacing.Simple, 
                        checkUnique!=null ? RemoveDuplicatesIfNecessary(uIndex, checkUnique) : null, 
                        viewSql, 
                        indexSql)!;
                }
                else
                {
                    return SqlPreCommand.Combine(Spacing.Double,
                        checkUnique != null ? RemoveDuplicatesIfNecessary(uIndex, checkUnique) : null,
                        CreateIndexBasic(index, forHistoryTable: false))!;
                }
            }
            else
            {
                return CreateIndexBasic(index, forHistoryTable: false);
            }
        }

        public int DuplicateCount(UniqueTableIndex uniqueIndex, Replacements rep)
        {
            var primaryKey = uniqueIndex.Table.Columns.Values.Where(a => a.PrimaryKey).Only();

            if (primaryKey == null)
                throw new InvalidOperationException("No primary key found"); ;

            var oldTableName = rep.Apply(Replacements.KeyTablesInverse, uniqueIndex.Table.Name.ToString());

            var columnReplacement = rep.TryGetC(Replacements.KeyColumnsForTable(uniqueIndex.Table.Name.ToString()))?.Inverse() ?? new Dictionary<string, string>();

            var oldColumns = uniqueIndex.Columns.ToString(c => (columnReplacement.TryGetC(c.Name) ?? c.Name).SqlEscape(isPostgres), ", ");

            var oldPrimaryKey = columnReplacement.TryGetC(primaryKey.Name) ?? primaryKey.Name;

            return Convert.ToInt32(Executor.ExecuteScalar(
$@"SELECT Count(*) FROM {oldTableName}
WHERE {oldPrimaryKey.SqlEscape(IsPostgres)} NOT IN
(
    SELECT MIN({oldPrimaryKey.SqlEscape(IsPostgres)})
    FROM {oldTableName}
    {(!uniqueIndex.Where.HasText() ? "" : "WHERE " + uniqueIndex.Where.Replace(columnReplacement))}
    GROUP BY {oldColumns}
){(!uniqueIndex.Where.HasText() ? "" : "AND " + uniqueIndex.Where.Replace(columnReplacement))};")!);
        }

        public SqlPreCommand? RemoveDuplicatesIfNecessary(UniqueTableIndex uniqueIndex, Replacements rep)
        {
            try
            {
                var primaryKey = uniqueIndex.Table.Columns.Values.Where(a => a.PrimaryKey).Only();

                if (primaryKey == null)
                    return null;


                int count = DuplicateCount(uniqueIndex, rep);

                if (count == 0)
                    return null;

                var columns = uniqueIndex.Columns.ToString(c => c.Name.SqlEscape(isPostgres), ", ");

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
                return new SqlPreCommandSimple($"-- Impossible to determine duplicates in new index {uniqueIndex.IndexName.SqlEscape(isPostgres)}");

            }
        }

        private SqlPreCommand RemoveDuplicates(UniqueTableIndex uniqueIndex, IColumn primaryKey, string columns, bool commentedOut)
        {
            return new SqlPreCommandSimple($@"DELETE {uniqueIndex.Table.Name}
WHERE {primaryKey.Name} NOT IN
(
    SELECT MIN({primaryKey.Name})
    FROM {uniqueIndex.Table.Name}
    {(string.IsNullOrWhiteSpace(uniqueIndex.Where) ? "" : "WHERE " + uniqueIndex.Where)}
    GROUP BY {columns}
){(string.IsNullOrWhiteSpace(uniqueIndex.Where) ? "" : " AND " + uniqueIndex.Where)};".Let(txt => commentedOut ? txt.Indent(2, '-') : txt));
        }

        public SqlPreCommand CreateIndexBasic(Maps.TableIndex index, bool forHistoryTable)
        {
            var indexType = index is UniqueTableIndex ? "UNIQUE INDEX" : "INDEX";
            var columns = index.Columns.ToString(c => c.Name.SqlEscape(isPostgres), ", ");
            var include = index.IncludeColumns.HasItems() ? $" INCLUDE ({index.IncludeColumns.ToString(c => c.Name.SqlEscape(isPostgres), ", ")})" : null;
            var where = index.Where.HasText() ? $" WHERE {index.Where}" : "";

            var tableName = forHistoryTable ? index.Table.SystemVersioned!.TableName : index.Table.Name;

            return new SqlPreCommandSimple($"CREATE {indexType} {index.GetIndexName(tableName).SqlEscape(isPostgres)} ON {tableName}({columns}){include}{where};");
        }

        internal SqlPreCommand UpdateTrim(ITable tab, IColumn tabCol)
        {
            return new SqlPreCommandSimple("UPDATE {0} SET {1} = RTRIM({1});".FormatWith(tab.Name, tabCol.Name));;
        }

        public SqlPreCommand AlterTableDropConstraint(ObjectName tableName, ObjectName foreignKeyName) =>
            AlterTableDropConstraint(tableName, foreignKeyName.Name);

        public SqlPreCommand AlterTableDropConstraint(ObjectName tableName, string constraintName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP CONSTRAINT {1};".FormatWith(
                tableName,
                constraintName.SqlEscape(isPostgres)));
        }

        public SqlPreCommand AlterTableDropDefaultConstaint(ObjectName tableName, DiffColumn column)
        {
            if (isPostgres)
                return AlterTableAlterColumnDropDefault(tableName, column.Name);
            else
                return AlterTableDropConstraint(tableName, column.DefaultConstraint!.Name!);
        }

        public SqlPreCommand AlterTableAlterColumnDropDefault(ObjectName tableName, string columnName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} DROP DEFAULT;".FormatWith(
                tableName,
                columnName.SqlEscape(isPostgres)));
        }

        public SqlPreCommandSimple AlterTableAddDefaultConstraint(ObjectName tableName, DefaultConstraint constraint)
        {
            return new SqlPreCommandSimple($"ALTER TABLE {tableName} ADD CONSTRAINT {constraint.Name} DEFAULT {constraint.QuotedDefinition} FOR {constraint.ColumnName};");
        }

        public SqlPreCommand? AlterTableAddConstraintForeignKey(ITable table, string fieldName, ITable foreignTable)
        {
            return AlterTableAddConstraintForeignKey(table.Name, fieldName, foreignTable.Name, foreignTable.PrimaryKey.Name);
        }

        public SqlPreCommand? AlterTableAddConstraintForeignKey(ObjectName parentTable, string parentColumn, ObjectName targetTable, string targetPrimaryKey)
        {
            if (!object.Equals(parentTable.Schema.Database, targetTable.Schema.Database))
                return null;

            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3}({4});".FormatWith(
               parentTable,
               ForeignKeyName(parentTable.Name, parentColumn).SqlEscape(isPostgres),
               parentColumn.SqlEscape(isPostgres),
               targetTable,
               targetPrimaryKey.SqlEscape(isPostgres)));
        }

        public string ForeignKeyName(string table, string fieldName)
        {
            var result = "FK_{0}_{1}".FormatWith(table, fieldName);

            return StringHashEncoder.ChopHash(result, this.connector.MaxNameLength);
        }

        public SqlPreCommand RenameForeignKey(ObjectName tn, ObjectName foreignKeyName, string newName)
        {
            if (IsPostgres)
                return new SqlPreCommandSimple($"ALTER TABLE {tn} RENAME CONSTRAINT {foreignKeyName.Name.SqlEscape(IsPostgres)} TO {newName.SqlEscape(IsPostgres)};");

            return SP_RENAME(foreignKeyName.Schema.Database, foreignKeyName.OnDatabase(null).ToString(), newName, "OBJECT");
        }

        public SqlPreCommandSimple SP_RENAME(DatabaseName? database, string oldName, string newName, string? objectType)
        {
            return new SqlPreCommandSimple("EXEC {0}SP_RENAME '{1}' , '{2}'{3};".FormatWith(
                database == null ? null: SchemaName.Default(isPostgres).OnDatabase(database).ToString() + ".",
                oldName,
                newName,
                objectType == null ? null : ", '{0}'".FormatWith(objectType)
                ));
        }

        public SqlPreCommand RenameOrChangeSchema(ObjectName oldTableName, ObjectName newTableName)
        {
            if (!object.Equals(oldTableName.Schema.Database, newTableName.Schema.Database))
                throw new InvalidOperationException("Different database");

            if (object.Equals(oldTableName.Schema, newTableName.Schema))
                return RenameTable(oldTableName, newTableName.Name);
            
            var oldNewSchema = oldTableName.OnSchema(newTableName.Schema);

            return SqlPreCommand.Combine(Spacing.Simple,
                AlterSchema(oldTableName, newTableName.Schema),
                oldNewSchema.Equals(newTableName) ? null : RenameTable(oldNewSchema, newTableName.Name))!;
        }

        public SqlPreCommand RenameOrMove(DiffTable oldTable, ITable newTable, ObjectName newTableName)
        {
            if (object.Equals(oldTable.Name.Schema.Database, newTableName.Schema.Database))
                return RenameOrChangeSchema(oldTable.Name, newTableName);
            
            return SqlPreCommand.Combine(Spacing.Simple,
              CreateTableSql(newTable, newTableName, avoidSystemVersioning: true),
              MoveRows(oldTable.Name, newTableName, newTable.Columns.Keys, avoidIdentityInsert: newTable.SystemVersioned != null && newTable.SystemVersioned.Equals(newTable)),
              DropTable(oldTable))!;
        }

        public SqlPreCommand MoveRows(ObjectName oldTable, ObjectName newTable, IEnumerable<string> columnNames, bool avoidIdentityInsert = false)
        {
            SqlPreCommandSimple command = new SqlPreCommandSimple(
@"INSERT INTO {0} ({2})
SELECT {3}
FROM {1} as [table];".FormatWith(
                   newTable,
                   oldTable,
                   columnNames.ToString(a => a.SqlEscape(isPostgres), ", "),
                   columnNames.ToString(a => "[table]." + a.SqlEscape(isPostgres), ", ")));

            if (avoidIdentityInsert)
                return command;

            return SqlPreCommand.Combine(Spacing.Simple,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} ON;".FormatWith(newTable)) { GoBefore = true },
                command,
                new SqlPreCommandSimple("SET IDENTITY_INSERT {0} OFF;".FormatWith(newTable)) { GoAfter = true })!;
        }

        public SqlPreCommand RenameTable(ObjectName oldName, string newName)
        {
            if (IsPostgres)
                return new SqlPreCommandSimple($"ALTER TABLE {oldName} RENAME TO {newName.SqlEscape(IsPostgres)};");

            return SP_RENAME(oldName.Schema.Database, oldName.OnDatabase(null).ToString(), newName, null);
        }

        public SqlPreCommandSimple AlterSchema(ObjectName oldName, SchemaName schemaName)
        {
            if (IsPostgres)
                return new SqlPreCommandSimple($"ALTER TABLE {oldName} SET SCHEMA {schemaName.Name.SqlEscape(IsPostgres)};");

            return new SqlPreCommandSimple("ALTER SCHEMA {0} TRANSFER {1};".FormatWith(schemaName.Name.SqlEscape(isPostgres), oldName));
        }

        public SqlPreCommand RenameColumn(ObjectName tableName, string oldName, string newName)
        {
            if (IsPostgres)
                return new SqlPreCommandSimple($"ALTER TABLE {tableName} RENAME COLUMN {oldName.SqlEscape(IsPostgres)} TO {newName.SqlEscape(IsPostgres)};");

            return SP_RENAME(tableName.Schema.Database, tableName.OnDatabase(null) + "." + oldName, newName, "COLUMN");
        }

        public SqlPreCommand RenameIndex(ObjectName tableName, string oldName, string newName)
        {
            if (IsPostgres)
                return new SqlPreCommandSimple($"ALTER INDEX {oldName.SqlEscape(IsPostgres)} RENAME TO {newName.SqlEscape(IsPostgres)};");

            return SP_RENAME(tableName.Schema.Database, tableName.OnDatabase(null) + "." + oldName, newName, "INDEX");
        }
        #endregion

        public SqlPreCommandSimple SetIdentityInsert(ObjectName tableName, bool value)
        {
            return new SqlPreCommandSimple("SET IDENTITY_INSERT {0} {1}".FormatWith(
                tableName, value ? "ON" : "OFF"));
        }

        public SqlPreCommandSimple SetSingleUser(string databaseName)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;".FormatWith(databaseName));
        }

        public SqlPreCommandSimple SetMultiUser(string databaseName)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET MULTI_USER;".FormatWith(databaseName));
        }

        public SqlPreCommandSimple SetSnapshotIsolation(string databaseName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET ALLOW_SNAPSHOT_ISOLATION {1};".FormatWith(databaseName, value ? "ON" : "OFF"));
        }

        public SqlPreCommandSimple MakeSnapshotIsolationDefault(string databaseName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET READ_COMMITTED_SNAPSHOT {1};".FormatWith(databaseName, value ? "ON" : "OFF"));
        }

        public SqlPreCommandSimple SelectRowCount()
        {
            return new SqlPreCommandSimple("select @@rowcount;");
        }

        public SqlPreCommand CreateSchema(SchemaName schemaName)
        {
            if (schemaName.Database == null)
                return new SqlPreCommandSimple("CREATE SCHEMA {0};".FormatWith(schemaName)) { GoAfter = true, GoBefore = true };
            else
                return new SqlPreCommandSimple($"EXEC('use {schemaName.Database}; EXEC sp_executesql N''CREATE SCHEMA {schemaName.Name}'' ');");
        }

        public SqlPreCommand DropSchema(SchemaName schemaName)
        {
            return new SqlPreCommandSimple("DROP SCHEMA {0};".FormatWith(schemaName)) { GoAfter = true, GoBefore = true };
        }

        public SqlPreCommandSimple DisableForeignKey(ObjectName tableName, string foreignKey)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} NOCHECK CONSTRAINT {1};".FormatWith(tableName, foreignKey));
        }

        public SqlPreCommandSimple EnableForeignKey(ObjectName tableName, string foreignKey)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} WITH CHECK CHECK CONSTRAINT {1};".FormatWith(tableName, foreignKey));
        }

        public SqlPreCommandSimple DisableIndex(ObjectName tableName, string indexName)
        {
            return new SqlPreCommandSimple("ALTER INDEX [{0}] ON {1} DISABLE;".FormatWith(indexName, tableName));
        }

        public SqlPreCommandSimple RebuildIndex(ObjectName tableName, string indexName)
        {
            return new SqlPreCommandSimple("ALTER INDEX [{0}] ON {1} REBUILD;".FormatWith(indexName, tableName));
        }

        public SqlPreCommandSimple DropPrimaryKeyConstraint(ObjectName tableName)
        {
            DatabaseName? db = tableName.Schema.Database;

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

        internal SqlPreCommand? DropStatistics(string tn, List<DiffStats> list)
        {
            if (list.IsEmpty())
                return null;

            return new SqlPreCommandSimple("DROP STATISTICS " + list.ToString(s => tn.SqlEscape(isPostgres) + "." + s.StatsName.SqlEscape(isPostgres), ",\r\n") + ";");
        }

        public SqlPreCommand TruncateTable(ObjectName tableName) => new SqlPreCommandSimple($"TRUNCATE TABLE {tableName};");
    }
}
