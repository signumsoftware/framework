using Signum.Utilities.Reflection;
using System.Data;
using Signum.Utilities.DataStructures;
using System.Data.Common;
using System.Collections.Concurrent;
using Signum.Engine.Basics;

namespace Signum.Engine.Maps;

public struct Forbidden
{
    public Forbidden(HashSet<Entity>? set)
    {
        this.set = set;
    }

    public Forbidden(DirectedGraph<Entity>? graph, Entity entity)
    {
        this.set = graph?.TryRelatedTo(entity);
    }

    readonly HashSet<Entity>? set;

    public bool IsEmpty
    {
        get { return set == null || set.Count == 0; }
    }

    public bool Contains(Entity entity)
    {
        return set != null && set.Contains(entity);
    }
}

public struct EntityForbidden
{
    public readonly Entity Entity;
    public readonly Forbidden Forbidden;

    public EntityForbidden(Entity entity, Forbidden forbidden)
    {
        this.Entity = (Entity)entity;
        this.Forbidden = forbidden;
    }

    public EntityForbidden(Entity entity, DirectedGraph<Entity>? graph)
    {
        this.Entity = (Entity)entity;
        this.Forbidden = new Forbidden(graph, entity);
    }
}

public partial class Table
{
    internal static string Var(bool isPostgres, string varName)
    {
        if (isPostgres)
            return varName;
        else
            return "@" + varName;
    }

    internal static string DeclareTempTable(string variableName, FieldPrimaryKey id, bool isPostgres)
    {
        if (isPostgres)
            return $"CREATE TEMP TABLE {variableName} ({id.Name.SqlEscape(isPostgres)} {id.DbType.ToString(isPostgres)});";
        else
            return $"DECLARE {variableName} TABLE({id.Name.SqlEscape(isPostgres)} {id.DbType.ToString(isPostgres)});";
    }

    ResetLazy<InsertCacheIdentity> inserterIdentity;
    ResetLazy<InsertCacheDisableIdentity> inserterDisableIdentity;

    internal void InsertMany(List<Entity> list, DirectedGraph<Entity>? backEdges)
    {
        using (HeavyProfiler.LogNoStackTrace("InsertMany", () => this.Type.TypeName()))
        {
            if (IdentityBehaviour && !Administrator.IsIdentityBehaviourDisabled(this))
            {
                InsertCacheIdentity ic = inserterIdentity.Value;
                list.SplitStatements(this.Columns.Count, ls => ic.GetInserter(ls.Count)(ls, backEdges));
            }
            else
            {
                InsertCacheDisableIdentity ic = inserterDisableIdentity.Value;
                list.SplitStatements(this.Columns.Count, ls => ic.GetInserter(ls.Count)(ls, backEdges));
            }
        }
    }

    internal object?[] BulkInsertDataRow(object/*Entity or IView*/ entity)
    {
        List<DbParameter> parameters = GetInsertParameters(entity);

        return parameters.Select(a => a.Value).ToArray();
    }

    internal List<DbParameter> GetInsertParameters(object entity)
    {
        List<DbParameter> parameters = new List<DbParameter>();
        if (IdentityBehaviour && !Administrator.IsIdentityBehaviourDisabled(this))
            inserterIdentity.Value.InsertParameters((Entity)entity, new Forbidden(), "", parameters);
        else
            inserterDisableIdentity.Value.InsertParameters(entity, new Forbidden(), "", parameters);
        return parameters;
    }

    class InsertCacheDisableIdentity
    {
        internal Table table;

        public Func<string[], string> SqlInsertPattern;
        public Action<object /*Entity*/, Forbidden, string, List<DbParameter>> InsertParameters;

        ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>?>> insertDisableIdentityCache =
            new ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>?>>();

        public InsertCacheDisableIdentity(Table table, Func<string[], string> sqlInsertPattern, Action<object, Forbidden, string, List<DbParameter>> insertParameters)
        {
            this.table = table;
            SqlInsertPattern = sqlInsertPattern;
            InsertParameters = insertParameters;
        }

        internal Action<List<Entity>, DirectedGraph<Entity>?> GetInserter(int numElements)
        {
            return insertDisableIdentityCache.GetOrAdd(numElements, (int num) => num == 1 ? GetInsertDisableIdentity() : GetInsertMultiDisableIdentity(num));
        }

        Action<List<Entity>, DirectedGraph<Entity>?> GetInsertDisableIdentity()
        {
            string sqlSingle = SqlInsertPattern(new[] { "" });

            return (list, graph) =>
            {
                Entity entity = list.Single();

                AssertHasId(entity);

                entity.Ticks = Clock.Now.Ticks;

                table.SetToStrField(entity);

                var forbidden = new Forbidden(graph, entity);

                var parameters = new List<DbParameter>();
                InsertParameters(entity, forbidden, "", parameters);
                new SqlPreCommandSimple(sqlSingle, parameters).ExecuteNonQuery();

                entity.IsNew = false;
                if (table.saveCollections.Value != null)
                    table.saveCollections.Value.InsertCollections(new List<EntityForbidden> { new EntityForbidden(entity, forbidden) });
            };
        }

        Action<List<Entity>, DirectedGraph<Entity>?> GetInsertMultiDisableIdentity(int num)
        {
            string sqlMulti = SqlInsertPattern(Enumerable.Range(0, num).Select(i => i.ToString()).ToArray());

            return (entities, graph) =>
            {
                for (int i = 0; i < num; i++)
                {
                    var entity = entities[i];
                    AssertHasId(entity);

                    entity.Ticks = Clock.Now.Ticks;

                    table.SetToStrField(entity);
                }

                List<DbParameter> result = new List<DbParameter>();
                for (int i = 0; i < entities.Count; i++)
                    InsertParameters(entities[i], new Forbidden(graph, entities[i]), i.ToString(), result);

                new SqlPreCommandSimple(sqlMulti, result).ExecuteNonQuery();
                for (int i = 0; i < num; i++)
                {
                    Entity ident = entities[i];

                    ident.IsNew = false;
                }

                if (table.saveCollections.Value != null)
                    table.saveCollections.Value.InsertCollections(entities.Select(e => new EntityForbidden(e, graph)).ToList());
            };
        }

        internal static InsertCacheDisableIdentity InitializeInsertDisableIdentity(Table table)
        {
            using (HeavyProfiler.LogNoStackTrace("InitializeInsertDisableIdentity", () => table.Type.TypeName()))
            {
                var trios = new List<Table.Trio>();
                var assigments = new List<Expression>();
                var paramIdent = Expression.Parameter(typeof(object) /*Entity*/, "ident");
                var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                var paramSuffix = Expression.Parameter(typeof(string), "suffix");
                var paramList = Expression.Parameter(typeof(List<DbParameter>), "dbParams");

                var cast = Expression.Parameter(table.Type, "casted");
                assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, table.Type)));

