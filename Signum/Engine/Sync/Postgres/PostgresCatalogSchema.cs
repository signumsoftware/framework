using Signum.Engine.Maps;
using NpgsqlTypes;
using static Signum.Engine.Sync.Postgres.PostgresFunctions;

namespace Signum.Engine.Sync.Postgres;

public static class PostgresCatalogSchema
{

    public static Dictionary<string, DiffTable> GetDatabaseDescription(List<DatabaseName?> databases)
    {
        List<DiffTable> allTables = new List<DiffTable>();

        var isPostgres = Schema.Current.Settings.IsPostgres;

        foreach (var db in databases)
        {
            SafeConsole.WriteColor(ConsoleColor.Cyan, '.');

            using (Administrator.OverrideDatabaseInSysViews(db))
            {
                var databaseName = db == null ? Connector.Current.DatabaseName() : db.Name;

                //var sysDb = Database.View<SysDatabases>().Single(a => a.name == databaseName);

                var con = Connector.Current;

                var tables =
                    (from ns in Database.View<PgNamespace>()
                     where !ns.IsInternal()
                     from t in ns.Tables()
                     select new DiffTable
                     {
                         Name = new ObjectName(new SchemaName(db, ns.nspname, isPostgres), t.relname, isPostgres),

                         VersionningTrigger = t.Triggers()
                             .Where(t => t.Proc()!.proname == "versioning")
                             .Select(t => t.tgname == null ? null : new DiffPostgresVersioningTrigger
                             {
                                 proname = t.Proc()!.proname,
                                 tgrelid = t.tgrelid,
                                 tgname = t.tgname,
                                 tgfoid = t.tgfoid,
                                 tgargs = t.tgargs
                             })
                             .SingleOrDefault(),

                      

                         PrimaryKeyName = (from c in t.Constraints()
                                           where c.contype == ConstraintType.PrimaryKey
                                           select c.conname == null ? null : new ObjectName(new SchemaName(db, c.Namespace()!.nspname, isPostgres), c.conname, isPostgres))
                                           .SingleOrDefaultEx(),

                         Columns = (from c in t.Attributes()
                                    let def = c.Default()
                                    select new DiffColumn
                                    {
                                        Name = c.attname,
                                        DbType = new AbstractDbType(TypeMappings.GetOrThrow(c.Type()!.typname)),
                                        UserTypeName = null,
                                        Nullable = !c.attnotnull,
                                        Collation = null,
                                        Length = _pg_char_max_length(c.atttypid, c.atttypmod) ?? -1,
                                        Precision = c.atttypid == 1700  /*numeric*/ ? c.atttypmod - 4 >> 16 & 65535 : 0,
                                        Scale = c.atttypid == 1700  /*numeric*/ ? c.atttypmod - 4 & 65535 : 0,
                                        Identity = c.attidentity == 'a',
                                        GeneratedAlwaysType = GeneratedAlwaysType.None,
                                        DefaultConstraint = def == null ? null : new DiffDefaultConstraint
                                        {
                                            Definition = pg_get_expr(def.adbin, def.adrelid),
                                        },
                                        PrimaryKey = t.Indices().Any(i => i.indisprimary && i.indkey.Contains(c.attnum)),
                                    }).ToDictionaryEx(a => a.Name, "columns"),

                         MultiForeignKeys = (from fk in t.Constraints()
                                             where fk.contype == ConstraintType.ForeignKey
                                             select new DiffForeignKey
                                             {
                                                 Name = new ObjectName(new SchemaName(db, fk.Namespace()!.nspname, isPostgres), fk.conname, isPostgres),
                                                 IsDisabled = false,
                                                 TargetTable = new ObjectName(new SchemaName(db, fk.TargetTable().Namespace()!.nspname, isPostgres), fk.TargetTable().relname, isPostgres),
                                                 Columns = generate_subscripts(fk.conkey, 1).Select(i => new DiffForeignKeyColumn
                                                 {
                                                     Parent = t.Attributes().Single(c => c.attnum == fk.conkey[i]).attname,
                                                     Referenced = fk.TargetTable().Attributes().Single(c => c.attnum == fk.confkey[i]).attname,
                                                 }).ToList(),
                                             }).ToList(),

                         SimpleIndices = (from ix in t.Indices()
                                          select new DiffIndex
                                          {
                                              IsUnique = ix.indisunique,
                                              IsPrimary = ix.indisprimary,
                                              IndexName = ix.Class().relname,
                                              FilterDefinition = pg_get_expr(ix.indpred!, ix.indrelid),
                                              Type = DiffIndexType.NonClustered,
                                              Columns = (from i in generate_subscripts(ix.indkey, 1)
                                                         let at = t.Attributes().Single(a => a.attnum == ix.indkey[i])
                                                         orderby i
                                                         select new DiffIndexColumn { ColumnName = at.attname, Type = i >= ix.indnkeyatts ? DiffIndexColumnType.Included : DiffIndexColumnType.Key }).ToList()
                                          }).ToList(),

                         ViewIndices = new List<DiffIndex>(),

                         Stats = new List<DiffStats>(),

                     }).ToList();


                if (SchemaSynchronizer.IgnoreTable != null)
                    tables.RemoveAll(SchemaSynchronizer.IgnoreTable);


                tables.ForEach(t =>
                {
                    t.TemporalType = t.VersionningTrigger == null ? SysTableTemporalType.None : SysTableTemporalType.SystemVersionTemporalTable;
                    t.TemporalTableName = t.VersionningTrigger == null ? null : ParseVersionFunctionParam(t.VersionningTrigger.tgargs);
                });

                tables.ForEach(t => t.Columns.RemoveAll(c => c.Value.DbType.PostgreSql == (NpgsqlDbType)(-1)));

                tables.ForEach(t => t.ForeignKeysToColumns());

                allTables.AddRange(tables);
            }
        }

        var database = allTables.ToDictionary(t => t.Name.ToString());

        var historyTables = database.Values.Select(a => a.TemporalTableName?.ToString()).NotNull().ToHashSet();

        Replacements rep = new Replacements();
        var replacementKey = "Postgres broken versioning triggers";
        rep.AskForReplacements(historyTables, database.Keys.ToHashSet(), replacementKey);

        var withReplacements = rep.ApplyReplacementsToNew(database, replacementKey);
        historyTables.ToList().ForEach(h =>
        {
            var t = withReplacements.TryGetC(h);
             
            if (t != null)
                t.TemporalType = SysTableTemporalType.HistoryTable;
        });

        return database;
    }

