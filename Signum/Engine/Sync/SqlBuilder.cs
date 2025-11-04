using System.Data;
using Microsoft.AspNetCore.Mvc.Formatters;
using Signum.Engine.Maps;
using Signum.Engine.Sync;
using Signum.Utilities;

#pragma warning disable CA1822 // Mark members as static

namespace Signum.Engine.Sync;

public class SqlBuilder
{
    Connector connector;
    bool isPostgres;

    public bool IsPostgres => isPostgres;

    internal SqlBuilder(Connector connector)
    {
        this.connector = connector;
        isPostgres = connector.Schema.Settings.IsPostgres;
    }

    public List<string> SystemSchemasPostgres = new List<string>();
    public List<string> SystemSchemasSqlServer = new List<string>()
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



    internal List<string> GetSystemSchemas(bool isPostgres)
    {
        if (isPostgres)
            return SystemSchemasPostgres;
        else
            return SystemSchemasSqlServer;
    }

    public SqlPreCommandSimple? UseDatabase(string? databaseName = null)
    {
        if (Schema.Current.Settings.IsPostgres)
            return null;

        return new SqlPreCommandSimple("use {0}".FormatWith((databaseName ?? Connector.Current.DatabaseName()).SqlEscape(Schema.Current.Settings.IsPostgres)));
    }

    #region Create Tables
    public SqlPreCommand CreateTableSql(ITable t, ObjectName? tableName = null, bool avoidSystemVersioning = false, bool forHistoryTable = false)
    {
        var primaryKeyConstraint = t.PrimaryKey == null || t.SystemVersioned != null && tableName != null && t.SystemVersioned.TableName.Equals(tableName) ? null :
            t.AllIndexes().SingleEx(a => a.PrimaryKey).Let(pk =>
            isPostgres ?
            $"CONSTRAINT {pk.IndexName.SqlEscape(isPostgres)} PRIMARY KEY ({t.PrimaryKey.Name.SqlEscape(isPostgres)})" :
            $"CONSTRAINT {pk.IndexName.SqlEscape(isPostgres)} PRIMARY KEY {(pk.Clustered ? "CLUSTERED" : "NONCLUSTERED")} ({t.PrimaryKey.Name.SqlEscape(isPostgres)} ASC)");

        var systemPeriod = t.SystemVersioned == null || IsPostgres || forHistoryTable || avoidSystemVersioning ? null : Period(t.SystemVersioned);

        var columns = t.Columns.Values.Select(c => ColumnLine(c, GetDefaultConstaint(t.Name, c), GetCheckConstaint(t, c), isChange: false, avoidSystemVersion: avoidSystemVersioning, forHistoryTable: forHistoryTable))
            .And(primaryKeyConstraint)
            .And(systemPeriod)
            .NotNull()
            .ToString(",\n");

        var systemVersioning = t.SystemVersioned == null || avoidSystemVersioning || IsPostgres ? null :
            $"\nWITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {t.SystemVersioned.TableName.OnDatabase(null)}))";

        var result = new SqlPreCommandSimple($"CREATE {(IsPostgres && t.Name.IsTemporal ? "TEMPORARY " : "")}TABLE {tableName ?? t.Name}(\n{columns}\n)" + systemVersioning + ";");


        return result;

    }

    public SqlPreCommandSimple CreateSystemTableVersionLike(ITable t)
    {
        return new SqlPreCommandSimple($"CREATE TABLE {t.SystemVersioned!.TableName}(LIKE {t.Name});");
    }