                foreach (var item in table.Fields.Values)
                    item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramSuffix);

                if (table.Mixins != null)
                    foreach (var item in table.Mixins.Values)
                        item.CreateParameter(trios, assigments, cast, paramForbidden, paramSuffix);

                var isPostgres = Schema.Current.Settings.IsPostgres;

                string insertPattern(string[] suffixes) =>
                        "INSERT INTO {0}\r\n  ({1})\r\n{2}VALUES\r\n{3};".FormatWith(table.Name,
                        trios.ToString(p => p.SourceColumn.SqlEscape(isPostgres), ", "),
                        isPostgres ? "OVERRIDING SYSTEM VALUE\r\n" : null,
                        suffixes.ToString(s => "  (" + trios.ToString(p => p.ParameterName + s, ", ") + ")", ",\r\n"));

                var expr = Expression.Lambda<Action<object, Forbidden, string, List<DbParameter>>>(
                    CreateBlock(trios.Select(a => a.ParameterBuilder), assigments, paramList), paramIdent, paramForbidden, paramSuffix, paramList);


                return new InsertCacheDisableIdentity(table, insertPattern, expr.Compile());
            }
        }
    }

    class InsertCacheIdentity
    {
        internal Table table;

        public Func<string[], string?, string> SqlInsertPattern;
        public Action<Entity, Forbidden, string, List<DbParameter>> InsertParameters;

        ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>?>> insertIdentityCache =
           new ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>?>>();

        public InsertCacheIdentity(Table table, Func<string[], string?, string> sqlInsertPattern, Action<Entity, Forbidden, string, List<DbParameter>> insertParameters)
        {
            this.table = table;
            SqlInsertPattern = sqlInsertPattern;
            InsertParameters = insertParameters;
        }

        internal Action<List<Entity>, DirectedGraph<Entity>?> GetInserter(int numElements)
        {
            return insertIdentityCache.GetOrAdd(numElements, (int num) => GetInsertMultiIdentity(num));
        }

        Action<List<Entity>, DirectedGraph<Entity>?> GetInsertMultiIdentity(int num)
        {
            var isPostgres = Schema.Current.Settings.IsPostgres;
            string sqlMulti = SqlInsertPattern(Enumerable.Range(0, num).Select(i => i.ToString()).ToArray(), "" /*output but no name*/);

            return (entities, graph) =>
            {
                for (int i = 0; i < num; i++)
                {
                    var entity = entities[i];
                    AssertNoId(entity);

                    entity.Ticks = Clock.Now.Ticks;

                    table.SetToStrField(entity);
                }

                List<DbParameter> result = new List<DbParameter>();
                for (int i = 0; i < entities.Count; i++)
                    InsertParameters(entities[i], new Forbidden(graph, entities[i]), i.ToString(), result);

                DataTable dt = new SqlPreCommandSimple(sqlMulti, result).ExecuteDataTable();

                for (int i = 0; i < num; i++)
                {
                    Entity ident = entities[i];

                    ident.id = new PrimaryKey((IComparable)dt.Rows[i][0]);
                    ident.IsNew = false;
                }

                if (table.saveCollections.Value != null)
                    table.saveCollections.Value.InsertCollections(entities.Select(e => new EntityForbidden(e, graph)).ToList());
            };

        }

        internal static InsertCacheIdentity InitializeInsertIdentity(Table table)
        {
            using (HeavyProfiler.LogNoStackTrace("InitializeInsertIdentity", () => table.Type.TypeName()))
            {
                var trios = new List<Table.Trio>();
                var assigments = new List<Expression>();
                var paramIdent = Expression.Parameter(typeof(Entity), "ident");
                var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                var paramSuffix = Expression.Parameter(typeof(string), "suffix");
                var paramList = Expression.Parameter(typeof(List<DbParameter>), "dbParams");

                var cast = Expression.Parameter(table.Type, "casted");
                assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, table.Type)));

                foreach (var item in table.Fields.Values.Where(a => !(a.Field is FieldPrimaryKey)))
                    item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramSuffix);

                if (table.Mixins != null)
                    foreach (var item in table.Mixins.Values)
                        item.CreateParameter(trios, assigments, cast, paramForbidden, paramSuffix);

                var isPostgres = Schema.Current.Settings.IsPostgres;
                string sqlInsertPattern(string[] suffixes, string? output) =>
                    "INSERT INTO {0}\r\n  ({1})\r\n{2}VALUES\r\n{3}{4};".FormatWith(
                    table.Name,
                    trios.ToString(p => p.SourceColumn.SqlEscape(isPostgres), ", "),
                    output != null && !isPostgres ? $"OUTPUT INSERTED.{table.PrimaryKey.Name.SqlEscape(isPostgres)}{(output.Length > 0 ? " INTO " + output : "")}\r\n" : null,
                    suffixes.ToString(s => " (" + trios.ToString(p => p.ParameterName + s, ", ") + ")", ",\r\n"),
                    output != null && isPostgres ? $"\r\nRETURNING {table.PrimaryKey.Name.SqlEscape(isPostgres)}{(output.Length > 0 ? " INTO " + output : "")}" : null);


                var expr = Expression.Lambda<Action<Entity, Forbidden, string, List<DbParameter>>>(
                    CreateBlock(trios.Select(a => a.ParameterBuilder), assigments, paramList), paramIdent, paramForbidden, paramSuffix, paramList);

                return new InsertCacheIdentity(table, sqlInsertPattern, expr.Compile());
            }
        }
    }

    static void AssertHasId(Entity ident)
    {
        if (ident.IdOrNull == null)
            throw new InvalidOperationException("{0} should have an Id, since the table has no Identity".FormatWith(ident, ident.IdOrNull));
    }

    static void AssertNoId(Entity ident)
    {
        if (ident.IdOrNull != null)
            throw new InvalidOperationException("{0} is new, but has Id {1}".FormatWith(ident, ident.IdOrNull));
    }


    public IColumn? ToStrColumn
    {
        get
        {

            if (Fields.TryGetValue("toStr", out var entity))
                return (IColumn)entity.Field;

            return null;
        }
    }

    internal bool SetToStrField(Entity entity)
    {
        var toStrColumn = ToStrColumn;
        if (toStrColumn != null)
        {
            string newStr;
            using (CultureInfoUtils.ChangeCultureUI(Schema.Current.ForceCultureInfo))
                newStr = entity.ToString();

            if (newStr.HasText() && toStrColumn.Size.HasValue && newStr.Length > toStrColumn.Size)
                newStr = newStr.Substring(0, toStrColumn.Size.Value);

            if (entity.toStr != newStr)
            {
                entity.toStr = newStr;
                return true;
            }
        }

        return false;
    }


    internal static FieldInfo fiId = ReflectionTools.GetFieldInfo((Entity i) => i.id);

    internal void UpdateMany(List<Entity> list, DirectedGraph<Entity>? backEdges)
    {
        using (HeavyProfiler.LogNoStackTrace("UpdateMany", () => this.Type.TypeName()))
        {
            var uc = updater.Value;
            list.SplitStatements(this.Columns.Count + 2, ls => uc.GetUpdater(ls.Count)(ls, backEdges));
        }
    }

    class UpdateCache
    {
        internal Table table;

        public Func<string, bool, string> SqlUpdatePattern;
        public Action<Entity, long, Forbidden, string, List<DbParameter>> UpdateParameters;

        ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>?>> updateCache =
            new ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>?>>();

        public UpdateCache(Table table, Func<string, bool, string> sqlUpdatePattern, Action<Entity, long, Forbidden, string, List<DbParameter>> updateParameters)
        {
            this.table = table;
            SqlUpdatePattern = sqlUpdatePattern;
            UpdateParameters = updateParameters;
        }

        public Action<List<Entity>, DirectedGraph<Entity>?> GetUpdater(int numElements)
        {
            return updateCache.GetOrAdd(numElements, num => num == 1 ? GenerateUpdate() : GetUpdateMultiple(num));
        }

        Action<List<Entity>, DirectedGraph<Entity>?> GenerateUpdate()
        {
            string sqlUpdate = SqlUpdatePattern("", false);

            if (table.Ticks != null)
            {
                return (uniList, graph) =>
                {
                    Entity ident = uniList.Single();
                    Entity entity = (Entity)ident;

                    long oldTicks = entity.Ticks;
                    entity.Ticks = Clock.Now.Ticks;

                    table.SetToStrField(ident);

                    var forbidden = new Forbidden(graph, ident);

                    int num = (int)new SqlPreCommandSimple(sqlUpdate, new List<DbParameter>().Do(ps => UpdateParameters(ident, oldTicks, forbidden, "", ps))).ExecuteNonQuery();
                    if (num != 1)
                        throw new ConcurrencyException(ident.GetType(), ident.Id);

                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.UpdateCollections(new List<EntityForbidden> { new EntityForbidden(ident, forbidden) });
                };
            }
            else
            {
                return (uniList, graph) =>
                {
                    Entity ident = uniList.Single();

                    table.SetToStrField(ident);

                    var forbidden = new Forbidden(graph, ident);

                    int num = (int)new SqlPreCommandSimple(sqlUpdate, new List<DbParameter>().Do(ps => UpdateParameters(ident, -1, forbidden, "", ps))).ExecuteNonQuery();
                    if (num != 1)
                        throw new EntityNotFoundException(ident.GetType(), ident.Id);
                };
            }
        }

        Action<List<Entity>, DirectedGraph<Entity>?> GetUpdateMultiple(int num)
        {
            var isPostgres = Schema.Current.Settings.IsPostgres;
            var updated = Table.Var(isPostgres, "updated");
            var id = this.table.PrimaryKey.Name.SqlEscape(isPostgres);
            string sqlMulti = num == 1 ? SqlUpdatePattern("", true) :
                new StringBuilder()
                  .AppendLine(Table.DeclareTempTable(updated, this.table.PrimaryKey, isPostgres))
                  .AppendLine()
                  .AppendLines(Enumerable.Range(0, num).Select(i => SqlUpdatePattern(i.ToString(), true) + "\r\n"))
                  .AppendLine()
                  .AppendLine($"SELECT {id} from {updated}")
                  .ToString();

            if (table.Ticks != null)
            {
                return (idents, graph) =>
                {
                    List<DbParameter> parameters = new List<DbParameter>();
                    for (int i = 0; i < num; i++)
                    {
                        Entity entity = (Entity)idents[i];

                        long oldTicks = entity.Ticks;
                        entity.Ticks = Clock.Now.Ticks;

                        UpdateParameters(entity, oldTicks, new Forbidden(graph, entity), i.ToString(), parameters);
                    }

                    DataTable dt = new SqlPreCommandSimple(sqlMulti, parameters).ExecuteDataTable();

                    if (dt.Rows.Count != idents.Count)
                    {
                        var updated = dt.Rows.Cast<DataRow>().Select(r => new PrimaryKey((IComparable)r[0])).ToList();

                        var missing = idents.Select(a => a.Id).Except(updated).ToArray();

                        throw new ConcurrencyException(table.Type, missing);
                    }

                    if (isPostgres && num > 1)
                    {
                        new SqlPreCommandSimple($"DROP TABLE {updated}").ExecuteNonQuery();
                    }

                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.UpdateCollections(idents.Select(e => new EntityForbidden(e, new Forbidden(graph, e))).ToList());
                };
            }
            else
            {
                return (idents, graph) =>
                {
                    List<DbParameter> parameters = new List<DbParameter>();
                    for (int i = 0; i < num; i++)
                    {
                        var ident = idents[i];
                        UpdateParameters(ident, -1, new Forbidden(graph, ident), i.ToString(), parameters);
                    }

                    DataTable dt = new SqlPreCommandSimple(sqlMulti, parameters).ExecuteDataTable();

                    if (dt.Rows.Count != idents.Count)
                    {
                        var updated = dt.Rows.Cast<DataRow>().Select(r => new PrimaryKey((IComparable)r[0])).ToList();

                        var missing = idents.Select(a => a.Id).Except(updated).ToArray();

                        throw new EntityNotFoundException(table.Type, missing);
                    }

                    for (int i = 0; i < num; i++)
                    {
                        Entity ident = idents[i];
                    }

                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.UpdateCollections(idents.Select(e => new EntityForbidden(e, new Forbidden(graph, e))).ToList());
                };
            }
        }

        internal static UpdateCache InitializeUpdate(Table table)
        {
            using (HeavyProfiler.LogNoStackTrace("InitializeUpdate", () => table.Type.TypeName()))
            {
                var trios = new List<Trio>();
                var assigments = new List<Expression>();
                var paramIdent = Expression.Parameter(typeof(Entity), "ident");
                var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                var paramOldTicks = Expression.Parameter(typeof(long), "oldTicks");
                var paramSuffix = Expression.Parameter(typeof(string), "suffix");
                var paramList = Expression.Parameter(typeof(List<DbParameter>), "paramList");

                var cast = Expression.Parameter(table.Type);
                assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, table.Type)));

                foreach (var item in table.Fields.Values.Where(a => !(a.Field is FieldPrimaryKey)))
                    item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramSuffix);

                if (table.Mixins != null)
                    foreach (var item in table.Mixins.Values)
                        item.CreateParameter(trios, assigments, cast, paramForbidden, paramSuffix);

                var pb = Connector.Current.ParameterBuilder;

                string idParamName = ParameterBuilder.GetParameterName("id");

                string oldTicksParamName = ParameterBuilder.GetParameterName("old_ticks");

                var isPostgres = Schema.Current.Settings.IsPostgres;

                var id = table.PrimaryKey.Name.SqlEscape(isPostgres);
                var updated = Table.Var(isPostgres, "updated");

                Func<string, bool, string> sqlUpdatePattern = (suffix, output) =>
                {
                    var result = $"UPDATE {table.Name} SET\r\n" +
trios.ToString(p => "{0} = {1}".FormatWith(p.SourceColumn.SqlEscape(isPostgres), p.ParameterName + suffix).Indent(2), ",\r\n") + "\r\n" +
(output && !isPostgres ? $"OUTPUT INSERTED.{id} INTO {updated}\r\n" : null) +
$"WHERE {id} = {idParamName + suffix}" + (table.Ticks != null ? $" AND {table.Ticks.Name.SqlEscape(isPostgres)} = {oldTicksParamName + suffix}" : null) + "\r\n" +
(output && isPostgres ? $"RETURNING ({id})" : null);

                    if (!(isPostgres && output))
                        return result.Trim() + ";";

                    return $@"WITH rows AS (
{result.Indent(4)}
)
INSERT INTO {updated}({id})
SELECT {id} FROM rows;";
                };

                List<Expression> parameters = new List<Expression>
                    {
                        pb.ParameterFactory(Trio.Concat(idParamName, paramSuffix), table.PrimaryKey.DbType, null, null, null, null, false,
                        Expression.Field(Expression.Property(Expression.Field(paramIdent, fiId), "Value"), "Object"))
                    };

                if (table.Ticks != null)
                {
                    parameters.Add(pb.ParameterFactory(Trio.Concat(oldTicksParamName, paramSuffix), table.Ticks.DbType, null, null, null, null, false, table.Ticks.ConvertTicks(paramOldTicks)));
                }

                parameters.AddRange(trios.Select(a => (Expression)a.ParameterBuilder));

                var expr = Expression.Lambda<Action<Entity, long, Forbidden, string, List<DbParameter>>>(
                    CreateBlock(parameters, assigments, paramList), paramIdent, paramOldTicks, paramForbidden, paramSuffix, paramList);


                return new UpdateCache(table, sqlUpdatePattern, expr.Compile());
            }
        }

    }

    ResetLazy<UpdateCache> updater;


    class CollectionsCache
    {
        public List<TableMList.IMListCache> Caches;

        public SqlPreCommand InsertCollectionsSync(Entity ident, string suffix, string parameterIdVar)
        {
            return Caches.Select((rc, i) => rc.RelationalInsertSync(ident, suffix + "_" + i.ToString(), parameterIdVar)).Combine(Spacing.Double)!;
        }

        public SqlPreCommand UpdateCollectionsSync(Entity ident, string suffix, bool replaceParameter)
        {
            return Caches.Select((rc, i) => rc.RelationalUpdateSync(ident, suffix + "_" + i.ToString(), replaceParameter)).Combine(Spacing.Double)!;
        }

        public void InsertCollections(List<EntityForbidden> entities)
        {
            foreach (var rc in Caches)
                rc.RelationalInserts(entities);

        }

        public void UpdateCollections(List<EntityForbidden> entities)
        {
            foreach (var rc in Caches)
                rc.RelationalUpdates(entities);
        }

        public CollectionsCache(List<TableMList.IMListCache> caches)
        {
            this.Caches = caches;
        }

        internal static CollectionsCache? InitializeCollections(Table table)
        {
            using (HeavyProfiler.LogNoStackTrace("InitializeCollections", () => table.Type.TypeName()))
            {
                var caches =
                    (from rt in table.TablesMList()
                     select rt.cache.Value).ToList();

                if (caches.IsEmpty())
                    return null;
                else
                {
                    return new CollectionsCache(caches);
                }
            }
        }
    }

    ResetLazy<CollectionsCache?> saveCollections;




    public SqlPreCommand InsertSqlSync(Entity ident, bool includeCollections = true, string? comment = null, string suffix = "", string? forceParentId = null)
    {
        PrepareEntitySync(ident);
        SetToStrField(ident);

        bool isGuid = this.PrimaryKey.DbType.IsGuid();
        var isPostgres = Schema.Current.Settings.IsPostgres;

        var virtualMLists = VirtualMList.RegisteredVirtualMLists.TryGetC(this.Type);

        var identityBehaviour = IdentityBehaviour && !Administrator.IsIdentityBehaviourDisabled(this);

        if (!includeCollections || (saveCollections.Value == null && virtualMLists == null))
        {
            SqlPreCommandSimple simpleInsert = identityBehaviour ?
                 new SqlPreCommandSimple(
                     inserterIdentity.Value.SqlInsertPattern(new[] { suffix }, null),
                     new List<DbParameter>().Do(dbParams => inserterIdentity.Value.InsertParameters(ident, new Forbidden(), suffix, dbParams))).AddComment(comment) :
                 new SqlPreCommandSimple(
                     inserterDisableIdentity.Value.SqlInsertPattern(new[] { suffix }),
                     new List<DbParameter>().Do(dbParams => inserterDisableIdentity.Value.InsertParameters(ident, new Forbidden(), suffix, dbParams))).AddComment(comment);

            return simpleInsert;
        }

        string? parentId = identityBehaviour ? (forceParentId ?? Table.Var(isPostgres, "parentId")) : null;
        string? newIds = identityBehaviour && (!isPostgres && isGuid) ? Table.Var(isPostgres, "newIDs") : null;

        SqlPreCommandSimple insert = identityBehaviour ?
             new SqlPreCommandSimple(
                 inserterIdentity.Value.SqlInsertPattern(new[] { suffix }, newIds ?? (isPostgres ? parentId : null)),
                 new List<DbParameter>().Do(dbParams => inserterIdentity.Value.InsertParameters(ident, new Forbidden(), suffix, dbParams))).AddComment(comment) :
             new SqlPreCommandSimple(
                 inserterDisableIdentity.Value.SqlInsertPattern(new[] { suffix }),
                 new List<DbParameter>().Do(dbParams => inserterDisableIdentity.Value.InsertParameters(ident, new Forbidden(), suffix, dbParams))).AddComment(comment);

        SqlPreCommand? collections = saveCollections.Value?.InsertCollectionsSync(ident, suffix, parentId!);

        SqlPreCommand? virtualMList = virtualMLists?.Values
            .Select(vmi => giInsertVirtualMListSync.GetInvoker(this.Type, vmi.BackReferenceRoute.RootType)(ident, vmi, parentId!))
            .Combine(Spacing.Double);


        if (!identityBehaviour)
        {
            return SqlPreCommand.Combine(Spacing.Simple,
               insert,
               virtualMList,
               collections)!;
        }

        var pkType = this.PrimaryKey.DbType.ToString(isPostgres);

        if (isPostgres)
        {
            return new SqlPreCommandSimple(@$"DO $$
DECLARE {parentId} {pkType};
BEGIN
{insert.PlainSql().Indent(4)}

{collections?.PlainSql().Indent(4)}
{virtualMList?.PlainSql().Indent(4)}
END $$;"); ;
        }
        else if (isGuid)
        {
            return SqlPreCommand.Combine(Spacing.Simple,
                insert,
                new SqlPreCommandSimple($"DECLARE {parentId} {pkType};"),
                new SqlPreCommandSimple($"SELECT {parentId} = ID FROM {newIds};"),
                collections,
                virtualMList)!;
        }
        else
        {
            return SqlPreCommand.Combine(Spacing.Simple,
                new SqlPreCommandSimple($"DECLARE {parentId} {pkType};") { GoBefore = forceParentId == null },
                insert,
                new SqlPreCommandSimple($"SET {parentId} = @@Identity;"),
                collections,
                virtualMList)!;
        }
    }

    public SqlPreCommand? UpdateSqlSync<T>(T entity, Expression<Func<T, bool>>? where, bool includeCollections = true, string? comment = null, string suffix = "")
        where T : Entity
    {
        if (typeof(T) != Type && where != null)
            throw new InvalidOperationException("Invalid table");

        PrepareEntitySync(entity);

        if (SetToStrField(entity))
            entity.SetSelfModified();

        if (entity.Modified == ModifiedState.Clean || entity.Modified == ModifiedState.Sealed)
            return null;

        var uc = updater.Value;
        var sql = uc.SqlUpdatePattern(suffix, false);
        var parameters = new List<DbParameter>().Do(ps => uc.UpdateParameters(entity, (entity as Entity)?.Ticks ?? -1, new Forbidden(), suffix, ps));

        SqlPreCommand? update;
        if (where != null)
        {
            bool isPostgres = Schema.Current.Settings.IsPostgres;

            var declare = DeclarePrimaryKeyVariable(entity, where);
            var updateSql = new SqlPreCommandSimple(sql, parameters).AddComment(comment).ReplaceFirstParameter( entity.Id.VariableName);

            update = isPostgres ?
                PostgresDoBlock(entity.Id.VariableName!, declare, updateSql) :
                SqlPreCommand.Combine(Spacing.Simple, declare, updateSql); ;
        }
        else
        {
            update = new SqlPreCommandSimple(sql, parameters).AddComment(comment);
        }

        if (!includeCollections)
            return update;

        var vmis = VirtualMList.RegisteredVirtualMLists.TryGetC(this.Type);

        SqlPreCommand? virtualMList = vmis?.Values
            .Select(vmi => giUpdateVirtualMListSync.GetInvoker(this.Type, vmi.BackReferenceRoute.RootType)(entity, vmi))
            .Combine(Spacing.Double);

        var cc = saveCollections.Value;
        SqlPreCommand? collections = cc?.UpdateCollectionsSync((Entity)entity, suffix, where != null);

        return SqlPreCommand.Combine(Spacing.Double, update, collections, virtualMList);
    }

    static GenericInvoker<Func<Entity, VirtualMListInfo, SqlPreCommand>> giUpdateVirtualMListSync = new GenericInvoker<Func<Entity, VirtualMListInfo, SqlPreCommand>>(
        (e, vmi) => UpdateVirtualMListSync<Entity, Entity>(e, vmi));

    static SqlPreCommand UpdateVirtualMListSync<T, E>(T entity, VirtualMListInfo vmli)
        where T : Entity
        where E : Entity
    {
        var table = Schema.Current.Table(typeof(E));

        var lambda = vmli.MListRoute.GetLambdaExpression<T, MList<E>>(safeNullAccess: true).Compile();

        var mlist = lambda(entity);

        var backRef = vmli.BackReferenceRoute.GetLambdaExpression<E, Lite<T>>(safeNullAccess: false);

        var delete = Administrator.UnsafeDeletePreCommand(Database.Query<E>().Where(e => backRef.Evaluate(e).Is(entity)));

        var getContainer = vmli.BackReferenceRoute.Parent!.GetLambdaExpression<E, ModifiableEntity>(safeNullAccess: true).Compile();

        var setter = ReflectionTools.CreateSetter<ModifiableEntity, Lite<T>>((MemberInfo?)vmli.BackReferenceRoute.PropertyInfo ?? vmli.BackReferenceRoute.FieldInfo!)!;

        var inserts = mlist.Select(elem =>
        {
            setter(getContainer(elem), entity.ToLite());
            
            var result = table.InsertSqlSync(elem);
            return result;
        }).Combine(Spacing.Simple);

        return SqlPreCommand.Combine(Spacing.Simple, delete, inserts)!;
    }

    static GenericInvoker<Func<Entity, VirtualMListInfo, string, SqlPreCommand>> giInsertVirtualMListSync = new GenericInvoker<Func<Entity, VirtualMListInfo, string, SqlPreCommand>>(
        (e, vmi, parentId) => InsertVirtualMListSync<Entity, Entity>(e, vmi, parentId));

    static SqlPreCommand InsertVirtualMListSync<T, E>(T entity, VirtualMListInfo vmli, string parentId)
        where T : Entity
        where E : Entity
    {
        var table = Schema.Current.Table(typeof(E));

        var lambda = vmli.MListRoute.GetLambdaExpression<T, MList<E>>(safeNullAccess: true).Compile();

        var mlist = lambda(entity);

        var backRef = vmli.BackReferenceRoute.GetLambdaExpression<E, Lite<T>>(safeNullAccess: false);

        bool isPostgres = Schema.Current.Settings.IsPostgres;
        var field = (FieldReference)Schema.Current.Field(vmli.BackReferenceRoute)!;

        var paramName = Signum.Engine.ParameterBuilder.GetParameterName(field.Name);

        var inserts = mlist.Select((elem, i) =>
        {
            var result = table.InsertSqlSync(elem, forceParentId: parentId + "_" + i);

            var simple = result as SqlPreCommandSimple ?? (SqlPreCommandSimple)((SqlPreCommandConcat)result).Commands.FirstEx(r => r is SqlPreCommandSimple s && s.Parameters != null);

            var param = simple.Parameters!.SingleEx(p => p.ParameterName == paramName);

            simple.ReplaceParameter(param, parentId);

            return result;
        }).Combine(Spacing.Simple);

        return SqlPreCommand.Combine(Spacing.Simple, inserts)!;
    }

    void PrepareEntitySync(Entity entity)
    {
        Schema current = Schema.Current;
        DirectedGraph<Modifiable> modifiables = Saver.PreSaving(() => GraphExplorer.FromRoot(entity));

        var error = GraphExplorer.FullIntegrityCheck(modifiables);
        if (error != null)
        {
#if DEBUG
            var withEntites = error.WithEntities(modifiables);
            throw new IntegrityCheckException(withEntites);
#else
                throw new IntegrityCheckException(error);
#endif
        }
        GraphExplorer.PropagateModifications(modifiables.Inverse());
    }

    public class Trio
    {
        public Trio(IColumn column, Expression value, Expression suffix)
        {
            this.SourceColumn = column.Name;
            this.ParameterName = Signum.Engine.ParameterBuilder.GetParameterName(column.Name);
            this.ParameterBuilder = Connector.Current.ParameterBuilder.ParameterFactory(
                Concat(this.ParameterName, suffix),
                column.DbType, column.Size, column.Precision, column.Scale, column.UserDefinedTypeName, column.Nullable.ToBool(), value);
        }

        public string SourceColumn;
        public string ParameterName;
        public MemberInitExpression ParameterBuilder; //Expression<DbParameter>

        public override string ToString()
        {
            return "{0} {1} {2}".FormatWith(SourceColumn, ParameterName, ParameterBuilder.ToString());
        }

        static readonly MethodInfo miConcat = ReflectionTools.GetMethodInfo(() => string.Concat("", ""));

        internal static Expression Concat(string baseName, Expression suffix)
        {
            return Expression.Call(null, miConcat, Expression.Constant(baseName), suffix);
        }
    }

    static MethodInfo miAdd = ReflectionTools.GetMethodInfo(() => new List<DbParameter>(1).Add(null!));

    public static Expression CreateBlock(IEnumerable<Expression> parameters, IEnumerable<Expression> assigments, Expression parameterList)
    {
        return Expression.Block(
            assigments.OfType<BinaryExpression>().Select(a => (ParameterExpression)a.Left),
            assigments.Concat(parameters.Select(p => Expression.Call(parameterList, miAdd, p))));
    }
}


