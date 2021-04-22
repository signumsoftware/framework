using Signum.Engine.Maps;
using Signum.Engine.PostgresCatalog;
using Signum.Utilities;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using NpgsqlTypes;
using static Signum.Engine.PostgresCatalog.PostgresFunctions;

namespace Signum.Engine.Engine
{
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

                             TemporalType = t.Triggers().Any(t => t.Proc()!.proname == "versioning") ? Signum.Engine.SysTableTemporalType.SystemVersionTemporalTable : SysTableTemporalType.None,

                             //Period = !con.SupportsTemporalTables ? null :

                             //(from p in t.Periods()
                             // join sc in t.Columns() on p.start_column_id equals sc.column_id
                             // join ec in t.Columns() on p.end_column_id equals ec.column_id
                             // select new DiffPeriod
                             // {
                             //     StartColumnName = sc.name,
                             //     EndColumnName = ec.name,
                             // }).SingleOrDefaultEx(),

                             TemporalTableName = t.Triggers()
                                 .Where(t => t.Proc()!.proname == "versioning")
                                 .Select(t => ParseVersionFunctionParam(t.tgargs))
                                 .SingleOrDefaultEx(),

                             //TemporalTableName = !con.SupportsTemporalTables || t.history_table_id == null ? null :
                             //    Database.View<SysTables>()
                             //    .Where(ht => ht.object_id == t.history_table_id)
                             //    .Select(ht => new ObjectName(new SchemaName(db, ht.Schema().name, isPostgres), ht.name, isPostgres))
                             //    .SingleOrDefault(),

                             PrimaryKeyName = (from c in t.Constraints()
                                               where c.contype == ConstraintType.PrimaryKey
                                               select c.conname == null ? null : new ObjectName(new SchemaName(db, c.Namespace()!.nspname, isPostgres), c.conname, isPostgres))
                                               .SingleOrDefaultEx(),

