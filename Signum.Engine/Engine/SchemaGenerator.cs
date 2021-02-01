using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.SchemaInfoTables;
using System.IO;

namespace Signum.Engine
{
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

        public static SqlPreCommand? CreateTablesScript()
        {
            var sqlBuilder = Connector.Current.SqlBuilder;
            Schema s = Schema.Current;
            List<ITable> tables = s.GetDatabaseTables().Where(t => !s.IsExternalDatabase(t.Name.Schema.Database)).ToList();

            SqlPreCommand? createTables = tables.Select(t => sqlBuilder.CreateTableSql(t)).Combine(Spacing.Double)?.PlainSqlCommand();

            SqlPreCommand? foreignKeys = tables.Select(sqlBuilder.AlterTableForeignKeys).Combine(Spacing.Double)?.PlainSqlCommand();

            SqlPreCommand? indices = tables.Select(t =>
            {
                var allIndexes = t.GeneratAllIndexes().Where(a => !(a is PrimaryKeyIndex)); ;

                var mainIndices = allIndexes.Select(ix => sqlBuilder.CreateIndex(ix, checkUnique: null)).Combine(Spacing.Simple);

                var historyIndices = t.SystemVersioned == null ? null :
                         allIndexes.Where(a => a.GetType() == typeof(TableIndex)).Select(mix => sqlBuilder.CreateIndexBasic(mix, forHistoryTable: true)).Combine(Spacing.Simple);

                return SqlPreCommand.Combine(Spacing.Double, mainIndices, historyIndices);

            }).NotNull().Combine(Spacing.Double)?.PlainSqlCommand();


            return SqlPreCommand.Combine(Spacing.Triple, createTables, foreignKeys, indices);
        }

        public static SqlPreCommand? InsertEnumValuesScript()
        {
            return (from t in Schema.Current.Tables.Values
                    let enumType = EnumEntity.Extract(t.Type)
                    where enumType != null
                    select EnumEntity.GetEntities(enumType).Select((e, i) => t.InsertSqlSync(e, suffix: t.Name.Name + i)).Combine(Spacing.Simple)
                    ).Combine(Spacing.Double)?.PlainSqlCommand();
        }

        public static SqlPreCommand? PostgresExtensions()
        {
            if (!Schema.Current.Settings.IsPostgres)
                return null;

            return Schema.Current.PostgresExtensions.Select(p => Connector.Current.SqlBuilder.CreateExtensionIfNotExist(p)).Combine(Spacing.Simple);
        }

        public static SqlPreCommand? PostgreeTemporalTableScript()
        {
            if (!Schema.Current.Settings.IsPostgres)
                return null;

            if (!Schema.Current.Tables.Any(t => t.Value.SystemVersioned != null))
                return null;

            var file = Schema.Current.Settings.PostresVersioningFunctionNoChecks ?
                "versioning_function_nochecks.sql" :
                "versioning_function.sql";

            var text = new StreamReader(typeof(Schema).Assembly.GetManifestResourceStream($"Signum.Engine.Engine.Scripts.{file}")!).Using(a => a.ReadToEnd());

            return new SqlPreCommandSimple(text);
        }

        public static SqlPreCommand? SnapshotIsolation()
        {
            var connector = Connector.Current;

            if (!connector.AllowsSetSnapshotIsolation)
                return null;


            var list = connector.Schema.DatabaseNames().Select(a => a?.ToString()).ToList();

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
}