public partial class TableMList
{
    internal interface IMListCache
    {
        SqlPreCommand? RelationalUpdateSync(Entity parent, string suffix, bool replaceParameter);
        SqlPreCommand? RelationalInsertSync(Entity parent, string suffix, string parentIdVar);
        void RelationalInserts(List<EntityForbidden> entities);
        void RelationalUpdates(List<EntityForbidden> entities);

        object?[] BulkInsertDataRow(Entity entity, object value, int order);
    }

    internal class TableMListCache<T> : IMListCache
    {
        internal TableMList table = null!;

        internal Func<string, string> sqlDelete = null!;
        public Func<Entity, string, DbParameter> DeleteParameter = null!;
        public ConcurrentDictionary<int, Action<List<Entity>>> deleteCache = new ConcurrentDictionary<int, Action<List<Entity>>>();

        Action<List<Entity>> GetDelete(int numEntities)
        {
            return deleteCache.GetOrAdd(numEntities, num =>
            {
                string sql = Enumerable.Range(0, num).ToString(i => sqlDelete(i.ToString()), ";\r\n");

                return list =>
                {
                    List<DbParameter> parameters = new List<DbParameter>();
                    for (int i = 0; i < num; i++)
                    {
                        parameters.Add(DeleteParameter(list[i], i.ToString()));
                    }
                    new SqlPreCommandSimple(sql, parameters).ExecuteNonQuery();
                };
            });
        }

