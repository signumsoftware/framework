using Microsoft.Identity.Client;
using Signum.Engine.Maps;
using Signum.Engine.Sync.Postgres;
using Signum.Engine.Sync.SqlServer;
using System.IO;

namespace Signum.Engine.Sync;

public static class SchemaGenerator
{
    public static SqlPreCommand? CreateSchemasScript()
    {
        Schema s = Schema.Current;
        var sqlBuilder = Connector.Current.SqlBuilder;
        var defaultSchema = SchemaName.Default(s.Settings.IsPostgres);

        var schemas = s.GetDatabaseTables()
            .Select(a => a.Name.Schema)
            .Where(sn => !sn.OnDatabase(null).Equals(defaultSchema) && !s.IsExternalDatabase(sn.Database))
            .Distinct();

        return schemas
            .Select(sqlBuilder.CreateSchema)
            .Combine(Spacing.Simple);
    }

    public static SqlPreCommand? CreatePartitioningFunctionScript()
    {
        Schema s = Schema.Current;
        var sqlBuilder = Connector.Current.SqlBuilder;
        var defaultSchema = SchemaName.Default(s.Settings.IsPostgres);

        var schemes = s.GetDatabaseTables().Where(a => a.PartitionScheme != null).Select(a => (a.Name.Schema.Database, scheme: a.PartitionScheme!)).Distinct();

        return SqlPreCommand.Combine(Spacing.Double,

            schemes
            .Select(a => (a.Database, a.scheme.PartitionFunction))
            .Distinct()
            .Select(a => sqlBuilder.CreateSqlPartitionFunction(a.PartitionFunction, a.Database))
            .Combine(Spacing.Simple),

            schemes
            .Select(a => sqlBuilder.CreateSqlPartitionScheme(a.scheme, a.Database))
            .Combine(Spacing.Simple)
        );
    }

    public static SqlPreCommand? CreateTablesScript()
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        Schema s = Schema.Current;

        List<ITable> tables = s.GetDatabaseTables().Where(t => !s.IsExternalDatabase(t.Name.Schema.Database)).ToList();

        SqlPreCommand? createTables = tables.Select(t =>
        {

            var table = sqlBuilder.CreateTableSql(t);

            if (sqlBuilder.IsPostgres && t.SystemVersioned != null)
            {
                return SqlPreCommand.Combine(Spacing.Simple, table,
                    sqlBuilder.CreateVersioningTrigger(t),
                    sqlBuilder.CreateSystemTableVersionLike(t)
                    );
            }

            return table;
        }).Combine(Spacing.Double)?.PlainSqlCommand();

        if (createTables != null)
            createTables.GoAfter = true;

        SqlPreCommand? foreignKeys = tables.Select(sqlBuilder.AlterTableForeignKeys).Combine(Spacing.Double)?.PlainSqlCommand();

        if (foreignKeys != null)
            foreignKeys.GoAfter = true;

        HashSet<string> fullTextSearchCatallogs = new HashSet<string>(); 

        SqlPreCommand? indices = tables.Select(t =>
        {
            var allIndexes = t.AllIndexes().Where(a => !a.PrimaryKey);

            fullTextSearchCatallogs.AddRange(allIndexes.OfType<FullTextTableIndex>().Select(a => a.CatallogName));

            var mainIndices = allIndexes.Select(ix => sqlBuilder.CreateIndex(ix, checkUnique: null)).Combine(Spacing.Simple);

            var historyIndices = t.SystemVersioned == null ? null :
                     allIndexes.Where(a => a.GetType() == typeof(TableIndex) && !a.Unique).Select(mix => sqlBuilder.CreateIndexBasic(mix, forHistoryTable: true)).Combine(Spacing.Simple);

            return SqlPreCommand.Combine(Spacing.Double, mainIndices, historyIndices);

        }).NotNull().Combine(Spacing.Double)?.PlainSqlCommand();

        if (indices != null)
            indices.GoAfter = true;

        if (fullTextSearchCatallogs.Any() && !Connector.Current.SupportsFullTextSearch)
            throw new InvalidOperationException("Current database does not support Full-Text Search");

        var catallogs = fullTextSearchCatallogs.Select(a => sqlBuilder.CreateFullTextCatallog(a)).Combine(Spacing.Simple);
        if (catallogs != null)
            catallogs.GoAfter = true;

        return SqlPreCommand.Combine(Spacing.Triple, createTables, foreignKeys, catallogs, indices);
    }

    public static SqlPreCommand? InsertEnumValuesScript()
    {
        var result = (from t in Schema.Current.Tables.Values
                      let enumType = EnumEntity.Extract(t.Type)
                      where enumType != null
                      select EnumEntity.GetEntities(enumType).Select((e, i) => t.InsertSqlSync(e, suffix: t.Name.Name + i)).Combine(Spacing.Simple)
                ).Combine(Spacing.Double)?.PlainSqlCommand();

        if (result != null)
            result.GoAfter = true;

        return result;
    }

    public static SqlPreCommand? CreatePostgresExtensions()
    {
        if (!Schema.Current.Settings.IsPostgres)
            return null;

        var s = Schema.Current;

        return s.PostgresExtensions.Where(kvp => kvp.Value(s)).Select(kvp => Connector.Current.SqlBuilder.CreateExtensionIfNotExist(kvp.Key)).Combine(Spacing.Simple);
    }


    public static SqlPreCommand? SnapshotIsolation()
    {
        var connector = Connector.Current;

        if (!connector.AllowsSetSnapshotIsolation)
            return null;

        var list = connector.Schema.DatabaseNames().Select(a => a?.Name).ToList();

        if (list.Contains(null))
        {
            list.Remove(null);
            list.Add(connector.DatabaseName());
        }

        var sqlBuilder = connector.SqlBuilder;

        var cmd = list.NotNull()
            .Select(dbn => new DatabaseName(null, dbn, connector.Schema.Settings.IsPostgres))
            .Where(db => !SnapshotIsolationEnabled(db))
            .Select(db => SqlPreCommand.Combine(Spacing.Simple,
                sqlBuilder.SetSingleUser(db),
                sqlBuilder.SetSnapshotIsolation(db, true),
                sqlBuilder.MakeSnapshotIsolationDefault(db, true),
                sqlBuilder.SetMultiUser(db))
            ).Combine(Spacing.Double);

        return cmd;

    }

    private static bool SnapshotIsolationEnabled(DatabaseName dbName)
    {
        //SQL Server Replication makes it hard to do ALTER DATABASE statments, so we are conservative even if Generate should not have Synchronize behaviour
        var result = Database.View<SysDatabases>().Where(s => s.name == dbName.Name).Select(a => a.is_read_committed_snapshot_on && a.snapshot_isolation_state).SingleOrDefaultEx();
        return result;
    }
}
