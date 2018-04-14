using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities.Reflection;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Data;
using Signum.Utilities.ExpressionTrees;
using System.Threading;
using System.Text;
using Signum.Utilities.DataStructures;
using System.Data.Common;
using System.Collections.Concurrent;
using Signum.Engine.Basics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Signum.Engine.Maps
{
    public struct Forbidden
    {
        public Forbidden(HashSet<Entity> set)
        {
            this.set = set;
        }

        public Forbidden(DirectedGraph<Entity> graph, Entity entity)
        {
            this.set = graph?.TryRelatedTo(entity);
        }

        readonly HashSet<Entity> set;

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

        public EntityForbidden(Entity entity, DirectedGraph<Entity> graph)
        {
            this.Entity = (Entity)entity;
            this.Forbidden = new Forbidden(graph, entity);
        }
    }

    public partial class Table
    {
        ResetLazy<InsertCacheIdentity> inserterIdentity;
        ResetLazy<InsertCacheDisableIdentity> inserterDisableIdentity;

        internal void InsertMany(List<Entity> list, DirectedGraph<Entity> backEdges)
        {
            using (HeavyProfiler.LogNoStackTrace("InsertMany", () => this.Type.TypeName()))
            {
                if (IdentityBehaviour)
                {
                    InsertCacheIdentity ic = inserterIdentity.Value;
                    list.SplitStatements(ls => ic.GetInserter(ls.Count)(ls, backEdges));
                }
                else
                {
                    InsertCacheDisableIdentity ic = inserterDisableIdentity.Value;
                    list.SplitStatements(ls => ic.GetInserter(ls.Count)(ls, backEdges));
                }
            }
        }

        internal object[] BulkInsertDataRow(object/*Entity or IView*/ entity)
        {
            var parameters = IdentityBehaviour ?
                inserterIdentity.Value.InsertParameters((Entity)entity, new Forbidden(), "") :
                inserterDisableIdentity.Value.InsertParameters(entity, new Forbidden(), "");

            return parameters.Select(a => a.Value).ToArray();
        }

        class InsertCacheDisableIdentity
        {
            internal Table table;

            public Func<string, string> SqlInsertPattern;
            public Func<object /*Entity*/, Forbidden, string, List<DbParameter>> InsertParameters;

            ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>>> insertDisableIdentityCache = 
                new ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>>>();

           
            internal Action<List<Entity>, DirectedGraph<Entity>> GetInserter(int numElements)
            {
                return insertDisableIdentityCache.GetOrAdd(numElements, (int num) => num == 1 ? GetInsertDisableIdentity() : GetInsertMultiDisableIdentity(num));
            }

       
            Action<List<Entity>, DirectedGraph<Entity>> GetInsertDisableIdentity()
            {
                string sqlSingle = SqlInsertPattern("");

                return (list, graph) =>
                {
                    Entity etity = list.Single();

                    AssertHasId(etity);

                    if (etity is Entity entity)
                        entity.Ticks = TimeZoneManager.Now.Ticks;

                    table.SetToStrField(etity);

                    var forbidden = new Forbidden(graph, etity);

                    new SqlPreCommandSimple(sqlSingle, InsertParameters(etity, forbidden, "")).ExecuteNonQuery();

                    etity.IsNew = false;
                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.InsertCollections(new List<EntityForbidden> { new EntityForbidden(etity, forbidden) });
                };
            }



            Action<List<Entity>, DirectedGraph<Entity>> GetInsertMultiDisableIdentity(int num)
            {
                string sqlMulti = Enumerable.Range(0, num).ToString(i => SqlInsertPattern(i.ToString()), ";\r\n");

                return (idents, graph) =>
                {
                    for (int i = 0; i < num; i++)
                    {
                        var ident = idents[i];
                        AssertHasId(ident);

                        if (ident is Entity entity)
                            entity.Ticks = TimeZoneManager.Now.Ticks;

                        table.SetToStrField(ident);
                    }

                    List<DbParameter> result = new List<DbParameter>();
                    for (int i = 0; i < idents.Count; i++)
                        result.AddRange(InsertParameters(idents[i], new Forbidden(graph, idents[i]), i.ToString()));

                    new SqlPreCommandSimple(sqlMulti, result).ExecuteNonQuery();
                    for (int i = 0; i < num; i++)
                    {
                        Entity ident = idents[i];

                        ident.IsNew = false;
                    }

                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.InsertCollections(idents.Select(e => new EntityForbidden(e, graph)).ToList());
                };
            }

            internal static InsertCacheDisableIdentity InitializeInsertDisableIdentity(Table table)
            {
                using (HeavyProfiler.LogNoStackTrace("InitializeInsertDisableIdentity", () => table.Type.TypeName()))
                {
                    InsertCacheDisableIdentity result = new InsertCacheDisableIdentity { table = table };

                    var trios = new List<Table.Trio>();
                    var assigments = new List<Expression>();
                    var paramIdent = Expression.Parameter(typeof(object) /*Entity*/, "ident");
                    var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                    var paramSuffix = Expression.Parameter(typeof(string), "suffix");

                    var cast = Expression.Parameter(table.Type, "casted");
                    assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, table.Type)));

                    foreach (var item in table.Fields.Values)
                        item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramSuffix);
                    
                    if(table.Mixins != null)
                        foreach (var item in table.Mixins.Values)
                            item.CreateParameter(trios, assigments, cast, paramForbidden, paramSuffix);

                    result.SqlInsertPattern = (suffix) =>
                        "INSERT {0} ({1})\r\n VALUES ({2})".FormatWith(table.Name,
                        trios.ToString(p => p.SourceColumn.SqlEscape(), ", "),
                        trios.ToString(p => p.ParameterName + suffix, ", "));

                    var expr = Expression.Lambda<Func<object, Forbidden, string, List<DbParameter>>>(
                        CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramForbidden, paramSuffix);

                    result.InsertParameters = expr.Compile();

                    return result;
                }
            }
        }

        class InsertCacheIdentity
        {
            internal Table table;

            public Func<string, bool, string> SqlInsertPattern;
            public Func<Entity, Forbidden, string, List<DbParameter>> InsertParameters;

            ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>>> insertIdentityCache =
               new ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>>>();

            internal Action<List<Entity>, DirectedGraph<Entity>> GetInserter(int numElements)
            {
                return insertIdentityCache.GetOrAdd(numElements, (int num) => GetInsertMultiIdentity(num));
            }

            Action<List<Entity>, DirectedGraph<Entity>> GetInsertMultiIdentity(int num)
            {
                string sqlMulti = new StringBuilder()
                    .AppendLine("DECLARE @MyTable TABLE (Id " + this.table.PrimaryKey.SqlDbType.ToString().ToUpperInvariant() + ");")
                    .AppendLines(Enumerable.Range(0, num).Select(i => SqlInsertPattern(i.ToString(), true)))
                    .AppendLine("SELECT Id from @MyTable").ToString();

                return (idents, graph) =>
                {
                    for (int i = 0; i < num; i++)
                    {
                        var ident = idents[i];
                        AssertNoId(ident);

                        if (ident is Entity entity)
                            entity.Ticks = TimeZoneManager.Now.Ticks;

                        table.SetToStrField(ident);
                    }

                    List<DbParameter> result = new List<DbParameter>();
                    for (int i = 0; i < idents.Count; i++)
                        result.AddRange(InsertParameters(idents[i], new Forbidden(graph, idents[i]), i.ToString()));

                    DataTable dt = new SqlPreCommandSimple(sqlMulti, result).ExecuteDataTable();

                    for (int i = 0; i < num; i++)
                    {
                        Entity ident = idents[i];

                        ident.id = new PrimaryKey((IComparable)dt.Rows[i][0]);
                        ident.IsNew = false;
                    }

                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.InsertCollections(idents.Select(e => new EntityForbidden(e, graph)).ToList());
                };

            }

            internal static InsertCacheIdentity InitializeInsertIdentity(Table table)
            {
                using (HeavyProfiler.LogNoStackTrace("InitializeInsertIdentity", () => table.Type.TypeName()))
                {
                    InsertCacheIdentity result = new InsertCacheIdentity { table = table };

                    var trios = new List<Table.Trio>();
                    var assigments = new List<Expression>();
                    var paramIdent = Expression.Parameter(typeof(Entity), "ident");
                    var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                    var paramSuffix = Expression.Parameter(typeof(string), "suffix");

                    var cast = Expression.Parameter(table.Type, "casted");
                    assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, table.Type)));

                    foreach (var item in table.Fields.Values.Where(a => !(a.Field is FieldPrimaryKey)))
                        item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramSuffix);

                    if (table.Mixins != null)
                        foreach (var item in table.Mixins.Values)
                            item.CreateParameter(trios, assigments, cast, paramForbidden, paramSuffix);

                    result.SqlInsertPattern = (suffix, output) =>
                        "INSERT {0} ({1})\r\n{2} VALUES ({3})".FormatWith(table.Name,
                        trios.ToString(p => p.SourceColumn.SqlEscape(), ", "),
                        output ? "OUTPUT INSERTED.Id into @MyTable \r\n" : null,
                        trios.ToString(p => p.ParameterName + suffix, ", "));


                    var expr = Expression.Lambda<Func<Entity, Forbidden, string, List<DbParameter>>>(
                        CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramForbidden, paramSuffix);

                    result.InsertParameters = expr.Compile();

                    return result;
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


        public IColumn ToStrColumn
        {
            get
            {

                if (Fields.TryGetValue("toStr", out EntityField entity))
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

        internal void UpdateMany(List<Entity> list, DirectedGraph<Entity> backEdges)
        {
            using (HeavyProfiler.LogNoStackTrace("UpdateMany", () => this.Type.TypeName()))
            {
                var uc = updater.Value;
                list.SplitStatements(ls => uc.GetUpdater(ls.Count)(ls, backEdges));
            }
        }

        class UpdateCache
        {
            internal Table table; 

            public Func<string, bool, string> SqlUpdatePattern;
            public Func<Entity, long, Forbidden, string, List<DbParameter>> UpdateParameters;

            ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>>> updateCache = 
                new ConcurrentDictionary<int, Action<List<Entity>, DirectedGraph<Entity>>>();


            public Action<List<Entity>, DirectedGraph<Entity>> GetUpdater(int numElements)
            {
                return updateCache.GetOrAdd(numElements, num => num == 1 ? GenerateUpdate() : GetUpdateMultiple(num)); 
            }

            Action<List<Entity>, DirectedGraph<Entity>> GenerateUpdate()
            {
                string sqlUpdate = SqlUpdatePattern("", false);

                if (table.Ticks != null)
                {
                    return (uniList, graph) =>
                    {
                        Entity ident = uniList.Single();
                        Entity entity = (Entity)ident;

                        long oldTicks = entity.Ticks;
                        entity.Ticks = TimeZoneManager.Now.Ticks;

                        table.SetToStrField(ident);

                        var forbidden = new Forbidden(graph, ident);

                        int num = (int)new SqlPreCommandSimple(sqlUpdate, UpdateParameters(ident, oldTicks, forbidden, "")).ExecuteNonQuery();
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

                        int num = (int)new SqlPreCommandSimple(sqlUpdate, UpdateParameters(ident, -1, forbidden, "")).ExecuteNonQuery();
                        if (num != 1)
                            throw new EntityNotFoundException(ident.GetType(), ident.Id);
                    };
                }
            }

            Action<List<Entity>, DirectedGraph<Entity>> GetUpdateMultiple(int num)
            {
                string sqlMulti = new StringBuilder()
                      .AppendLine("DECLARE @NotFound TABLE (Id " + this.table.PrimaryKey.SqlDbType.ToString().ToUpperInvariant() + ");")
                      .AppendLines(Enumerable.Range(0, num).Select(i => SqlUpdatePattern(i.ToString(), true)))
                      .AppendLine("SELECT Id from @NotFound").ToString();

                if (table.Ticks != null)
                {
                    return (idents, graph) =>
                    {
                        List<DbParameter> parameters = new List<DbParameter>();
                        for (int i = 0; i < num; i++)
                        {
                            Entity entity = (Entity)idents[i];

                            long oldTicks = entity.Ticks;
                            entity.Ticks = TimeZoneManager.Now.Ticks;

                            parameters.AddRange(UpdateParameters(entity, oldTicks, new Forbidden(graph, entity), i.ToString()));
                        }

                        DataTable dt = new SqlPreCommandSimple(sqlMulti, parameters).ExecuteDataTable();

                        if (dt.Rows.Count > 0)
                            throw new ConcurrencyException(table.Type, dt.Rows.Cast<DataRow>().Select(r => new PrimaryKey((IComparable)r[0])).ToArray());

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
                            parameters.AddRange(UpdateParameters(ident, -1, new Forbidden(graph, ident), i.ToString()));
                        }

                        DataTable dt = new SqlPreCommandSimple(sqlMulti, parameters).ExecuteDataTable();

                        if (dt.Rows.Count > 0)
                            throw new EntityNotFoundException(table.Type, dt.Rows.Cast<DataRow>().Select(r => new PrimaryKey((IComparable)r[0])).ToArray());

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
                    UpdateCache result = new UpdateCache { table = table };

                    var trios = new List<Trio>();
                    var assigments = new List<Expression>();
                    var paramIdent = Expression.Parameter(typeof(Entity), "ident");
                    var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                    var paramOldTicks = Expression.Parameter(typeof(long), "oldTicks");
                    var paramSuffix = Expression.Parameter(typeof(string), "suffix");

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

                    result.SqlUpdatePattern = (suffix, output) =>
                    {
                        string update = "UPDATE {0} SET \r\n{1}\r\n WHERE {2} = {3}".FormatWith(
                            table.Name,
                            trios.ToString(p => "{0} = {1}".FormatWith(p.SourceColumn.SqlEscape(), p.ParameterName + suffix).Indent(2), ",\r\n"),
                            table.PrimaryKey.Name.SqlEscape(),
                            idParamName + suffix);


                        if (table.Ticks != null)
                            update += " AND {0} = {1}".FormatWith(table.Ticks.Name.SqlEscape(), oldTicksParamName + suffix);

                        if (!output)
                            return update;
                        else
                            return update + "\r\nIF @@ROWCOUNT = 0 INSERT INTO @NotFound (id) VALUES ({0})".FormatWith(idParamName + suffix);
                    };

                    List<Expression> parameters = new List<Expression>();

                    parameters.Add(pb.ParameterFactory(Trio.Concat(idParamName, paramSuffix), table.PrimaryKey.SqlDbType, null, false,
                        Expression.Field(Expression.Property(Expression.Field(paramIdent, fiId), "Value"), "Object")));

                    if (table.Ticks != null)
                    {
                        parameters.Add(pb.ParameterFactory(Trio.Concat(oldTicksParamName, paramSuffix), table.Ticks.SqlDbType, null, false, table.Ticks.ConvertTicks(paramOldTicks)));
                    }

                    parameters.AddRange(trios.Select(a => (Expression)a.ParameterBuilder));

                    var expr = Expression.Lambda<Func<Entity, long, Forbidden, string, List<DbParameter>>>(
                        CreateBlock(parameters, assigments), paramIdent, paramOldTicks, paramForbidden, paramSuffix);

                    result.UpdateParameters = expr.Compile();

                    return result;
                }
            }

        }

        ResetLazy<UpdateCache> updater;

   
        class CollectionsCache
        {
            public Func<Entity, string, bool, SqlPreCommand> InsertCollectionsSync;

            public Action<List<EntityForbidden>> InsertCollections;
            public Action<List<EntityForbidden>> UpdateCollections;

            internal static CollectionsCache InitializeCollections(Table table)
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
                        return new CollectionsCache
                        {
                            InsertCollections = (entities) =>
                            {
                                foreach (var rc in caches)
                                    rc.RelationalInserts(entities);
                            },

                            UpdateCollections = (entities) =>
                            {
                                foreach (var rc in caches)
                                    rc.RelationalUpdates(entities);
                            },

                            InsertCollectionsSync = (ident, suffix, replaceParameter) =>
                                caches.Select((rc, i) => rc.RelationalUpdateSync(ident, suffix + "_" + i.ToString(), replaceParameter)).Combine(Spacing.Double)
                        };
                    }
                }
            }
        }

        ResetLazy<CollectionsCache> saveCollections;


        public SqlPreCommand InsertSqlSync(Entity ident, bool includeCollections = true, string comment = null, string suffix = "")
        {
            PrepareEntitySync(ident);
            SetToStrField(ident);

            SqlPreCommandSimple insert = IdentityBehaviour ?
                new SqlPreCommandSimple(
                    inserterIdentity.Value.SqlInsertPattern(suffix, false),
                    inserterIdentity.Value.InsertParameters(ident, new Forbidden(), suffix)).AddComment(comment) :
                new SqlPreCommandSimple(
                    inserterDisableIdentity.Value.SqlInsertPattern(suffix),
                    inserterDisableIdentity.Value.InsertParameters(ident, new Forbidden(), suffix)).AddComment(comment);

            if (!includeCollections)
                return insert;

            var cc = saveCollections.Value;
            if (cc == null)
                return insert;

            SqlPreCommand collections = cc.InsertCollectionsSync((Entity)ident, suffix, false);

            if (collections == null)
                return insert;

            SqlPreCommand declareParent = new SqlPreCommandSimple("DECLARE @parentId INT") { GoBefore = true };

            SqlPreCommand setParent = new SqlPreCommandSimple("SET @parentId = @@Identity");

            return SqlPreCommand.Combine(Spacing.Simple, declareParent, insert, setParent, collections);
        }

        public SqlPreCommand UpdateSqlSync<T>(T entity, Expression<Func<T, bool>> where, bool includeCollections = true, string comment = null, string suffix = "")
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
            var parameters = uc.UpdateParameters(entity, (entity as Entity)?.Ticks ?? -1, new Forbidden(), suffix);

            SqlPreCommand update;
            if (where != null)
            {
                update = SqlPreCommand.Combine(Spacing.Simple,
                    DeclarePrimaryKeyVariable(entity, where),
                    new SqlPreCommandSimple(sql, parameters).AddComment(comment).ReplaceFirstParameter(entity.Id.VariableName));
            }
            else
            {
                update = new SqlPreCommandSimple(sql, parameters).AddComment(comment);
            }

            if (!includeCollections)
                return update;

            var cc = saveCollections.Value;
            if (cc == null)
                return update;

            SqlPreCommand collections = cc.InsertCollectionsSync((Entity)entity, suffix, where != null);
            
            return SqlPreCommand.Combine(Spacing.Simple, update, collections);
        }

        void PrepareEntitySync(Entity entity)
        {
            Schema current = Schema.Current;
            DirectedGraph<Modifiable> modifiables = Saver.PreSaving(() => GraphExplorer.FromRoot(entity));

            var error = GraphExplorer.FullIntegrityCheck(modifiables);
            if (error != null)
                throw new IntegrityCheckException(error);

            GraphExplorer.PropagateModifications(modifiables.Inverse());
        }

        public class Trio
        {
            public Trio(IColumn column, Expression value, Expression suffix)
            {
                this.SourceColumn = column.Name;
                this.ParameterName = Engine.ParameterBuilder.GetParameterName(column.Name);
                this.ParameterBuilder = Connector.Current.ParameterBuilder.ParameterFactory(Concat(this.ParameterName, suffix), column.SqlDbType, column.UserDefinedTypeName, column.Nullable.ToBool(), value);
            }

            public string SourceColumn;
            public string ParameterName;
            public MemberInitExpression ParameterBuilder; //Expression<DbParameter>

            public override string ToString()
            {
                return "{0} {1} {2}".FormatWith(SourceColumn, ParameterName, ParameterBuilder.ToString());
            }

            static MethodInfo miConcat = ReflectionTools.GetMethodInfo(() => string.Concat("", ""));

            internal static Expression Concat(string baseName, Expression suffix)
            {
                return Expression.Call(null, miConcat, Expression.Constant(baseName), suffix);
            }
        }

        static ConstructorInfo ciNewList = ReflectionTools.GetConstuctorInfo(() => new List<DbParameter>(1));

        public static Expression CreateBlock(IEnumerable<Expression> parameters, IEnumerable<Expression> assigments)
        {
            return Expression.Block(assigments.OfType<BinaryExpression>().Select(a => (ParameterExpression)a.Left),
                assigments.And(
                Expression.ListInit(Expression.New(ciNewList, Expression.Constant(parameters.Count())),
                parameters)));
        }
    }


    public partial class TableMList
    {
        internal interface IMListCache
        {
            SqlPreCommand RelationalUpdateSync(Entity parent, string suffix, bool replaceParameter);
            void RelationalInserts(List<EntityForbidden> entities);
            void RelationalUpdates(List<EntityForbidden> entities);

            object[] BulkInsertDataRow(Entity entity, object value, int order);
        }

        internal class TableMListCache<T> : IMListCache
        {
            internal TableMList table;

            internal Func<string, string> sqlDelete;
            public Func<Entity, string, DbParameter> DeleteParameter;
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

            internal Func<int, string> sqlDeleteExcept;
            public Func<MListDelete, List<DbParameter>> DeleteExceptParameter;
            public ConcurrentDictionary<int, Action<MListDelete>> deleteExceptCache = new ConcurrentDictionary<int, Action<MListDelete>>();

            Action<MListDelete> GetDeleteExcept(int numExceptions)
            {
                return deleteExceptCache.GetOrAdd(numExceptions, num =>
                {
                    string sql = sqlDeleteExcept(numExceptions); Enumerable.Range(0, num).ToString(i => sqlDelete(i.ToString()), ";\r\n");

                    return delete =>
                    {
                        new SqlPreCommandSimple(sql,  DeleteExceptParameter(delete)).ExecuteNonQuery();
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
            internal Func<string, string> sqlUpdate;
            public Func<Entity, PrimaryKey, T, int, Forbidden, string, List<DbParameter>> UpdateParameters;
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

                            parameters.AddRange(UpdateParameters(pair.Entity, row.RowId.Value, row.Element, pair.Index, pair.Forbidden, i.ToString()));
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

            internal Func<string, bool, string> sqlInsert;
            public Func<Entity, T, int, Forbidden, string, List<DbParameter>> InsertParameters;
            public ConcurrentDictionary<int, Action<List<MListInsert>>> insertCache =
                new ConcurrentDictionary<int, Action<List<MListInsert>>>();

            Action<List<MListInsert>> GetInsert(int numElements)
            {
                return insertCache.GetOrAdd(numElements, num =>
                {
                    string sqlMulti = new StringBuilder()
                          .AppendLine("DECLARE @MyTable TABLE (Id " + this.table.PrimaryKey.SqlDbType.ToString().ToUpperInvariant() + ");")
                          .AppendLines(Enumerable.Range(0, num).Select(i => sqlInsert(i.ToString(), true)))
                          .AppendLine("SELECT Id from @MyTable").ToString();

                    return (List<MListInsert> list) =>
                    {
                        List<DbParameter> result = new List<DbParameter>();
                        for (int i = 0; i < num; i++)
                        {
                            var pair = list[i];
                            result.AddRange(InsertParameters(pair.Entity, pair.MList.InnerList[pair.Index].Element, pair.Index, pair.Forbidden, i.ToString()));
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

            public object[] BulkInsertDataRow(Entity entity, object value, int order)
            {
                return InsertParameters(entity, (T)value, order, new Forbidden(null), "").Select(a => a.Value).ToArray(); 
            }

            public Func<Entity, MList<T>> Getter;

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

                toInsert.SplitStatements(list => GetInsert(list.Count)(list));
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

                                if(row.RowId.HasValue)
                                {
                                    if(hasOrder  && row.OldIndex != i ||
                                       isEmbeddedEntity && ((ModifiableEntity)(object)row.Element).IsGraphModified)
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

                toDelete.SplitStatements(list => GetDelete(list.Count)(list));

                toDeleteExcept.ForEach(e => GetDeleteExcept(e.ExceptRowIds.Length)(e)); 
                toUpdate.SplitStatements(listPairs => GetUpdate(listPairs.Count)(listPairs));
                toInsert.SplitStatements(listPairs => GetInsert(listPairs.Count)(listPairs));
            }

            public SqlPreCommand RelationalUpdateSync(Entity parent, string suffix, bool replaceParameter)
            {
                MList<T> collection = Getter(parent);

                if (collection == null)
                {
                    if (parent.IsNew)
                        return null;

                    return new SqlPreCommandSimple(sqlDelete(suffix), new List<DbParameter> { DeleteParameter(parent, suffix) })
                        .ReplaceFirstParameter(replaceParameter ? parent.Id.VariableName : null);
                }

                if (collection.Modified == ModifiedState.Clean)
                    return null;

                if (parent.IsNew)
                {
                    return collection.Select((e, i) =>
                    {
                        var parameters = InsertParameters(parent, e, i, new Forbidden(new HashSet<Entity> { parent }), suffix + "_" + i);
                        var parentId = parameters.First(); // wont be replaced, generating @parentId
                        parameters.RemoveAt(0);
                        string script = sqlInsert(suffix + "_" + i, false);
                        script = script.Replace(parentId.ParameterName, "@parentId");
                        return new SqlPreCommandSimple(script, parameters).AddComment(e.ToString());
                    }).Combine(Spacing.Simple);
                }
                else
                {
                    return SqlPreCommand.Combine(Spacing.Simple,
                        new SqlPreCommandSimple(sqlDelete(suffix), new List<DbParameter> { DeleteParameter(parent, suffix) }).ReplaceFirstParameter(replaceParameter ? parent.Id.VariableName : null),
                        collection.Select((e, i) => new SqlPreCommandSimple(sqlInsert(suffix + "_" + i, false), InsertParameters(parent, e, i, new Forbidden(), suffix + "_" + i))
                            .AddComment(e.ToString())
                            .ReplaceFirstParameter(replaceParameter ? parent.Id.VariableName : null)
                        ).Combine(Spacing.Simple));
                }
            }
        }

        static GenericInvoker<Func<TableMList, IMListCache>> giCreateCache =
            new GenericInvoker<Func<TableMList, IMListCache>>((TableMList rt) => rt.CreateCache<int>());

        internal Lazy<IMListCache> cache;

        TableMListCache<T> CreateCache<T>()
        {
            var pb = Connector.Current.ParameterBuilder;

            TableMListCache<T> result = new TableMListCache<T>()
            {
                table = this,
                Getter = entity => (MList<T>)Getter(entity),

                sqlDelete = suffix => "DELETE {0} WHERE {1} = {2}".FormatWith(Name, BackReference.Name.SqlEscape(), ParameterBuilder.GetParameterName(BackReference.Name + suffix)),
                DeleteParameter = (ident, suffix) => pb.CreateReferenceParameter(ParameterBuilder.GetParameterName(BackReference.Name + suffix), ident.Id, this.BackReference.ReferenceTable.PrimaryKey),

                sqlDeleteExcept = num =>
                {
                    var sql = "DELETE {0} WHERE {1} = {2}"
                        .FormatWith(Name, BackReference.Name.SqlEscape(), ParameterBuilder.GetParameterName(BackReference.Name));

                    sql += " AND {0} NOT IN ({1})"
                        .FormatWith(PrimaryKey.Name.SqlEscape(), 0.To(num).Select(i => ParameterBuilder.GetParameterName("e" + i)).ToString(", "));

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
            
            
            {
                var trios = new List<Table.Trio>();
                var assigments = new List<Expression>();

                BackReference.CreateParameter(trios, assigments, paramIdent, paramForbidden, paramSuffix);
                if (this.Order != null)
                    Order.CreateParameter(trios, assigments, paramOrder, paramForbidden, paramSuffix);
                Field.CreateParameter(trios, assigments, paramItem, paramForbidden, paramSuffix);

                result.sqlInsert = (suffix, output) => "INSERT {0} ({1})\r\n{2} VALUES ({3})".FormatWith(Name,
                    trios.ToString(p => p.SourceColumn.SqlEscape(), ", "),
                    output ? "OUTPUT INSERTED.Id into @MyTable \r\n" : null,
                    trios.ToString(p => p.ParameterName + suffix, ", "));

                var expr = Expression.Lambda<Func<Entity, T, int, Forbidden, string, List<DbParameter>>>(
                    Table.CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramItem, paramOrder, paramForbidden, paramSuffix);

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

                result.sqlUpdate = suffix => "UPDATE {0} SET \r\n{1}\r\n WHERE {2} = {3} AND {4} = {5}".FormatWith(Name,
                    trios.ToString(p => "{0} = {1}".FormatWith(p.SourceColumn.SqlEscape(), p.ParameterName + suffix).Indent(2), ",\r\n"),
                    this.BackReference.Name.SqlEscape(), ParameterBuilder.GetParameterName(parentId + suffix),
                    this.PrimaryKey.Name.SqlEscape(), ParameterBuilder.GetParameterName(rowId + suffix));

                var parameters = trios.Select(a => a.ParameterBuilder).ToList();

                parameters.Add(pb.ParameterFactory(Table.Trio.Concat(parentId, paramSuffix), this.BackReference.SqlDbType, null, false,
                    Expression.Field(Expression.Property(Expression.Field(paramIdent, Table.fiId), "Value"), "Object")));
                parameters.Add(pb.ParameterFactory(Table.Trio.Concat(rowId, paramSuffix), this.PrimaryKey.SqlDbType, null, false,
                    Expression.Field(paramRowId, "Object")));

                var expr = Expression.Lambda<Func<Entity, PrimaryKey, T, int, Forbidden, string, List<DbParameter>>>(
                    Table.CreateBlock(parameters, assigments), paramIdent, paramRowId, paramItem, paramOrder, paramForbidden, paramSuffix);
                result.UpdateParameters = expr.Compile();
            }

            return result;
        }
    }

    internal static class SaveUtils
    {
        public static void SplitStatements<T>(this IList<T> original, Action<List<T>> action)
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
                int max = Schema.Current.Settings.MaxNumberOfStatementsInSaveQueries;

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
        static MethodInfo miGetIdForLite = ReflectionTools.GetMethodInfo(() => GetIdForLite(null, new Forbidden()));
        static MethodInfo miGetIdForEntity = ReflectionTools.GetMethodInfo(() => GetIdForEntity(null, new Forbidden()));
        static MethodInfo miGetIdForLiteCleanEntity = ReflectionTools.GetMethodInfo(() => GetIdForLiteCleanEntity(null, new Forbidden()));

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

        static MethodInfo miGetTypeForLite = ReflectionTools.GetMethodInfo(() => GetTypeForLite(null, new Forbidden()));
        static MethodInfo miGetTypeForEntity = ReflectionTools.GetMethodInfo(() => GetTypeForEntity(null, new Forbidden()));

        public static Expression GetTypeFactory(this IFieldReference fr, Expression value, Expression forbidden)
        {
            return Expression.Call(fr.IsLite ? miGetTypeForLite : miGetTypeForEntity, value, forbidden);
        }

        static Type GetTypeForLite(Lite<IEntity> value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            Lite<IEntity> l = (Lite<IEntity>)value;
            return l.EntityOrNull == null ? l.EntityType :
                 forbidden.Contains((Entity)l.EntityOrNull) ? null :
                 l.EntityType;
        }

        static Type GetTypeForEntity(IEntity value, Forbidden forbidden)
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

        static MethodInfo miCheckType = ReflectionTools.GetMethodInfo((FieldImplementedBy fe) => fe.CheckType(null));

        Type CheckType(Type type)
        {
            if (type != null && !ImplementationColumns.ContainsKey(type))
                throw new InvalidOperationException("Type {0} is not in the list of ImplementedBy:\r\n{1}".FormatWith(type.Name, ImplementationColumns.ToString(kvp => "{0} -> {1}".FormatWith(kvp.Key.Name, kvp.Value.Name), "\r\n")));

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
            trios.Add(new Table.Trio(Column, Expression.Call(miUnWrapToString, this.GetIdFactory(value, forbidden)), suffix));
            trios.Add(new Table.Trio(ColumnType, Expression.Call(miConvertType, this.GetTypeFactory(value, forbidden)), suffix));
        }

        static MethodInfo miUnWrapToString = ReflectionTools.GetMethodInfo(() => PrimaryKey.UnwrapToString(null));
        static MethodInfo miConvertType = ReflectionTools.GetMethodInfo(() => ConvertType(null));

        static IComparable ConvertType(Type type)
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
        }

        static MethodInfo miCheckNull = ReflectionTools.GetMethodInfo((FieldEmbedded fe) => fe.CheckNull(null));
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
            ParameterExpression mixin = Expression.Parameter(this.FieldType, "mixin");

            assigments.Add(Expression.Assign(mixin, Expression.Call(value, MixinDeclarations.miMixin.MakeGenericMethod(this.FieldType))));
            foreach (var ef in Fields.Values)
            {
                ef.Field.CreateParameter(trios, assigments,
                    Expression.Field(mixin, ef.FieldInfo), forbidden, suffix);
            }
        }
    }

}