        internal Func<int, string> sqlDeleteExcept = null!;
        public Func<MListDelete, List<DbParameter>> DeleteExceptParameter = null!;
        public ConcurrentDictionary<int, Action<MListDelete>> deleteExceptCache = new ConcurrentDictionary<int, Action<MListDelete>>();

        Action<MListDelete> GetDeleteExcept(int numExceptions)
        {
            return deleteExceptCache.GetOrAdd(numExceptions, num =>
            {
                string sql = sqlDeleteExcept(numExceptions); Enumerable.Range(0, num).ToString(i => sqlDelete(i.ToString()), ";\r\n");

                return delete =>
                {
                    new SqlPreCommandSimple(sql, DeleteExceptParameter(delete)).ExecuteNonQuery();
                };
            });
        }

        public struct MListDelete
        {
            public readonly Entity Entity;
            public readonly PrimaryKey[] ExceptRowIds;

            public MListDelete(Entity ident, PrimaryKey[] exceptRowIds)
            {
                this.Entity = ident;
                this.ExceptRowIds = exceptRowIds;
            }
        }

        internal bool hasOrder = false;
        internal bool isEmbeddedEntity = false;
        internal Func<string, string> sqlUpdate = null!;
        public Action<Entity, PrimaryKey, T, int, Forbidden, string, List<DbParameter>> UpdateParameters = null!;
        public ConcurrentDictionary<int, Action<List<MListUpdate>>> updateCache =
            new ConcurrentDictionary<int, Action<List<MListUpdate>>>();