                             Columns = (from c in t.Attributes()
                                        let def = c.Default()
                                        select new DiffColumn
                                        {
                                            Name = c.attname,
                                            DbType = new AbstractDbType(ToNpgsqlDbType(c.Type()!.typname)),
                                            UserTypeName = null,
                                            Nullable = !c.attnotnull,
                                            Collation = null,
                                            Length = PostgresFunctions._pg_char_max_length(c.atttypid, c.atttypmod) ?? -1,
                                            Precision = c.atttypid == 1700  /*numeric*/ ? ((c.atttypmod - 4) >> 16) & 65535 : 0,
                                            Scale = c.atttypid == 1700  /*numeric*/ ? (c.atttypmod - 4) & 65535 : 0,
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
                                                     Columns = PostgresFunctions.generate_subscripts(fk.conkey, 1).Select(i => new DiffForeignKeyColumn
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
                                                  FilterDefinition = PostgresFunctions.pg_get_expr(ix.indpred!, ix.indrelid),
                                                  Type = DiffIndexType.NonClustered,
                                                  Columns = (from  i in PostgresFunctions.generate_subscripts(ix.indkey, 1)
                                                             let at = t.Attributes().Single(a => a.attnum == ix.indkey[i])
                                                             orderby i
                                                             select new DiffIndexColumn { ColumnName = at.attname, IsIncluded = i >= ix.indnkeyatts }).ToList()
                                              }).ToList(),

                             ViewIndices = new List<DiffIndex>(),

                             Stats = new List<DiffStats>(),

                         }).ToList();


                    if (SchemaSynchronizer.IgnoreTable != null)
                        tables.RemoveAll(SchemaSynchronizer.IgnoreTable);

                    tables.ForEach(t => t.Columns.RemoveAll(c => c.Value.DbType.PostgreSql == (NpgsqlDbType)(-1)));

                    tables.ForEach(t => t.ForeignKeysToColumns());

                    allTables.AddRange(tables);
                }
            }

            var database = allTables.ToDictionary(t => t.Name.ToString());

            var historyTables = database.Values.Select(a => a.TemporalTableName).NotNull().ToList();

            historyTables.ForEach(h =>
            {
                var t = database.TryGetC(h.ToString());
                if (t != null)
                    t.TemporalType = SysTableTemporalType.HistoryTable;
            });

            return database;
        }

        private static ObjectName? ParseVersionFunctionParam(byte[]? tgargs)
        {
            if (tgargs == null)
                return null;

            var str = Encoding.UTF8.GetString(tgargs!);

            var args = str.Split("\0");

            return ObjectName.Parse(args[1], isPostgres: true);
        }

        public static NpgsqlDbType ToNpgsqlDbType(string str)
        {
            switch (str)
            {
                case "bool": return NpgsqlDbType.Boolean;
                case "bytea": return NpgsqlDbType.Bytea;
                case "char": return NpgsqlDbType.Char;
                case "int8": return NpgsqlDbType.Bigint;
                case "int2": return NpgsqlDbType.Smallint;
                case "float4": return NpgsqlDbType.Real;
                case "float8": return NpgsqlDbType.Double;
                case "int2vector": return NpgsqlDbType.Smallint | NpgsqlDbType.Array;
                case "int4": return NpgsqlDbType.Integer;
                case "text": return NpgsqlDbType.Text;
                case "json": return NpgsqlDbType.Json;
                case "xml": return NpgsqlDbType.Xml;
                case "point": return NpgsqlDbType.Point;
                case "lseg": return NpgsqlDbType.LSeg;
                case "path": return NpgsqlDbType.Path;
                case "box": return NpgsqlDbType.Box;
                case "polygon": return NpgsqlDbType.Polygon;
                case "line": return NpgsqlDbType.Line;
                case "circle": return NpgsqlDbType.Circle;
                case "money": return NpgsqlDbType.Money;
                case "macaddr": return NpgsqlDbType.MacAddr;
                case "macaddr8": return NpgsqlDbType.MacAddr8;
                case "inet": return NpgsqlDbType.Inet;
                case "varchar": return NpgsqlDbType.Varchar;
                case "date": return NpgsqlDbType.Date;
                case "time": return NpgsqlDbType.Time;
                case "timestamp": return NpgsqlDbType.Timestamp;
                case "timestamptz": return NpgsqlDbType.TimestampTz;
                case "interval": return NpgsqlDbType.Interval;
                case "timetz": return NpgsqlDbType.TimestampTz;
                case "bit": return NpgsqlDbType.Bit;
                case "varbit": return NpgsqlDbType.Varbit;
                case "numeric": return NpgsqlDbType.Numeric;
                case "uuid": return NpgsqlDbType.Uuid;
                case "tsvector": return NpgsqlDbType.TsVector;
                case "tsquery": return NpgsqlDbType.TsQuery;
                case "jsonb": return NpgsqlDbType.Jsonb;
                case "int4range": return NpgsqlDbType.Range | NpgsqlDbType.Integer;
                case "numrange": return NpgsqlDbType.Range | NpgsqlDbType.Numeric;
                case "tsrange": return NpgsqlDbType.Range | NpgsqlDbType.Timestamp;
                case "tstzrange": return NpgsqlDbType.Range | NpgsqlDbType.TimestampTz;
                case "daterange": return NpgsqlDbType.Range | NpgsqlDbType.Date;
                case "int8range": return NpgsqlDbType.Range | NpgsqlDbType.Bigint;
                case "oid":
                case "cid":
                case "xid":
                case "tid":
                    return (NpgsqlDbType)(-1);
                default: 
                    return (NpgsqlDbType)(-1);
            }

        }

        public static HashSet<SchemaName> GetSchemaNames(List<DatabaseName?> list)
        {
            var sqlBuilder = Connector.Current.SqlBuilder;
            var isPostgres = sqlBuilder.IsPostgres;
            HashSet<SchemaName> result = new HashSet<SchemaName>();
            foreach (var db in list)
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    var schemaNames = Database.View<PgNamespace>().Where(a=>!a.IsInternal()).Select(s => s.nspname).ToList();

                    result.AddRange(schemaNames.Select(sn => new SchemaName(db, sn, isPostgres)).Where(a => !SchemaSynchronizer.IgnoreSchema(a)));
                }
            }
            return result;
        }
    }
}