    public SqlPreCommand CreateVersioningTrigger(ITable t, bool replace = false)
    {
        return new SqlPreCommandSimple(@$"{(replace ? "CREATE OR REPLACE" : "CREATE")} TRIGGER versioning_trigger
BEFORE INSERT OR UPDATE OR DELETE ON {t.Name}
FOR EACH ROW EXECUTE PROCEDURE versioning({VersioningTriggerArgs(t.SystemVersioned!)});");
    }

    public string VersioningTriggerArgs(SystemVersionedInfo si)
    {
        return $"'{si.PostgresSysPeriodColumnName}', '{si.TableName}', true";
    }

    public SqlPreCommandSimple DropVersionningTrigger(ObjectName tableName, string triggerName)
    {
        return new SqlPreCommandSimple($"DROP TRIGGER {triggerName} ON {tableName};");
    }

    public SqlPreCommandSimple DisableVersionningTrigger(ObjectName tableName)
    {
        return new SqlPreCommandSimple($"ALTER TABLE {tableName} DISABLE TRIGGER versioning_trigger;");
    }

    public SqlPreCommandSimple EnableVersionningTrigger(ObjectName tableName)
    {
        return new SqlPreCommandSimple($"ALTER TABLE {tableName} ENABLE TRIGGER versioning_trigger;");
    }


    public SqlPreCommand DropTable(DiffTable diffTable, bool isPostgres)
    {
        if (diffTable.TemporalTableName == null)
            return DropTable(diffTable.Name);

        return SqlPreCommand.Combine(Spacing.Simple,
            isPostgres ? null : AlterTableDisableSystemVersioning(diffTable.Name),
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
        return new SqlPreCommandSimple($"CREATE EXTENSION IF NOT EXISTS \"{extensionName}\";");
    }

    public SqlPreCommandSimple DropExtension(string extensionName)
    {
        return new SqlPreCommandSimple($"DROP EXTENSION \"{extensionName}\";");
    }

    SqlPreCommand DropViewIndex(ObjectName viewName, string index)
    {
        return new[]{
                 DropIndex(viewName, index),
                 DropView(viewName)
            }.Combine(Spacing.Simple)!;
    }

    public SqlPreCommandSimple AlterTableAddPeriod(ITable table)
    {
        return new SqlPreCommandSimple($"ALTER TABLE {table.Name} ADD {Period(table.SystemVersioned!)};");
    }

    string? Period(SystemVersionedInfo sv)
    {

        if (!Connector.Current.SupportsTemporalTables)
            throw new InvalidOperationException($"The current connector '{Connector.Current}' does not support Temporal Tables");

        return $"PERIOD FOR SYSTEM_TIME ({sv.StartColumnName!.SqlEscape(isPostgres)}, {sv.EndColumnName!.SqlEscape(isPostgres)})";
    }

    public SqlPreCommandSimple AlterTableDropPeriod(ITable table)
    {
        return new SqlPreCommandSimple($"ALTER TABLE {table.Name} DROP PERIOD FOR SYSTEM_TIME;");
    }

    public SqlPreCommandSimple AlterTableEnableSystemVersioning(ITable table)
    {
        return new SqlPreCommandSimple($"ALTER TABLE {table.Name} SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = {table.SystemVersioned!.TableName.OnDatabase(null)}));");
    }

    public SqlPreCommandSimple AlterTableDisableSystemVersioning(ObjectName tableName)
    {
        return new SqlPreCommandSimple($"ALTER TABLE {tableName} SET (SYSTEM_VERSIONING = OFF);");
    }

    public SqlPreCommand AlterTableDropColumn(ITable table, string columnName, bool withHistory)
    {
        if (!withHistory)
            return AlterTableDropColumn(table.Name, columnName);

        return new SqlPreCommand_WithHistory(
            AlterTableDropColumn(table.Name, columnName),
            AlterTableDropColumn(table.SystemVersioned!.TableName, columnName)
        );
    }

    public SqlPreCommand AlterTableDropColumn(ObjectName tableName, string columnName)
    {
        return new SqlPreCommandSimple("ALTER TABLE {0} DROP COLUMN {1};".FormatWith(tableName, columnName.SqlEscape(isPostgres)));
    }

    public SqlPreCommand AlterTableAddColumn(ITable table, IColumn column, DefaultConstraint? tempDefault = null, bool forHistory = false) => AlterTableAddColumn(forHistory ? table.SystemVersioned!.TableName : table.Name, column, tempDefault, forHistory);
    public SqlPreCommand AlterTableAddColumn(ObjectName tableName, IColumn column, DefaultConstraint? tempDefault = null, bool forHistory = false)
    {
        return new SqlPreCommandSimple("ALTER TABLE {0} ADD {1};".FormatWith(tableName, ColumnLine(column, tempDefault ?? GetDefaultConstaint(tableName, column), checkConst: null, isChange: false, forHistoryTable: forHistory)));
    }

    public SqlPreCommand AlterTableAlterColumn(ITable table, IColumn column, DiffColumn diffColumn, ObjectName? forceTableName = null)
    {
        var tableName = forceTableName ?? table.Name;

        var alterColumn = !IsPostgres ?
             new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1};".FormatWith(tableName, ColumnLine(column, null, null, isChange: true))) :
             new[]
             {
                 !diffColumn.DbType.Equals(column.DbType) || diffColumn.Collation != column.Collation || !diffColumn.ScaleEquals(column) || !diffColumn.SizeEquals(column) ?
                 new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} TYPE {2};".FormatWith(tableName, column.Name.SqlEscape(isPostgres),  GetColumnType(column) + (column.Collation != null ? " COLLATE " + column.Collation : null))) : null,
                 diffColumn.Nullable &&  !column.Nullable.ToBool()? new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} SET NOT NULL;".FormatWith(tableName, column.Name.SqlEscape(isPostgres))) : null,
                 !diffColumn.Nullable && column.Nullable.ToBool()? new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} DROP NOT NULL;".FormatWith(tableName, column.Name.SqlEscape(isPostgres))) : null,
             }.Combine(Spacing.Simple) ?? new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1} -- UNEXPECTED COLUMN CHANGE!!".FormatWith(tableName, column.Name.SqlEscape(isPostgres)));

        return alterColumn;
    }

    public DefaultConstraint? GetDefaultConstaint(ObjectName tableNAme, IColumn c)
    {
        if (c.Default == null)
            return null;

        return new DefaultConstraint(c.Name, $"DF_{tableNAme.Name}_{c.Name}", Quote(c.DbType, c.Default));
    }

    public CheckConstraint? GetCheckConstaint(ITable t, IColumn c)
    {
        if (c.Check == null)
            return null;

        return new CheckConstraint(c.Name, $"CK_{t.Name.Name}_{c.Name}", c.Check);
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

    public class CheckConstraint
    {
        public string ColumnName;
        public string Name;
        public string Definition;

        public CheckConstraint(string columnName, string name, string quotedDefinition)
        {
            ColumnName = columnName;
            Name = name;
            Definition = quotedDefinition;
        }
    }

    public string ColumnLine(IColumn c, DefaultConstraint? defaultConst, CheckConstraint? checkConst, bool isChange, bool avoidSystemVersion = false, bool forHistoryTable = false)
    {
        string fullType = GetColumnType(c);

        var generatedAlways =
            c.ComputedColumn is { } ga ? (isPostgres ?
                $"GENERATED ALWAYS AS ({ga.Expression}) {(ga.Persisted ? "STORED" : null)}":
                $"AS ({ga.Expression}) {((ga.Persisted ? " PERSISTED" : null))}") :
            c is SystemVersionedInfo.SqlServerPeriodColumn svc && !forHistoryTable && !avoidSystemVersion ? $"GENERATED ALWAYS AS ROW {(svc.SystemVersionColumnType == SystemVersionedInfo.SystemVersionColumnType.Start ? "START" : "END")} HIDDEN" :
            null;

        var defaultConstraint = defaultConst != null ? $"CONSTRAINT {defaultConst.Name} DEFAULT " + defaultConst.QuotedDefinition : null;
        var checkConstraint = checkConst != null ? $"CONSTRAINT {checkConst.Name} CHECK " + checkConst.Definition : null;

        return $" ".Combine(
            c.Name.SqlEscape(isPostgres),
            fullType,
            c.Identity && !isChange && !forHistoryTable ? isPostgres ? "GENERATED ALWAYS AS IDENTITY" : "IDENTITY" : null,
            generatedAlways,
            c.Collation != null ? "COLLATE " + c.Collation : null,
            generatedAlways != null ? null : c.Nullable.ToBool() ? "NULL" : "NOT NULL",
            generatedAlways != null ? null: defaultConstraint,
            generatedAlways != null ? null: checkConstraint
            );
    }

    public string GetColumnType(IColumn c)
    {
        return c.UserDefinedTypeName ?? c.DbType.ToString(IsPostgres) + GetSizePrecisionScale(c);
    }

    public string GetColumnType(DiffColumn c)
    {
        return c.UserTypeName ?? c.DbType.ToString(IsPostgres) /*+ GetSizeScale(Math.Max(c.Length, c.Precision), c.Scale)*/;
    }

    public string GetSizePrecisionScale(IColumn c)
    {
        return GetSizePrecisionScale(c.Size, c.Precision, c.Scale, c.DbType.IsDecimal());
    }

    public string GetSizePrecisionScale(int? size, byte? precision, byte? scale, bool isDecimal)
    {
        if (size == null && precision == null)
            return "";

        if (isDecimal)
        {
            if (scale == null)
                return "({0})".FormatWith(precision);
            else
                return "({0},{1})".FormatWith(precision, scale);
        }

        if (size == int.MaxValue)
            return IsPostgres ? "" : "(MAX)";

        return "({0})".FormatWith(size);
    }

    public string Quote(AbstractDbType type, string @default)
    {
        if (type.IsString() && !(@default.StartsWith("'") && @default.StartsWith("'")))
            return "'" + @default + "'";

        return @default;
    }

    public SqlPreCommand? AlterTableForeignKeys(ITable t)
    {
        return t.Columns.Values.Select(c =>
            c.ReferenceTable == null || c.AvoidForeignKey ? null : AlterTableAddConstraintForeignKey(t, c.Name, c.ReferenceTable))
            .Combine(Spacing.Simple);
    }


    public SqlPreCommand DropIndex(ObjectName tableName, DiffIndex index)
    {
        if (index.IsPrimary)
            return AlterTableDropConstraint(tableName, new ObjectName(tableName.Schema, index.IndexName, isPostgres))!;

        if (index.FullTextIndex != null)
            return new SqlPreCommandSimple($@"DROP FULLTEXT INDEX ON {tableName}");

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
                .FormatWith(objectName.Schema.Database.ToString(), indexName.SqlEscape(isPostgres), objectName.OnDatabase(null).ToString()));
    }

    public SqlPreCommand DisconnectTableFromPartitionSchema(DiffTable table)
    {
        var pk = table.Columns.Values.SingleEx(a => a.PrimaryKey);
        string indexName = $"TEMP_PK_{table.Name.Name}";
        return new[]
        {
            new SqlPreCommandSimple($"CREATE CLUSTERED INDEX {indexName} ON {table.Name}({pk.Name}) ON [Primary]; -- Necessary to disconnect table from Partition Schema https://stackoverflow.com/a/55750977/38670"),
            new SqlPreCommandSimple($"DROP INDEX {indexName} ON {table.Name}"),
        }.Combine(Spacing.Simple)!;
    }

    public SqlPreCommand CreateIndex(TableIndex index, Replacements? checkUnique)
    {
        if (index.PrimaryKey)
        {
            var columns = index.Columns.ToString(c => c.Name.SqlEscape(isPostgres), ", ");

            return new SqlPreCommandSimple($"ALTER TABLE {index.Table.Name} ADD CONSTRAINT {index.IndexName.SqlEscape(isPostgres)} PRIMARY KEY {(IsPostgres ? "": index.Clustered? "CLUSTERED" : "NONCLUSTERED")}({columns}){(index.Partitioned ? $" ON {index.PartitionSchemeName} ({index.PartitionColumnName})" : null)};");
        }

        if (index.Unique)
        {
            if (index.ViewName != null)
            {
                ObjectName viewName = new ObjectName(index.Table.Name.Schema, index.ViewName, isPostgres);
                
                var columns = index.Columns.ToString(c => c.Name.SqlEscape(isPostgres), ", ");

                SqlPreCommandSimple viewSql = new SqlPreCommandSimple($"CREATE VIEW {viewName} WITH SCHEMABINDING AS SELECT {columns} FROM {index.Table.Name} WHERE {index.Where};")
                { GoBefore = true, GoAfter = true };

                SqlPreCommandSimple indexSql = new SqlPreCommandSimple($"CREATE UNIQUE CLUSTERED INDEX {index.IndexName.SqlEscape(isPostgres)} ON {viewName}({columns});");

                return SqlPreCommand.Combine(Spacing.Simple,
                    checkUnique != null ? RemoveDuplicatesIfNecessary(index, checkUnique) : null,
                    viewSql,
                    indexSql)!;
            }
            else
            {
                return SqlPreCommand.Combine(Spacing.Double,
                    checkUnique != null ? RemoveDuplicatesIfNecessary(index, checkUnique) : null,
                    CreateIndexBasic(index, forHistoryTable: false))!;
            }
        }
        else if(index is FullTextTableIndex ftindex)
        {
            if (!isPostgres)
            {
                var sqls = ftindex.SqlServer;

                var pk = ftindex.Table.AllIndexes().SingleEx(a => a.PrimaryKey);

                var columns = index.Columns.ToString(c => c.Name.SqlEscape(isPostgres), ", ");

                var options = new[]
                {
                    sqls.ChangeTraking != null ? "CHANGE_TRACKING = " + GetSqlserverString(sqls.ChangeTraking.Value) : null,
                    sqls.StoplistName != null ? "STOPLIST =" + sqls.StoplistName : null,
                    sqls.PropertyListName != null ? "SEARCH PROPERTY LIST=" + sqls.PropertyListName : null,
                }.NotNull().ToList();

                SqlPreCommandSimple indexSql = new SqlPreCommandSimple(new string?[]
                {
                    $"CREATE FULLTEXT INDEX ON {ftindex.Table.Name}({columns})",
                    $"KEY INDEX {pk.IndexName}",
                    $"ON {sqls.CatallogName}",
                    options.Any() ? "WITH " + options.ToString(", ") : null
                }.ToString("\n"))
                { NoTransaction = NoTransactionMode.AfterScript };

                return indexSql;
            }
            else
            {
                var pg = ftindex.Postgres;

                return new SqlPreCommandSimple($"CREATE INDEX {ftindex.IndexName} ON {ftindex.Table} USING GIN ({pg.TsVectorColumnName.SqlEscape(true)});");
            }
        }
        else
        {
            return CreateIndexBasic(index, forHistoryTable: false);
        }
    }

    private string GetSqlserverString(FullTextIndexChangeTracking changeTraking) => changeTraking switch
    {
        FullTextIndexChangeTracking.Manual => "MANUAL",
        FullTextIndexChangeTracking.Auto => "AUTO",
        FullTextIndexChangeTracking.Off => "OFF",
        FullTextIndexChangeTracking.Off_NoPopulation => "OFF, NO POPULATION",
        _ => throw new UnexpectedValueException(changeTraking)
    };

    public int DuplicateCount(TableIndex uniqueIndex, Replacements rep)
    {
        var primaryKey = uniqueIndex.Table.Columns.Values.Where(a => a.PrimaryKey).Only();

        if (primaryKey == null)
            throw new InvalidOperationException("No primary key found");

        var oldTableName = rep.Apply(Replacements.KeyTablesInverse, uniqueIndex.Table.Name.ToString());

        var columnReplacement = rep.TryGetC(Replacements.KeyColumnsForTable(uniqueIndex.Table.Name.ToString()))?.Inverse() ?? new Dictionary<string, string>();

        var oldColumns = uniqueIndex.Columns.ToString(c => (columnReplacement.TryGetC(c.Name) ?? c.Name).SqlEscape(isPostgres), ", ");

        var oldPrimaryKey = columnReplacement.TryGetC(primaryKey.Name) ?? primaryKey.Name;

        if (isPostgres) // min not defined for uuid
        {
            return Convert.ToInt32(Executor.ExecuteScalar(
    $@"SELECT Count(*) FROM {oldTableName}
WHERE {oldPrimaryKey.SqlEscape(IsPostgres)} NOT IN
(
    SELECT DISTINCT ON ({oldColumns}) {oldPrimaryKey.SqlEscape(IsPostgres)}
    FROM {oldTableName}
    {(!uniqueIndex.Where.HasText() ? "" : "WHERE " + uniqueIndex.Where.Replace(columnReplacement))}
){(!uniqueIndex.Where.HasText() ? "" : $" AND ({uniqueIndex.Where.Replace(columnReplacement)})")}")!);
        }
        else
        {
            return Convert.ToInt32(Executor.ExecuteScalar(
    $@"SELECT Count(*) FROM {oldTableName}
WHERE {oldPrimaryKey.SqlEscape(IsPostgres)} NOT IN
(
    SELECT MIN({oldPrimaryKey.SqlEscape(IsPostgres)})
    FROM {oldTableName}
    {(!uniqueIndex.Where.HasText() ? "" : "WHERE " + uniqueIndex.Where.Replace(columnReplacement))}
    GROUP BY {oldColumns}
){(!uniqueIndex.Where.HasText() ? "" : $" AND ({uniqueIndex.Where.Replace(columnReplacement)})")}")!);
        }
    }

    public SqlPreCommand? RemoveDuplicatesIfNecessary(TableIndex uniqueIndex, Replacements rep)
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

    private SqlPreCommand RemoveDuplicates(TableIndex uniqueIndex, IColumn primaryKey, string columns, bool commentedOut)
    {
        //Postgress doesn't have min on uuid
        var minId = isPostgres && primaryKey.Type == typeof(Guid) ? $"MIN({primaryKey.Name}::text)::uuid" : $"MIN({primaryKey.Name})";

        return new SqlPreCommandSimple($"""
            DELETE FROM {uniqueIndex.Table.Name}
            WHERE {primaryKey.Name} NOT IN
            (
                SELECT {minId}
                FROM {uniqueIndex.Table.Name}
                {(string.IsNullOrWhiteSpace(uniqueIndex.Where) ? "" : "WHERE " + uniqueIndex.Where)}
                GROUP BY {columns}
            ){(string.IsNullOrWhiteSpace(uniqueIndex.Where) ? "" : " AND " + uniqueIndex.Where)};
            """
            .Let(txt => commentedOut ? txt.Indent(2, '-') : txt));

    }

    public SqlPreCommand CreateIndexBasic(TableIndex index, bool forHistoryTable)
    {
        var indexType = " ".Combine(index.Unique ? "UNIQUE" : null, index.Clustered ? "CLUSTERED" : null, "INDEX");
        var tableName = forHistoryTable ? index.Table.SystemVersioned!.TableName : index.Table.Name;
        
        var columns = index.Columns.ToString(c => c.Name.SqlEscape(isPostgres), ", ");
        var include = index.IncludeColumns.HasItems() ? $" INCLUDE ({index.IncludeColumns.ToString(c => c.Name.SqlEscape(isPostgres), ", ")})" : null;
        var where = index.Where.HasText() ? $" WHERE {index.Where}" : null;
        var partitioning = index.Partitioned ? $" ON {index.PartitionSchemeName}({index.PartitionColumnName})" : " ON 'PRIMARY'";


        if (!isPostgres )

            return new SqlPreCommandSimple($"CREATE {indexType} {index.GetIndexName(tableName).SqlEscape(isPostgres)} ON {tableName}({columns}){include}{where}{partitioning};");
        else
            return new SqlPreCommandSimple($"CREATE {indexType} {index.GetIndexName(tableName).SqlEscape(isPostgres)} ON {tableName}({columns}){include}{where};");

    }



    internal SqlPreCommand? RecreateDiffIndex(ITable tab, DiffIndex dix)
    {

        try
        {
            if (dix.IsPrimary)
                throw new InvalidOperationException("Unable to re-create primary-key index");

            if (dix.FullTextIndex != null)
                throw new InvalidOperationException("Unable to re-create full-text-search index");
        }
        catch (Exception e)
        {
            return new SqlPreCommandSimple("-- " + e.Message);
        }   

        var indexType = dix.IsUnique ? "UNIQUE INDEX" : "INDEX";
        var columns = dix.Columns.Where(a => !a.IsIncluded).ToString(c => c.ColumnName.SqlEscape(isPostgres) + (c.IsDescending?" DESC" :""), ", ");
        var include = dix.Columns.Where(a => a.IsIncluded).HasItems() ? $" INCLUDE ({dix.Columns.Where(a => a.IsIncluded).ToString(c => c.ColumnName.SqlEscape(isPostgres), ", ")})" : null;
        var where = dix.FilterDefinition.HasText() ? $" WHERE {dix.FilterDefinition}" : "";
        var tableName = tab.Name;

        return new SqlPreCommandSimple($"CREATE {indexType} {dix.IndexName.SqlEscape(isPostgres)} ON {tableName}({columns}){include}{where};");
    }

    internal SqlPreCommand UpdateTrim(ITable tab, IColumn tabCol)
    {
        return new SqlPreCommandSimple("UPDATE {0} SET {1} = RTRIM({1});".FormatWith(tab.Name, tabCol.Name)); ;
    }


    public SqlPreCommand? AlterTableDropConstraint(ObjectName tableName, ObjectName foreignKeyName) =>
        AlterTableDropConstraint(tableName, foreignKeyName.Name);

    public SqlPreCommand? AlterTableDropConstraint(ObjectName tableName, string constraintName)
    {
        return new SqlPreCommandSimple("ALTER TABLE {0} DROP CONSTRAINT {1};".FormatWith(
            tableName,
            constraintName.SqlEscape(isPostgres)));
    }

    public SqlPreCommand AlterTableDropDefaultConstaint(ObjectName tableName, DiffColumn column) => 
        AlterTableDropDefaultConstaint(tableName, column.Name, column.DefaultConstraint!.Name!);
    public SqlPreCommand AlterTableDropDefaultConstaint(ObjectName tableName, string columnName, string constraintName)
    {
        if (isPostgres)
            return new SqlPreCommandSimple($"ALTER TABLE {tableName} ALTER COLUMN {columnName.SqlEscape(isPostgres)} DROP DEFAULT;");
        else
            return AlterTableDropConstraint(tableName, constraintName)!;
    }

    public SqlPreCommandSimple AlterTableAddDefaultConstraint(ObjectName tableName, DefaultConstraint defCons)
    {
        if (isPostgres)
            return new SqlPreCommandSimple($"ALTER TABLE {tableName} ALTER COLUMN {defCons.ColumnName.SqlEscape(IsPostgres)} SET DEFAULT {defCons.QuotedDefinition};");
        else
            return new SqlPreCommandSimple($"ALTER TABLE {tableName} ADD CONSTRAINT {defCons.Name.SqlEscape(IsPostgres)} DEFAULT {defCons.QuotedDefinition} FOR {defCons.ColumnName.SqlEscape(IsPostgres)};");
    }

    public SqlPreCommandSimple AlterTableAddCheckConstraint(ObjectName tableName, CheckConstraint checkCons)
    {
        return new SqlPreCommandSimple($"ALTER TABLE {tableName} ADD CONSTRAINT {checkCons.Name.SqlEscape(IsPostgres)} CHECK {checkCons.Definition};");
    }

    public SqlPreCommand? AlterTableAddConstraintForeignKey(ITable table, string fieldName, ITable foreignTable)
    {
        return AlterTableAddConstraintForeignKey(table.Name, fieldName, foreignTable.Name, foreignTable.PrimaryKey.Name);
    }

    public SqlPreCommand? AlterTableAddConstraintForeignKey(ObjectName parentTable, string parentColumn, ObjectName targetTable, string targetPrimaryKey)
    {
        if (!Equals(parentTable.Schema.Database, targetTable.Schema.Database))
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
        var result = (isPostgres ? "fk_{0}_{1}" : "FK_{0}_{1}").FormatWith(table, fieldName);

        return StringHashEncoder.ChopHash(result, connector.MaxNameLength, isPostgres);
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
            database == null ? null : SchemaName.Default(isPostgres).OnDatabase(database).ToString() + ".",
            oldName,
            newName,
            objectType == null ? null : ", '{0}'".FormatWith(objectType)
            ));
    }

    public SqlPreCommand RenameOrChangeSchema(ObjectName oldTableName, ObjectName newTableName)
    {
        if (!Equals(oldTableName.Schema.Database, newTableName.Schema.Database))
            throw new InvalidOperationException("Different database");

        if (Equals(oldTableName.Schema, newTableName.Schema))
            return RenameTable(oldTableName, newTableName.Name);

        var oldNewSchema = oldTableName.OnSchema(newTableName.Schema);

        return SqlPreCommand.Combine(Spacing.Simple,
            AlterSchema(oldTableName, newTableName.Schema),
            oldNewSchema.Equals(newTableName) ? null : RenameTable(oldNewSchema, newTableName.Name))!;
    }

    public SqlPreCommand RenameOrMove(DiffTable oldTable, ITable newTable, ObjectName newTableName, bool forHistoryTable)
    {
        if (Equals(oldTable.Name.Schema.Database, newTableName.Schema.Database))
            return RenameOrChangeSchema(oldTable.Name, newTableName);

        return SqlPreCommand.Combine(Spacing.Simple,
          CreateTableSql(newTable, newTableName, avoidSystemVersioning: true, forHistoryTable: forHistoryTable),
          MoveRows(oldTable.Name, newTableName, newTable.Columns.Keys, identityInsert: newTable.Columns.Values.Any(c => c.PrimaryKey && c.Identity) && !forHistoryTable),
          DropTable(oldTable, isPostgres))!;
    }

    public SqlPreCommand MoveRows(ObjectName oldTable, ObjectName newTable, IEnumerable<string> columnNames, bool identityInsert = false)
    {
        SqlPreCommandSimple command = new SqlPreCommandSimple(
@"INSERT INTO {0} ({2})
SELECT {3}
FROM {1} as [table];".FormatWith(
               newTable,
               oldTable,
               columnNames.ToString(a => a.SqlEscape(isPostgres), ", "),
               columnNames.ToString(a => "[table]." + a.SqlEscape(isPostgres), ", ")));

        if (!identityInsert)
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

    public SqlPreCommand RenameColumn(ITable table, string oldName, string newName, bool withHistory)
    {
        var normal = RenameColumn(table.Name, oldName, newName);

        if (!withHistory)
            return normal;
     
        return new SqlPreCommand_WithHistory(
            normal: normal,
            history: RenameColumn(table.SystemVersioned!.TableName, oldName, newName)
        );
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
            return new SqlPreCommandSimple($"ALTER INDEX {new ObjectName(tableName.Schema, oldName, tableName.IsPostgres)} RENAME TO {newName.SqlEscape(IsPostgres)};");

        return SP_RENAME(tableName.Schema.Database, tableName.OnDatabase(null) + "." + oldName, newName, "INDEX");
    }
    #endregion

    public SqlPreCommandSimple SetIdentityInsert(ObjectName tableName, bool value)
    {
        return new SqlPreCommandSimple("SET IDENTITY_INSERT {0} {1}".FormatWith(
            tableName, value ? "ON" : "OFF"));
    }

    public SqlPreCommandSimple SetSingleUser(DatabaseName databaseName)
    {
        return new SqlPreCommandSimple("ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;".FormatWith(databaseName)) { NoTransaction = NoTransactionMode.BeforeScript };
    }

    public SqlPreCommandSimple SetMultiUser(DatabaseName databaseName)
    {
        return new SqlPreCommandSimple("ALTER DATABASE {0} SET MULTI_USER;".FormatWith(databaseName)) { NoTransaction = NoTransactionMode.BeforeScript };
    }

    public SqlPreCommandSimple SetSnapshotIsolation(DatabaseName databaseName, bool value)
    {
        return new SqlPreCommandSimple("ALTER DATABASE {0} SET ALLOW_SNAPSHOT_ISOLATION {1};".FormatWith(databaseName, value ? "ON" : "OFF")) { NoTransaction = NoTransactionMode.BeforeScript };
    }

    public SqlPreCommandSimple MakeSnapshotIsolationDefault(DatabaseName databaseName, bool value)
    {
        return new SqlPreCommandSimple("ALTER DATABASE {0} SET READ_COMMITTED_SNAPSHOT {1};".FormatWith(databaseName, value ? "ON" : "OFF")) { NoTransaction = NoTransactionMode.BeforeScript };
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
            .Replace("DB.", db == null ? null : db.ToString() + ".")
            .Replace("@sql", "@" + varName)
            .Replace("{FullTable}", tableName.ToString())
            .Replace("{Table}", tn.ToString());

        return new SqlPreCommandSimple(command);
    }

    internal SqlPreCommand? DropStatistics(string tn, List<DiffStats> list)
    {
        if (list.IsEmpty())
            return null;

        return new SqlPreCommandSimple("DROP STATISTICS " + list.ToString(s => tn.SqlEscape(isPostgres) + "." + s.StatsName.SqlEscape(isPostgres), ",\n") + ";");
    }

    public SqlPreCommand TruncateTable(ObjectName tableName) => new SqlPreCommandSimple($"TRUNCATE TABLE {tableName};");

    public SqlPreCommand? CreateFullTextCatallog(FullTextCatallogName newSN) => WrapUseDatabase(newSN.Database, CreateFullTextCatallog(newSN.Name));
    public SqlPreCommand CreateFullTextCatallog(string catallogName)
    {
        return new SqlPreCommandSimple("CREATE FULLTEXT CATALOG " + catallogName) { NoTransaction = NoTransactionMode.BeforeScript, GoAfter = true };
    }

    public SqlPreCommand? DropFullTextCatallog(FullTextCatallogName newSN) => WrapUseDatabase(newSN.Database, DropFullTextCatallog(newSN.Name));
    public SqlPreCommand DropFullTextCatallog(string catallogName)
    {
        return new SqlPreCommandSimple("DROP FULLTEXT CATALOG " + catallogName) { NoTransaction = NoTransactionMode.AfterScript, GoAfter = true };
    }
    
    SqlPreCommand WrapUseDatabase(DatabaseName? db, SqlPreCommand command)
    {
        if (db == null)
            return command;

        return new[]
        {
            UseDatabase(db.Name),
            command,
            UseDatabase(null),
        }.Combine(Spacing.Simple)!;
    }




    public SqlPreCommand CreateSqlPartitionFunction(SqlPartitionFunction partFunction, DatabaseName? dbName)
    {
        return WrapUseDatabase(dbName, new SqlPreCommandSimple($"""
CREATE PARTITION FUNCTION {partFunction.Name.SqlEscape(isPostgres: false)} ({partFunction.DbType.ToString(isPostgres: false)})
    AS RANGE LEFT FOR VALUES ({partFunction.Points.Select(p => SqlPreCommandSimple.LiteralValue(p, simple: true)).ToString(", ")});
"""));
    }

    public SqlPreCommand DropSqlPartitionFunction(DiffPartitionFunction partFunction)
    {
        return WrapUseDatabase(partFunction.DatabaseName,
            new SqlPreCommandSimple($"""DROP PARTITION FUNCTION {partFunction.FunctionName.SqlEscape(isPostgres: false)}"""));
    }

    public SqlPreCommand CreateSqlPartitionScheme(SqlPartitionScheme partScheme, DatabaseName? dbName)
    {
        var isPostgres = false;
        return WrapUseDatabase(dbName, new SqlPreCommandSimple($"""
CREATE PARTITION SCHEME {partScheme.Name.SqlEscape(isPostgres)} 
    AS PARTITION {partScheme.PartitionFunction.Name.SqlEscape(isPostgres)}
    {(partScheme.FileGroupNames is string s ? $"ALL TO ({s.SqlEscape(isPostgres)})" :
    partScheme.FileGroupNames is string[] ss ? $"TO ({ss.ToString(s => s.SqlEscape(isPostgres), ", ")})" :
    throw new UnexpectedValueException(partScheme.FileGroupNames)
    )}
"""));
    }

    public SqlPreCommand DropSqlPartitionScheme(DiffPartitionScheme partSchema)
    {
        return WrapUseDatabase(partSchema.DatabaseName,
            new SqlPreCommandSimple($"""DROP PARTITION SCHEME {partSchema.SchemeName.SqlEscape(isPostgres: false)}"""));
    }

    internal SqlPreCommandSimple AlterTableChangeOwner(ObjectName name, string executeAs)
    {
        return new SqlPreCommandSimple($"ALTER TABLE {name} OWNER TO {executeAs};");
    }

    internal SqlPreCommandSimple AlterSchemaChangeOwner(SchemaName name, string executeAs)
    {
        return new SqlPreCommandSimple($"ALTER SCHEMA {name} OWNER TO {executeAs};");
    }
}