        Action<List<MListUpdate>> GetUpdate(int numElements)
        {
            return updateCache.GetOrAdd(numElements, num =>
            {
                string sql = Enumerable.Range(0, num).ToString(i => sqlUpdate(i.ToString()), ";\r\n");

                return (List<MListUpdate> list) =>
                {
                    List<DbParameter> parameters = new List<DbParameter>();
                    for (int i = 0; i < num; i++)
                    {
                        var pair = list[i];

                        var row = pair.MList.InnerList[pair.Index];

                        UpdateParameters(pair.Entity, row.RowId!.Value, row.Element, pair.Index, pair.Forbidden, i.ToString(), parameters);
                    }
                    new SqlPreCommandSimple(sql, parameters).ExecuteNonQuery();
                };
            });
        }

        public struct MListUpdate
        {
            public readonly Entity Entity;
            public readonly Forbidden Forbidden;
            public readonly IMListPrivate<T> MList;
            public readonly int Index;

            public MListUpdate(EntityForbidden ef, MList<T> mlist, int index)
            {
                this.Entity = ef.Entity;
                this.Forbidden = ef.Forbidden;
                this.MList = mlist;
                this.Index = index;
            }
        }

        internal Func<string[], bool, string> sqlInsert = null!;
        public Action<Entity, T, int, Forbidden, string, List<DbParameter>> InsertParameters = null!;
        public ConcurrentDictionary<int, Action<List<MListInsert>>> insertCache =
            new ConcurrentDictionary<int, Action<List<MListInsert>>>();

        Action<List<MListInsert>> GetInsert(int numElements)
        {
            return insertCache.GetOrAdd(numElements, num =>
            {
                bool isPostgres = Schema.Current.Settings.IsPostgres;

                string sqlMulti = sqlInsert(Enumerable.Range(0, num).Select(i => i.ToString()).ToArray(), true);

                return (List<MListInsert> list) =>
                {
                    List<DbParameter> result = new List<DbParameter>();
                    for (int i = 0; i < num; i++)
                    {
                        var pair = list[i];
                        InsertParameters(pair.Entity, pair.MList.InnerList[pair.Index].Element, pair.Index, pair.Forbidden, i.ToString(), result);
                    }

                    DataTable dt = new SqlPreCommandSimple(sqlMulti, result).ExecuteDataTable();

                    for (int i = 0; i < num; i++)
                    {
                        var pair = list[i];

                        pair.MList.SetRowId(pair.Index, new PrimaryKey((IComparable)dt.Rows[i][0]));

                        if (this.hasOrder)
                            pair.MList.SetOldIndex(pair.Index);
                    }
                };
            });
        }

