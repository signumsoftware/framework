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
            List<ITable> tables =s.GetDatabaseTables().Where(t => !s.IsExternalDatabase(t.Name.Schema.Database)).ToList();
            
            SqlPreCommand createTables = tables.Select(SqlBuilder.CreateTableSql).Combine(Spacing.Double).PlainSqlCommand();

            SqlPreCommand foreignKeys = tables.Select(SqlBuilder.AlterTableForeignKeys).Combine(Spacing.Double).PlainSqlCommand();

            SqlPreCommand indices = tables.Select(SqlBuilder.CreateAllIndices).NotNull().Combine(Spacing.Double).PlainSqlCommand();

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

            var cmd = list.Select(a =>
                SqlPreCommand.Combine(Spacing.Simple,
                //DisconnectUsers(a.name, "SPID" + i) : null,
                SqlBuilder.SetSingleUser(a),
                SqlBuilder.SetSnapshotIsolation(a, true),
                SqlBuilder.MakeSnapshotIsolationDefault(a, true),
                SqlBuilder.SetMultiUser(a))                              
                ).Combine(Spacing.Double);

            return cmd;
        }
        
    }
}