using Signum.Engine.Maps;
using Signum.Engine.SchemaInfoTables;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Signum.Engine.Engine
{
    public static class SysTablesSchema
    {
        public static Dictionary<string, DiffTable> GetDatabaseDescription(List<DatabaseName?> databases)
        {
            List<DiffTable> allTables = new List<DiffTable>();

            var isPostgres = false;

            foreach (var db in databases)
            {
                SafeConsole.WriteColor(ConsoleColor.Cyan, '.');

                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    var databaseName = db == null ? Connector.Current.DatabaseName() : db.Name;

                    var sysDb = Database.View<SysDatabases>().Single(a => a.name == databaseName);

                    var con = Connector.Current;

                    var tables =
                        (from s in Database.View<SysSchemas>()
                         from t in s.Tables().Where(t => !t.ExtendedProperties().Any(a => a.name == "microsoft_database_tools_support")) //IntelliSense bug
                         select new DiffTable
                         {
                             Name = new ObjectName(new SchemaName(db, s.name, isPostgres), t.name, isPostgres),

                             TemporalType = !con.SupportsTemporalTables ? SysTableTemporalType.None : t.temporal_type,

                             Period = !con.SupportsTemporalTables ? null :
                             (from p in t.Periods()
                              join sc in t.Columns() on p.start_column_id equals sc.column_id
                              join ec in t.Columns() on p.end_column_id equals ec.column_id
#pragma warning disable CS0472
                              select (int?)p.object_id == null ? null : new DiffPeriod
#pragma warning restore CS0472
                                                  {
                                  StartColumnName = sc.name,
                                  EndColumnName = ec.name,
                              }).SingleOrDefaultEx(),

                             TemporalTableName = !con.SupportsTemporalTables || t.history_table_id == null ? null :
                                 Database.View<SysTables>()
                                 .Where(ht => ht.object_id == t.history_table_id)
                                 .Select(ht => new ObjectName(new SchemaName(db, ht.Schema().name, isPostgres), ht.name, isPostgres))
                                 .SingleOrDefault(),

                             PrimaryKeyName = (from k in t.KeyConstraints()
                                               where k.type == "PK"
                                               select k.name == null ? null : new ObjectName(new SchemaName(db, k.Schema().name, isPostgres), k.name, isPostgres))
                                               .SingleOrDefaultEx(),

                             Columns = (from c in t.Columns()
                                        join userType in Database.View<SysTypes>().DefaultIfEmpty() on c.user_type_id equals userType.user_type_id
                                        join sysType in Database.View<SysTypes>().DefaultIfEmpty() on c.system_type_id equals sysType.user_type_id
                                        join ctr in Database.View<SysDefaultConstraints>().DefaultIfEmpty() on c.default_object_id equals ctr.object_id
                                        select new DiffColumn
                                        {
                                            Name = c.name,
                                            DbType = new AbstractDbType(sysType == null ? SqlDbType.Udt : ToSqlDbType(sysType.name)),
                                            UserTypeName = sysType == null ? userType.name : null,
                                            Nullable = c.is_nullable,
                                            Collation = c.collation_name == sysDb.collation_name ? null : c.collation_name,
                                            Length = c.max_length,
                                            Precision = c.precision,
                                            Scale = c.scale,
                                            Identity = c.is_identity,
                                            GeneratedAlwaysType = con.SupportsTemporalTables ? c.generated_always_type : GeneratedAlwaysType.None,
                                            DefaultConstraint = ctr.name == null ? null : new DiffDefaultConstraint
                                            {
                                                Name = ctr.name,
                                                Definition = ctr.definition
                                            },
                                            PrimaryKey = t.Indices().Any(i => i.is_primary_key && i.IndexColumns().Any(ic => ic.column_id == c.column_id)),
                                        }).ToDictionaryEx(a => a.Name, "columns"),

                             MultiForeignKeys = (from fk in t.ForeignKeys()
                                                 join rt in Database.View<SysTables>() on fk.referenced_object_id equals rt.object_id
                                                 select new DiffForeignKey
                                                 {
                                                     Name = new ObjectName(new SchemaName(db, fk.Schema().name, isPostgres), fk.name, isPostgres),
                                                     IsDisabled = fk.is_disabled,
                                                     TargetTable = new ObjectName(new SchemaName(db, rt.Schema().name, isPostgres), rt.name, isPostgres),
                                                     Columns = fk.ForeignKeyColumns().Select(fkc => new DiffForeignKeyColumn
                                                     {
                                                         Parent = t.Columns().Single(c => c.column_id == fkc.parent_column_id).name,
                                                         Referenced = rt.Columns().Single(c => c.column_id == fkc.referenced_column_id).name
                                                     }).ToList(),
                                                 }).ToList(),

                             SimpleIndices = (from i in t.Indices()
                                              where /*!i.is_primary_key && */i.type != 0  /*heap indexes*/
                                              select new DiffIndex
                                              {
                                                  IsUnique = i.is_unique,
                                                  IsPrimary = i.is_primary_key,
                                                  IndexName = i.name,
                                                  FilterDefinition = i.filter_definition,
                                                  Type = (DiffIndexType)i.type,
                                                  Columns = (from ic in i.IndexColumns()
                                                             join c in t.Columns() on ic.column_id equals c.column_id
                                                             orderby ic.index_column_id
                                                             select new DiffIndexColumn { ColumnName = c.name, IsIncluded = ic.is_included_column }).ToList()
                                              }).ToList(),

                             ViewIndices = (from v in Database.View<SysViews>()
                                            where v.name.StartsWith("VIX_" + t.name + "_")
                                            from i in v.Indices()
                                            select new DiffIndex
                                            {
                                                IsUnique = i.is_unique,
                                                ViewName = v.name,
                                                IndexName = i.name,
                                                Columns = (from ic in i.IndexColumns()
                                                           join c in v.Columns() on ic.column_id equals c.column_id
                                                           orderby ic.index_column_id
                                                           select new DiffIndexColumn { ColumnName = c.name, IsIncluded = ic.is_included_column }).ToList()

                                            }).ToList(),

                             Stats = (from st in t.Stats()
                                      where st.user_created
                                      select new DiffStats
                                      {
                                          StatsName = st.name,
                                          Columns = (from ic in st.StatsColumns()
                                                     join c in t.Columns() on ic.column_id equals c.column_id
                                                     select c.name).ToList()
                                      }).ToList(),

                         }).ToList();

                    if (SchemaSynchronizer.IgnoreTable != null)
                        tables.RemoveAll(SchemaSynchronizer.IgnoreTable);

                    tables.ForEach(t => t.FixSqlColumnLengthSqlServer());
                    tables.ForEach(t => t.ForeignKeysToColumns());

                    allTables.AddRange(tables);
                }
            }

            var database = allTables.ToDictionary(t => t.Name.ToString());

            return database;
        }

        public static SqlDbType ToSqlDbType(string str)
        {
            if (str == "numeric")
                return SqlDbType.Decimal;

            return str.ToEnum<SqlDbType>(true);
        }


        public static HashSet<SchemaName> GetSchemaNames(List<DatabaseName?> list)
        {
            var sqlBuilder = Connector.Current.SqlBuilder;
            var isPostgres = false;
            HashSet<SchemaName> result = new HashSet<SchemaName>();
            foreach (var db in list)
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    var schemaNames = Database.View<SysSchemas>().Select(s => s.name).ToList().Except(sqlBuilder.SystemSchemas);

                    result.AddRange(schemaNames.Select(sn => new SchemaName(db, sn, isPostgres)).Where(a => !SchemaSynchronizer.IgnoreSchema(a)));
                }
            }
            return result;
        }
    }
}