    public static ObjectName? ParseVersionFunctionParam(byte[]? tgargs)
    {
        if (tgargs == null)
            return null;

        var str = Encoding.UTF8.GetString(tgargs!);

        var args = str.Split("\0");

        return ObjectName.Parse(args[1], isPostgres: true);
    }

    public static readonly Dictionary<string, NpgsqlDbType> TypeMappings = new Dictionary<string, NpgsqlDbType>
    {
        { "bool", NpgsqlDbType.Boolean },
        { "bytea", NpgsqlDbType.Bytea },
        { "char", NpgsqlDbType.Char },
        { "int8", NpgsqlDbType.Bigint },
        { "int2", NpgsqlDbType.Smallint },
        { "float4", NpgsqlDbType.Real },
        { "float8", NpgsqlDbType.Double },
        { "int2vector", NpgsqlDbType.Smallint | NpgsqlDbType.Array },
        { "int4", NpgsqlDbType.Integer },
        { "text", NpgsqlDbType.Text },
        { "json", NpgsqlDbType.Json },
        { "xml", NpgsqlDbType.Xml },
        { "point", NpgsqlDbType.Point },
        { "lseg", NpgsqlDbType.LSeg },
        { "path", NpgsqlDbType.Path },
        { "box", NpgsqlDbType.Box },
        { "polygon", NpgsqlDbType.Polygon },
        { "line", NpgsqlDbType.Line },
        { "circle", NpgsqlDbType.Circle },
        { "money", NpgsqlDbType.Money },
        { "macaddr", NpgsqlDbType.MacAddr },
        { "macaddr8", NpgsqlDbType.MacAddr8 },
        { "inet", NpgsqlDbType.Inet },
        { "varchar", NpgsqlDbType.Varchar },
        { "date", NpgsqlDbType.Date },
        { "time", NpgsqlDbType.Time },
        { "timestamp", NpgsqlDbType.Timestamp },
        { "timestamptz", NpgsqlDbType.TimestampTz },
        { "interval", NpgsqlDbType.Interval },
        { "timetz", NpgsqlDbType.TimestampTz },
        { "bit", NpgsqlDbType.Bit },
        { "varbit", NpgsqlDbType.Varbit },
        { "numeric", NpgsqlDbType.Numeric },
        { "uuid", NpgsqlDbType.Uuid },
        { "tsvector", NpgsqlDbType.TsVector },
        { "tsquery", NpgsqlDbType.TsQuery },
        { "jsonb", NpgsqlDbType.Jsonb },
        { "int4range", NpgsqlDbType.Range | NpgsqlDbType.Integer },
        { "numrange", NpgsqlDbType.Range | NpgsqlDbType.Numeric },
        { "tsrange", NpgsqlDbType.Range | NpgsqlDbType.Timestamp },
        { "tstzrange", NpgsqlDbType.Range | NpgsqlDbType.TimestampTz },
        { "daterange", NpgsqlDbType.Range | NpgsqlDbType.Date },
        { "int8range", NpgsqlDbType.Range | NpgsqlDbType.Bigint },
        { "ltree", NpgsqlDbType.LTree },
        { "name", NpgsqlDbType.Name },
        { "oidvector", NpgsqlDbType.Oidvector },
        { "pg_lsn", NpgsqlDbType.PgLsn },
        { "oid", (NpgsqlDbType)(-1) },
        { "cid", (NpgsqlDbType)(-1) },
        { "xid", (NpgsqlDbType)(-1) },
        { "tid", (NpgsqlDbType)(-1) }
    };

    public static HashSet<SchemaName> GetSchemaNames(List<DatabaseName?> list)
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        var isPostgres = sqlBuilder.IsPostgres;
        HashSet<SchemaName> result = new HashSet<SchemaName>();
        foreach (var db in list)
        {
            using (Administrator.OverrideDatabaseInSysViews(db))
            {
                var schemaNames = Database.View<PgNamespace>().Where(a => !a.IsInternal()).Select(s => s.nspname).ToList();

                result.AddRange(schemaNames.Select(sn => new SchemaName(db, sn, isPostgres)).Where(a => !SchemaSynchronizer.IgnoreSchema(a)));
            }
        }
        return result;
    }

    internal static List<FullTextCatallogName> GetFullTextSearchCatallogs(List<DatabaseName?> list)
    {
        return new List<FullTextCatallogName>();
    }


}


