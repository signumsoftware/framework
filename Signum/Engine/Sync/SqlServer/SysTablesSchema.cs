using Signum.Engine.Maps;
using System.Data;

namespace Signum.Engine.Sync.SqlServer;

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
                     let fti = t.FullTextSearchIndex()
                     select new DiffTable
                     {
                         Name = new ObjectName(new SchemaName(db, s.name, isPostgres), t.name, isPostgres),

                         TemporalType = !con.SupportsTemporalTables ? SysTableTemporalType.None : t.temporal_type,

                         Period = !con.SupportsTemporalTables ? null :
                         (from p in t.Periods()
                          join sc in t.Columns() on p.start_column_id equals sc.column_id
                          join ec in t.Columns() on p.end_column_id equals ec.column_id
#pragma warning disable CS0472
                          select p.object_id == null ? null : new DiffPeriod
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
                                    join cc in Database.View<SysComputedColumn>().DefaultIfEmpty() on new { c.column_id, c.object_id} equals new { cc.column_id, cc.object_id }
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
                                        ComputedColumn = cc.name == null ? null : new DiffComputedColumn 
                                        {
                                            Definition = cc.definition,
                                            Persisted = cc.is_persisted,
                                        },
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

                         CheckConstraints = (from cc in t.CheckConstraints()
                                             select new DiffCheckConstraint
                                             {
                                                 Name = cc.name,
                                                 Definition = cc.definition,
                                                 ColumnName = cc.parent_column_id == 0 ? null : t.Columns().SingleOrDefaultEx(c => c.column_id == cc.parent_column_id)!.name,
                                             }).ToList(),

                         SimpleIndices = (from i in t.Indices()
                                          //where /*!i.is_primary_key && */i.type != 0  /*heap indexes*/
                                          select new DiffIndex
                                          {
                                              IsUnique = i.is_unique,
                                              IsPrimary = i.is_primary_key,
                                              IndexName = i.name ?? "HEAP",
                                              DataSpaceName = i.DataSpaceName(),
                                              FilterDefinition = i.filter_definition,
                                              Type = (DiffIndexType)i.type,
                                              Columns = (from ic in i.IndexColumns()
                                                         join c in t.Columns() on ic.column_id equals c.column_id
                                                         orderby ic.index_column_id
                                                         select new DiffIndexColumn
                                                         {
                                                             ColumnName = c.name,
															 IsDescending = ic.is_descending_key,
                                                             Type = ic.partition_ordinal > 0 ? DiffIndexColumnType.Partition :
                                                             ic.is_included_column ? DiffIndexColumnType.Included :
                                                             DiffIndexColumnType.Key
                                                         }).ToList()
                                          }).ToList(),

                         ViewIndices = (from v in Database.View<SysViews>()
                                        where v.name.StartsWith("VIX_" + t.name + "_")
                                        from i in v.Indices()
                                        select new DiffIndex
                                        {
                                            IsUnique = i.is_unique,
                                            ViewName = v.name,
                                            IndexName = i.name,
                                            DataSpaceName = i.DataSpaceName(),
                                            Columns = (from ic in i.IndexColumns()
                                                       join c in v.Columns() on ic.column_id equals c.column_id
                                                       orderby ic.index_column_id
                                                       select new DiffIndexColumn
                                                       {
                                                           ColumnName = c.name,
                                                           IsDescending = ic.is_descending_key,
                                                           Type = ic.partition_ordinal > 0 ? DiffIndexColumnType.Partition :
                                                           ic.is_included_column ? DiffIndexColumnType.Included :
                                                           DiffIndexColumnType.Key
                                                       }).ToList()

                                        }).ToList(),

                         FullTextIndex = fti == null ? null :
                                   new DiffIndex
                                   {
                                       Columns = (from ic in fti.IndexColumns()
                                                  join c in t.Columns() on ic.column_id equals c.column_id
                                                  select new DiffIndexColumn { ColumnName = c.name }).ToList(),
                                       IndexName = FullTextTableIndex.SqlServerOptions.FULL_TEXT,
                                       IsUnique = false,
                                       IsPrimary = false,
                                       Type = DiffIndexType.FullTextIndex,
                                       FullTextIndex = new FullTextIndex
                                       {
                                           CatallogName = fti.Catallog().name,
                                           StopList = fti.Stoplist().name,
                                           PropertiesList = fti.Properties().property_name,
                                       }
                                   },


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


    public static Dictionary<SchemaName, DiffSchema> GetSchemaNames(List<DatabaseName?> list)
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        var isPostgres = false;
        var result = new Dictionary<SchemaName, DiffSchema>();
        foreach (var db in list)
        {
            using (Administrator.OverrideDatabaseInSysViews(db))
            {
                result.AddRange(Database.View<SysSchemas>().Select(sn => new DiffSchema { Name = new SchemaName(db, sn.name, isPostgres) })
                    .ToList().Where(a => !sqlBuilder.SystemSchemasSqlServer.Contains(a.Name.Name) && !SchemaSynchronizer.IgnoreSchema(a.Name))
                    .ToDictionary(a => a.Name));
            }
        }
        return result;
    }

    public static List<DiffPartitionFunction> GetPartitionFunctions(List<DatabaseName?> list)
    {
        var result = new List<DiffPartitionFunction>();
        foreach (var db in list)
        {
            using (Administrator.OverrideDatabaseInSysViews(db))
            {
                var functions = Database.View<SysPartitionFunction>()
                    .Where(a => !a.name.StartsWith("ifts_comp_fragmen")) //https://dba.stackexchange.com/questions/122021/restoring-sql-server-2012-enterprise-to-web-edition-not-possible-due-to-full-tex
                    .Select(f => new DiffPartitionFunction
                    {
                        DatabaseName = db,
                        FunctionName = f.name,
                        Fanout = f.fanout,
                        Values = Database.View<SysPartitionRangeValues>().Where(a => a.function_id == f.function_id).Select(a => a.value).ToArray()

                    });

                result.AddRange(functions);
            }
        }
        return result;
    }

    public static List<DiffPartitionScheme> GetPartitionSchemes(List<DatabaseName?> list)
    {
        var result = new List<DiffPartitionScheme>();
        foreach (var db in list)
        {
            using (Administrator.OverrideDatabaseInSysViews(db))
            {
                var functions = Database.View<SysPartitionSchemes>()
                    .Where(a => !a.name.StartsWith("ifts_comp_fragmen"))
                    .Select(s => new DiffPartitionScheme
                {
                    DatabaseName = db,
                    SchemeName = s.name,
                    FunctionName = Database.View<SysPartitionFunction>().SingleEx(f => f.function_id ==  s.function_id).name,
                    FileGroups = Database.View<SysDestinationDataSpaces>()
                    .Where(a => a.partition_scheme_id == s.data_space_id)
                    .Select(a => Database.View<SysFileGroups>().SingleEx(fg => fg.data_space_id == a.data_space_id).name)
                    .ToArray()

                });

                result.AddRange(functions);
            }
        }
        return result;
    }

    internal static List<FullTextCatallogName> GetFullTextSearchCatallogs(List<DatabaseName?> list)
    {
        var result = new List<FullTextCatallogName>();
        foreach (var db in list)
        {
            using (Administrator.OverrideDatabaseInSysViews(db))
            {
                var catallogNames = Database.View<SysFullTextCatallog>().Select(s => new FullTextCatallogName(s.name, db)).ToHashSet();

                result.AddRange(catallogNames);
            }
        }
        return result;
    }
}
