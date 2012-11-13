using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;

namespace Signum.Engine
{
    public static class SchemaGenerator
    {
        public static SqlPreCommand CreateTablesScript()
        {
            List<ITable> tables = Schema.Current.GetDatabaseTables().ToList();

            SqlPreCommand createTables = tables.Select(t => SqlBuilder.CreateTableSql(t)).Combine(Spacing.Double);

            SqlPreCommand foreignKeys = tables.Select(t => SqlBuilder.AlterTableForeignKeys(t)).Combine(Spacing.Double);
            
            SqlPreCommand indices = tables.Select(t => SqlBuilder.CreateAllIndices(t)).NotNull().Combine(Spacing.Double);

            return SqlPreCommand.Combine(Spacing.Triple, createTables, foreignKeys, indices);
        }

        public static SqlPreCommand InsertEnumValuesScript()
        {
            return (from t in Schema.Current.Tables.Values
                    let enumType = EnumEntity.Extract(t.Type)
                    where enumType != null
                    select (from ie in EnumEntity.GetEntities(enumType)
                            select t.InsertSqlSync(ie)).Combine(Spacing.Simple)).Combine(Spacing.Double);
        }
    }
}