        public struct MListInsert
        {
            public readonly Entity Entity;
            public readonly Forbidden Forbidden;
            public readonly IMListPrivate<T> MList;
            public readonly int Index;

            public MListInsert(EntityForbidden ef, MList<T> mlist, int index)
            {
                this.Entity = ef.Entity;
                this.Forbidden = ef.Forbidden;
                this.MList = mlist;
                this.Index = index;
            }
        }

        public object?[] BulkInsertDataRow(Entity entity, object value, int order)
        {
            List<DbParameter> paramList = new List<DbParameter>();
            InsertParameters(entity, (T)value, order, new Forbidden(null), "", paramList);
            return paramList.Select(a => a.Value).ToArray();
        }

        public Func<Entity, MList<T>> Getter = null!;

        public void RelationalInserts(List<EntityForbidden> entities)
        {
            List<MListInsert> toInsert = new List<MListInsert>();

            foreach (var ef in entities)
            {
                if (!ef.Forbidden.IsEmpty)
                    continue; //Will be called again

                MList<T> collection = Getter(ef.Entity);

                if (collection == null)
                    continue;

                if (collection.Modified == ModifiedState.Clean)
                    continue;

                for (int i = 0; i < collection.Count; i++)
                {
                    toInsert.Add(new MListInsert(ef, collection, i));
                }
            }

            toInsert.SplitStatements(this.table.Columns.Count, list => GetInsert(list.Count)(list));
        }

        public void RelationalUpdates(List<EntityForbidden> idents)
        {
            List<Entity> toDelete = new List<Entity>();
            List<MListDelete> toDeleteExcept = new List<MListDelete>();
            List<MListInsert> toInsert = new List<MListInsert>();
            List<MListUpdate> toUpdate = new List<MListUpdate>();

            foreach (var ef in idents)
            {
                if (!ef.Forbidden.IsEmpty)
                    continue; //Will be called again

                MList<T> collection = Getter(ef.Entity);

                if (collection == null)
                    toDelete.Add(ef.Entity);
                else
                {
                    if (collection.Modified == ModifiedState.Clean)
                        continue;

                    var innerList = ((IMListPrivate<T>)collection).InnerList;

                    var exceptions = innerList.Select(a => a.RowId).NotNull().ToArray();

                    if (exceptions.IsEmpty())
                        toDelete.Add(ef.Entity);
                    else
                        toDeleteExcept.Add(new MListDelete(ef.Entity, exceptions));

                    if (isEmbeddedEntity || hasOrder)
                    {
                        for (int i = 0; i < innerList.Count; i++)
                        {
                            var row = innerList[i];

                            if (row.RowId.HasValue)
                            {
                                if (hasOrder && row.OldIndex != i ||
                                   isEmbeddedEntity && ((ModifiableEntity)(object)row.Element!).IsGraphModified)
                                {
                                    toUpdate.Add(new MListUpdate(ef, collection, i));
                                }
                            }
                        }
                    }

                    for (int i = 0; i < innerList.Count; i++)
                    {
                        if (innerList[i].RowId == null)
                            toInsert.Add(new MListInsert(ef, collection, i));
                    }
                }
            }

            toDelete.SplitStatements(2, list => GetDelete(list.Count)(list));

            toDeleteExcept.ForEach(e => GetDeleteExcept(e.ExceptRowIds.Length)(e));
            toUpdate.SplitStatements(this.table.Columns.Count + 2, listPairs => GetUpdate(listPairs.Count)(listPairs));
            toInsert.SplitStatements(this.table.Columns.Count, listPairs => GetInsert(listPairs.Count)(listPairs));
        }

        public SqlPreCommand? RelationalUpdateSync(Entity parent, string suffix, bool replaceParameter)
        {
            MList<T> collection = Getter(parent);

            if (collection == null)
            {
                return new SqlPreCommandSimple(sqlDelete(suffix), new List<DbParameter> { DeleteParameter(parent, suffix) })
                    .ReplaceFirstParameter(replaceParameter ? parent.Id.VariableName : null);
            }

            if (collection.Modified == ModifiedState.Clean)
                return null;

            return SqlPreCommand.Combine(Spacing.Simple,
                new SqlPreCommandSimple(sqlDelete(suffix), new List<DbParameter> { DeleteParameter(parent, suffix) }).ReplaceFirstParameter(replaceParameter ? parent.Id.VariableName : null),
                collection.Select((e, i) => new SqlPreCommandSimple(sqlInsert(new[] { suffix + "_" + i }, false), new List<DbParameter>().Do(ps => InsertParameters(parent, e, i, new Forbidden(), suffix + "_" + i, ps)))
                    .AddComment(e?.ToString())
                    .ReplaceFirstParameter(replaceParameter ? parent.Id.VariableName : null)
                ).Combine(Spacing.Simple));
        }

        public SqlPreCommand? RelationalInsertSync(Entity parent, string suffix, string parentIdVar)
        {
            MList<T> collection = Getter(parent);

            if (collection == null || collection.IsEmpty())
                return null;

            if (collection.Modified == ModifiedState.Clean)
                return null;


            var isPostgres = Schema.Current.Settings.IsPostgres;
            return collection.Select((e, i) =>
            {
                var parameters = new List<DbParameter>();
                InsertParameters(parent, e, i, new Forbidden(new HashSet<Entity> { parent }), suffix + "_" + i, parameters);
                var parentId = parameters.First(); // wont be replaced, generating @parentId
                    parameters.RemoveAt(0);
                string script = sqlInsert(new[] { suffix + "_" + i }, false);
                script = script.Replace(parentId.ParameterName, parentIdVar);
                return new SqlPreCommandSimple(script, parameters).AddComment(e?.ToString());
            }).Combine(Spacing.Simple);
        }
    }

    static GenericInvoker<Func<TableMList, IMListCache>> giCreateCache =
    new((TableMList rt) => rt.CreateCache<int>());

    internal Lazy<IMListCache> cache;

