using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.SchemaInfoTables;

namespace Signum.Engine
{
    public static class SchemaGenerator
    {
        public static SqlPreCommand CreateSchemasScript()
        {
            Schema s = Schema.Current;

            return s.GetDatabaseTables()
                .Select(a => a.Name.Schema)
                .Where(sn => sn.Name != "dbo" && !s.IsExternalDatabase(sn.Database))
                .Distinct()
                .Select(SqlBuilder.CreateSchema)
                .Combine(Spacing.Simple);
        }

        public static SqlPreCommand CreateTablesScript()
        {
            Schema s = Schema.Current;
            List<ITable> tables = s.GetDatabaseTables().Where(t => !s.IsExternalDatabase(t.Name.Schema.Database)).ToList();
            
            SqlPreCommand createTables = tables.Select(SqlBuilder.CreateTableSql).Combine(Spacing.Double).PlainSqlCommand();

            SqlPreCommand foreignKeys = tables.Select(SqlBuilder.AlterTableForeignKeys).Combine(Spacing.Double).PlainSqlCommand();

            SqlPreCommand indices = tables.Select(t =>
            {
                var allIndexes = t.GeneratAllIndexes().Where(a => !(a is PrimaryClusteredIndex)); ;

                var mainIndices = allIndexes.Select(ix => SqlBuilder.CreateIndex(ix, checkUnique: null)).Combine(Spacing.Simple);

                var historyIndices = t.SystemVersioned == null ? null :
                         allIndexes.Where(a => a.GetType() == typeof(Index)).Select(mix => SqlBuilder.CreateIndexBasic(mix, forHistoryTable: true)).Combine(Spacing.Simple);

                return SqlPreCommand.Combine(Spacing.Double, mainIndices, historyIndices);

            }).NotNull().Combine(Spacing.Double).PlainSqlCommand();


            return SqlPreCommand.Combine(Spacing.Triple, createTables, foreignKeys, indices);
        }

        public static SqlPreCommand InsertEnumValuesScript()
        {
            return (from t in Schema.Current.Tables.Values
                    let enumType = EnumEntity.Extract(t.Type)
                    where enumType != null
                    select EnumEntity.GetEntities(enumType).Select((e, i) => t.InsertSqlSync(e, suffix: t.Name.Name + i)).Combine(Spacing.Simple)
                    ).Combine(Spacing.Double).PlainSqlCommand();
        }

      
        public static SqlPreCommand SnapshotIsolation()
        {
            if (!Connector.Current.AllowsSetSnapshotIsolation)
                return null;


            var list = Schema.Current.DatabaseNames().Select(a => a?.ToString()).ToList();

            if (list.Contains(null))
            {
                list.Remove(null);
                list.Add(Connector.Current.DatabaseName());
            }

            var cmd = list
                .Where(db => !SnapshotIsolationEnabled(db))
                .Select(a => SqlPreCommand.Combine(Spacing.Simple,
                    SqlBuilder.SetSingleUser(a),
                    SqlBuilder.SetSnapshotIsolation(a, true),
                    SqlBuilder.MakeSnapshotIsolationDefault(a, true),
                    SqlBuilder.SetMultiUser(a))                              
                ).Combine(Spacing.Double);

            return cmd;
        }

        private static bool SnapshotIsolationEnabled(string dbName)
        {
            //SQL Server Replication makes it hard to do ALTER DATABASE statments, so we are conservative even if Generate should not have Synchronize behaviour
            var result = Database.View<SysDatabases>().Where(s => s.name == dbName).Select(a => a.is_read_committed_snapshot_on && a.snapshot_isolation_state).SingleOrDefaultEx();
            return result;
        }
    }
}