    TableMListCache<T> CreateCache<T>()
    {
        var pb = Connector.Current.ParameterBuilder;
        var isPostgres = Schema.Current.Settings.IsPostgres;

        TableMListCache<T> result = new TableMListCache<T>
        {
            table = this,
            Getter = entity => (MList<T>)Getter(entity),

            sqlDelete = suffix => "DELETE FROM {0} WHERE {1} = {2}".FormatWith(Name, BackReference.Name.SqlEscape(isPostgres), ParameterBuilder.GetParameterName(BackReference.Name + suffix)),
            DeleteParameter = (ident, suffix) => pb.CreateReferenceParameter(ParameterBuilder.GetParameterName(BackReference.Name + suffix), ident.Id, this.BackReference.ReferenceTable.PrimaryKey),

            sqlDeleteExcept = num =>
            {
                var sql = "DELETE FROM {0} WHERE {1} = {2}"
                    .FormatWith(Name, BackReference.Name.SqlEscape(isPostgres), ParameterBuilder.GetParameterName(BackReference.Name));

                sql += " AND {0} NOT IN ({1})"
                    .FormatWith(PrimaryKey.Name.SqlEscape(isPostgres), 0.To(num).Select(i => ParameterBuilder.GetParameterName("e" + i)).ToString(", "));

                return sql;
            },

            DeleteExceptParameter = delete =>
            {
                var list = new List<DbParameter>
                {
                    pb.CreateReferenceParameter(ParameterBuilder.GetParameterName(BackReference.Name), delete.Entity.Id, BackReference)
                };

                list.AddRange(delete.ExceptRowIds.Select((e, i) => pb.CreateReferenceParameter(ParameterBuilder.GetParameterName("e" + i), e, PrimaryKey)));

                return list;
            }
        };
        var paramIdent = Expression.Parameter(typeof(Entity), "ident");
        var paramItem = Expression.Parameter(typeof(T), "item");
        var paramOrder = Expression.Parameter(typeof(int), "order");
        var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
        var paramSuffix = Expression.Parameter(typeof(string), "suffix");
        var paramList = Expression.Parameter(typeof(List<DbParameter>), "paramList");
        {
            var trios = new List<Table.Trio>();
            var assigments = new List<Expression>();

            BackReference.CreateParameter(trios, assigments, paramIdent, paramForbidden, paramSuffix);
            if (this.Order != null)
                Order.CreateParameter(trios, assigments, paramOrder, paramForbidden, paramSuffix);
            Field.CreateParameter(trios, assigments, paramItem, paramForbidden, paramSuffix);

            result.sqlInsert = (suffixes, output) => "INSERT INTO {0} ({1})\r\n{2}VALUES\r\n{3}{4};".FormatWith(Name,
                trios.ToString(p => p.SourceColumn.SqlEscape(isPostgres), ", "),
                output && !isPostgres ? $"OUTPUT INSERTED.{PrimaryKey.Name.SqlEscape(isPostgres)}\r\n" : null,
                suffixes.ToString(s => "  (" + trios.ToString(p => p.ParameterName + s, ", ") + ")", ",\r\n"),
                output && isPostgres ? $"\r\nRETURNING {PrimaryKey.Name.SqlEscape(isPostgres)}" : null);

            var expr = Expression.Lambda<Action<Entity, T, int, Forbidden, string, List<DbParameter>>>(
                Table.CreateBlock(trios.Select(a => a.ParameterBuilder), assigments, paramList), paramIdent, paramItem, paramOrder, paramForbidden, paramSuffix, paramList);

            result.InsertParameters = expr.Compile();
        }

        result.hasOrder = this.Order != null;
        result.isEmbeddedEntity = typeof(EmbeddedEntity).IsAssignableFrom(this.Field.FieldType);

        if (result.isEmbeddedEntity || result.hasOrder)
        {
            var trios = new List<Table.Trio>();
            var assigments = new List<Expression>();

            var paramRowId = Expression.Parameter(typeof(PrimaryKey), "rowId");

            string parentId = "parentId";
            string rowId = "rowId";

            //BackReference.CreateParameter(trios, assigments, paramIdent, paramForbidden, paramSuffix);
            if (this.Order != null)
                Order.CreateParameter(trios, assigments, paramOrder, paramForbidden, paramSuffix);
            Field.CreateParameter(trios, assigments, paramItem, paramForbidden, paramSuffix);

            result.sqlUpdate = suffix => "UPDATE {0} SET \r\n{1}\r\n WHERE {2} = {3} AND {4} = {5};".FormatWith(Name,
                trios.ToString(p => "{0} = {1}".FormatWith(p.SourceColumn.SqlEscape(isPostgres), p.ParameterName + suffix).Indent(2), ",\r\n"),
                this.BackReference.Name.SqlEscape(isPostgres), ParameterBuilder.GetParameterName(parentId + suffix),
                this.PrimaryKey.Name.SqlEscape(isPostgres), ParameterBuilder.GetParameterName(rowId + suffix));

            var parameters = trios.Select(a => a.ParameterBuilder).ToList();

            parameters.Add(pb.ParameterFactory(Table.Trio.Concat(parentId, paramSuffix), this.BackReference.DbType, null, null, null, null, false,
                Expression.Field(Expression.Property(Expression.Field(paramIdent, Table.fiId), "Value"), "Object")));
            parameters.Add(pb.ParameterFactory(Table.Trio.Concat(rowId, paramSuffix), this.PrimaryKey.DbType, null, null, null, null, false,
                Expression.Field(paramRowId, "Object")));

            var expr = Expression.Lambda<Action<Entity, PrimaryKey, T, int, Forbidden, string, List<DbParameter>>>(
                Table.CreateBlock(parameters, assigments, paramList), paramIdent, paramRowId, paramItem, paramOrder, paramForbidden, paramSuffix, paramList);
            result.UpdateParameters = expr.Compile();
        }

        return result;
    }
}

internal static class SaveUtils
{
    public static void SplitStatements<T>(this IList<T> original, int numParametersPerElement, Action<List<T>> action)
    {
        if (!Connector.Current.AllowsMultipleQueries)
        {
            List<T> part = new List<T>(1);
            for (int i = 0; i < original.Count; i++)
            {
                part[0] = original[i];
                action(part);
            }
        }
        else
        {
            var s = Schema.Current.Settings;
            int max = Math.Min(s.MaxNumberOfStatementsInSaveQueries, s.MaxNumberOfParameters / numParametersPerElement);

            List<T> part = new List<T>(max);
            int i = 0;
            for (; i <= original.Count - max; i += max)
            {
                Fill(part, original, i, max);
                action(part);
            }

            int remaining = original.Count - i;
            if (remaining > 0)
            {
                Fill(part, original, i, remaining);
                action(part);
            }
        }
    }

    static List<T> Fill<T>(List<T> part, IList<T> original, int pos, int count)
    {
        part.Clear();
        int max = pos + count;
        for (int i = pos; i < max; i++)
            part.Add(original[i]);
        return part;
    }
}


public abstract partial class Field
{
    protected internal virtual void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix) { }
}

public partial class FieldPrimaryKey
{
    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        trios.Add(new Table.Trio(this, Expression.Field(Expression.Property(value, "Value"), "Object"), suffix));
    }
}

public partial class FieldValue
{
    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        trios.Add(new Table.Trio(this, value, suffix));
    }
}

public partial class FieldTicks
{
    public static readonly ConstructorInfo ciDateTimeTicks = ReflectionTools.GetConstuctorInfo(() => new DateTime(0L));

    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        if (this.Type == this.FieldType)
            trios.Add(new Table.Trio(this, value, suffix));
        else if (this.Type == typeof(DateTime))
            trios.Add(new Table.Trio(this, Expression.New(ciDateTimeTicks, value), suffix));
        else
            throw new NotImplementedException("FieldTicks of type {0} not supported".FormatWith(this.Type));
    }

    internal Expression ConvertTicks(ParameterExpression paramOldTicks)
    {
        if (this.Type == this.FieldType)
            return paramOldTicks;

        if (this.Type == typeof(DateTime))
            return Expression.New(ciDateTimeTicks, paramOldTicks);

        throw new NotImplementedException("FieldTicks of type {0} not supported".FormatWith(this.Type));
    }
}

public static partial class FieldReferenceExtensions
{
    static readonly MethodInfo miGetIdForLite = ReflectionTools.GetMethodInfo(() => GetIdForLite(null!, new Forbidden()));
    static readonly MethodInfo miGetIdForEntity = ReflectionTools.GetMethodInfo(() => GetIdForEntity(null!, new Forbidden()));
    static readonly MethodInfo miGetIdForLiteCleanEntity = ReflectionTools.GetMethodInfo(() => GetIdForLiteCleanEntity(null!, new Forbidden()));

    public static void AssertIsLite(this IFieldReference fr)
    {
        if (!fr.IsLite)
            throw new InvalidOperationException("The field is not a lite");
    }

    public static Expression GetIdFactory(this IFieldReference fr, Expression value, Expression forbidden)
    {
        var mi = !fr.IsLite ? miGetIdForEntity :
            fr.ClearEntityOnSaving ? miGetIdForLiteCleanEntity :
            miGetIdForLite;

        return Expression.Call(mi, value, forbidden);
    }

    static PrimaryKey? GetIdForLite(Lite<IEntity> lite, Forbidden forbidden)
    {
        if (lite == null)
            return null;

        if (lite.EntityOrNull == null)
            return lite.Id;

        if (forbidden.Contains((Entity)lite.EntityOrNull))
            return null;

        lite.RefreshId();

        return lite.Id;
    }

    static PrimaryKey? GetIdForLiteCleanEntity(Lite<IEntity> lite, Forbidden forbidden)
    {
        if (lite == null)
            return null;

        if (lite.EntityOrNull == null)
            return lite.Id;

        if (forbidden.Contains((Entity)lite.EntityOrNull))
            return null;

        lite.RefreshId();
        lite.ClearEntity();

        return lite.Id;
    }

    static PrimaryKey? GetIdForEntity(IEntity value, Forbidden forbidden)
    {
        if (value == null)
            return null;

        Entity ie = (Entity)value;
        return forbidden.Contains(ie) ? (PrimaryKey?)null : ie.Id;
    }

    static MethodInfo miGetTypeForLite = ReflectionTools.GetMethodInfo(() => GetTypeForLite(null!, new Forbidden()));
    static MethodInfo miGetTypeForEntity = ReflectionTools.GetMethodInfo(() => GetTypeForEntity(null!, new Forbidden()));

    public static Expression GetTypeFactory(this IFieldReference fr, Expression value, Expression forbidden)
    {
        return Expression.Call(fr.IsLite ? miGetTypeForLite : miGetTypeForEntity, value, forbidden);
    }

    static Type? GetTypeForLite(Lite<IEntity> value, Forbidden forbidden)
    {
        if (value == null)
            return null;

        Lite<IEntity> l = (Lite<IEntity>)value;
        return l.EntityOrNull == null ? l.EntityType :
             forbidden.Contains((Entity)l.EntityOrNull) ? null :
             l.EntityType;
    }

    static Type? GetTypeForEntity(IEntity value, Forbidden forbidden)
    {
        if (value == null)
            return null;

        Entity ie = (Entity)value;
        return forbidden.Contains(ie) ? null : ie.GetType();
    }
}

public partial class FieldReference
{
    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        trios.Add(new Table.Trio(this, Expression.Call(miUnWrap, this.GetIdFactory(value, forbidden)), suffix));
    }

    static MethodInfo miUnWrap = ReflectionTools.GetMethodInfo(() => Signum.Entities.PrimaryKey.Unwrap(null));
}

public partial class FieldEnum
{
    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        trios.Add(new Table.Trio(this, Expression.Convert(value, this.Type), suffix));
    }
}



public partial class FieldImplementedBy
{
    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        ParameterExpression ibType = Expression.Parameter(typeof(Type), "ibType");
        ParameterExpression ibId = Expression.Parameter(typeof(PrimaryKey?), "ibId");

        assigments.Add(Expression.Assign(ibType, Expression.Call(Expression.Constant(this), miCheckType, this.GetTypeFactory(value, forbidden))));
        assigments.Add(Expression.Assign(ibId, this.GetIdFactory(value, forbidden)));

        foreach (var imp in ImplementationColumns)
        {
            trios.Add(new Table.Trio(imp.Value,
                Expression.Condition(Expression.Equal(ibType, Expression.Constant(imp.Key)),
                    Expression.Field(Expression.Property(ibId, "Value"), "Object"),
                    Expression.Constant(null, typeof(IComparable))),
                suffix));
        }
    }

    static readonly MethodInfo miCheckType = ReflectionTools.GetMethodInfo((FieldImplementedBy fe) => fe.CheckType(null!));

    Type? CheckType(Type? type)
    {
        if (type != null && !ImplementationColumns.ContainsKey(type))
            throw new InvalidOperationException($"Type {type.Name} is not in the list of ImplementedBy of {Route}, currently types allowed: {ImplementationColumns.Keys.ToString(a => a.Name, ", ")}.\r\n" +
                $"Consider writing in your Starter class something like: sb.Schema.Settings.FieldAttributes(({Route.RootType.Name} e) => e.{Route.PropertyString().Replace("/", ".First().")}).Replace(new ImplementedByAttribute({ImplementationColumns.Keys.And(type).ToString(t => $"typeof({t.Name})", ", ")}));");

        return type;
    }
}

public partial class ImplementationColumn
{

}

public partial class FieldImplementedByAll
{

    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        trios.Add(new Table.Trio(TypeColumn, Expression.Call(miConvertType, this.GetTypeFactory(value, forbidden)), suffix));
        foreach (var (k, col) in IdColumns)
        {

            var id = Expression.Parameter(typeof(IComparable), "id");
            var variables = new[] { id };

            var instructions = new List<Expression>();
            instructions.Add(Expression.Assign(id, Expression.Call(miUnWrap, this.GetIdFactory(value, forbidden))));
            instructions.Add(Expression.Call(Expression.Constant(this), miAssertPrimaryKeyTypes, id));

            if (IdColumns.Count > 1)
                instructions.Add(Expression.Condition(Expression.TypeIs(id, col.Type), Expression.Convert(id, col.Type), Expression.Constant(null, col.Type)));
            else
                instructions.Add(Expression.Convert(id, col.Type));

            trios.Add(new Table.Trio(col, Expression.Block(col.Type, variables, instructions), suffix));
        }
    }

    void AssertPrimaryKeyTypes(IComparable? c)
    {
        if (c == null)
            return;

        var col = this.IdColumns.TryGetC(c.GetType());

        if(col == null)
        {
            throw new InvalidOperationException($"Attempt to save a value of type {c.GetType().TypeName()} into the ImplementedByAll field {this.Route}, " +
                $"but {nameof(SchemaSettings)}.{nameof(SchemaSettings.ImplementedByAllPrimaryKeyTypes)} is configured for only {Schema.Current.Settings.ImplementedByAllPrimaryKeyTypes.CommaAnd(a => a.TypeName())}");
        }
    }

    static MethodInfo miUnWrap = ReflectionTools.GetMethodInfo(() => PrimaryKey.Unwrap(null));
    static MethodInfo miAssertPrimaryKeyTypes = ReflectionTools.GetMethodInfo((FieldImplementedByAll iba) => iba.AssertPrimaryKeyTypes(null));
    static MethodInfo miConvertType = ReflectionTools.GetMethodInfo(() => ConvertType(null!));

    static IComparable? ConvertType(Type type)
    {
        if (type == null)
            return null;

        return TypeLogic.TypeToId.GetOrThrow(type, "{0} not registered in the schema").Object;
    }
}

public partial class FieldMList
{
}

public partial class FieldEmbedded
{
    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        ParameterExpression embedded = Expression.Parameter(this.FieldType, "embedded");

        if (HasValue != null)
        {
            trios.Add(new Table.Trio(HasValue, Expression.NotEqual(value, Expression.Constant(null, FieldType)), suffix));
        }

        assigments.Add(Expression.Assign(embedded, Expression.Convert(value, this.FieldType)));

        foreach (var ef in EmbeddedFields.Values)
        {
            ef.Field.CreateParameter(trios, assigments,
                Expression.Condition(
                    Expression.Equal(embedded, Expression.Constant(null, this.FieldType)),
                    Expression.Constant(null, ef.FieldInfo.FieldType.Nullify()),
                    Expression.Field(embedded, ef.FieldInfo).Nullify()), forbidden, suffix);
        }

        if (Mixins != null)
        {
            foreach (var mi in Mixins)
            {
                mi.Value.CreateParameter(trios, assigments, embedded, forbidden, suffix);
            }
        }
    }

    static readonly MethodInfo miCheckNull = ReflectionTools.GetMethodInfo((FieldEmbedded fe) => fe.CheckNull(null!));
    object CheckNull(object obj)
    {
        if (obj == null)
            throw new InvalidOperationException("Impossible to save 'null' on the not-nullable embedded field of type '{0}'".FormatWith(this.FieldType.Name));

        return obj;
    }
}

public partial class FieldMixin
{
    protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression suffix)
    {
        if (value.Type.IsEntity())
        {
            ParameterExpression mixin = Expression.Parameter(this.FieldType, "mixin");

            assigments.Add(Expression.Assign(mixin, Expression.Call(value, MixinDeclarations.miMixin.MakeGenericMethod(this.FieldType))));
            foreach (var ef in Fields.Values)
            {
                ef.Field.CreateParameter(trios, assigments,
                    Expression.Field(mixin, ef.FieldInfo), forbidden, suffix);
            }
        }
        else
        {
            ParameterExpression mixin = Expression.Parameter(this.FieldType, "mixin");

            assigments.Add(Expression.Assign(mixin, Expression.Condition(
                Expression.Equal(value, Expression.Constant(null, value.Type)),
                Expression.Constant(null, this.FieldType),
                Expression.Call(value, MixinDeclarations.miMixin.MakeGenericMethod(this.FieldType))
            )));

            foreach (var ef in Fields.Values)
            {
                ef.Field.CreateParameter(trios, assigments,
                    Expression.Condition(
                    Expression.Equal(mixin, Expression.Constant(null, this.FieldType)),
                    Expression.Constant(null, ef.FieldInfo.FieldType.Nullify()),
                    Expression.Field(mixin, ef.FieldInfo).Nullify()), forbidden, suffix);
            }
        }
    }
}

