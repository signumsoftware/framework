using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Engine.Exceptions;
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

namespace Signum.Engine.Maps
{
    public struct Forbidden
    {
        public Forbidden(HashSet<IdentifiableEntity> set)
        {
            this.set = set;
        }

        public Forbidden(DirectedGraph<IdentifiableEntity> graph, IdentifiableEntity entity)
        {
            this.set = graph == null ? null : graph.TryRelatedTo(entity);
        }

        public Forbidden(DirectedGraph<IdentifiableEntity> graph, List<IdentifiableEntity> entities)
        {
            if (graph == null)
                this.set = null;
            else
            {
                this.set = new HashSet<IdentifiableEntity>();
                foreach (var entity in entities)
                    this.set.AddRange(graph.TryRelatedTo(entity));
            }
        }

        readonly HashSet<IdentifiableEntity> set;

        public bool IsEmpty
        {
            get { return set == null || set.Count == 0; }
        }

        public bool Contains(IdentifiableEntity entity)
        {
            return set != null && set.Contains(entity);
        }
    }

    public partial class Table
    {
        ResetLazy<InsertCacheIdentity> inserterIdentity;
        ResetLazy<InsertCacheDisableIdentity> inserterDisableIdentity;

        internal void InsertMany(List<IdentifiableEntity> list, DirectedGraph<IdentifiableEntity> backEdges)
        {
            using (HeavyProfiler.LogNoStackTrace("InsertMany", () => this.Type.TypeName()))
            {
                if (Identity)
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


        class InsertCacheDisableIdentity
        {
            internal Table table;

            public Func<string, string> SqlInsertPattern;
            public Func<IdentifiableEntity, Forbidden, string, List<DbParameter>> InsertParameters;

            ConcurrentDictionary<int, Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>>> insertDisableIdentityCache = 
                new ConcurrentDictionary<int, Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>>>();

           
            internal Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetInserter(int numElements)
            {
                return insertDisableIdentityCache.GetOrAdd(numElements, (int num) => num == 1 ? GetInsertDisableIdentity() : GetInsertMultiDisableIdentity(num));
            }

       
            Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetInsertDisableIdentity()
            {
                string sqlSingle = SqlInsertPattern("");

                return (list, graph) =>
                {
                    IdentifiableEntity ident = list.Single();

                    AssertHasId(ident);

                    Entity entity = ident as Entity;
                    if (entity != null)
                        entity.Ticks = TimeZoneManager.Now.Ticks;

                    table.SetToStrField(ident);

                    var forbidden = new Forbidden(graph, ident);

                    new SqlPreCommandSimple(sqlSingle, InsertParameters(ident, forbidden, "")).ExecuteNonQuery();

                    ident.IsNew = false;
                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.InsertCollections(new List<IdentifiableEntity> { ident }, forbidden);
                };
            }


            Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetInsertMultiDisableIdentity(int num)
            {
                string sqlMulti = Enumerable.Range(0, num).ToString(i => SqlInsertPattern(i.ToString()), ";\r\n");

                return (idents, graph) =>
                {
                    for (int i = 0; i < num; i++)
                    {
                        var ident = idents[i];
                        AssertHasId(ident);

                        Entity entity = ident as Entity;
                        if (entity != null)
                            entity.Ticks = TimeZoneManager.Now.Ticks;

                        table.SetToStrField(ident);
                    }

                    List<DbParameter> result = new List<DbParameter>();
                    for (int i = 0; i < idents.Count; i++)
                        result.AddRange(InsertParameters(idents[i], new Forbidden(graph, idents[i]), i.ToString()));

                    new SqlPreCommandSimple(sqlMulti, result).ExecuteNonQuery();
                    for (int i = 0; i < num; i++)
                    {
                        IdentifiableEntity ident = idents[i];

                        ident.IsNew = false;
                    }

                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.InsertCollections(idents, new Forbidden(graph, idents));
                };
            }

            internal static InsertCacheDisableIdentity InitializeInsertDisableIdentity(Table table)
            {
                using (HeavyProfiler.LogNoStackTrace("InitializeInsertDisableIdentity", () => table.Type.TypeName()))
                {
                    InsertCacheDisableIdentity result = new InsertCacheDisableIdentity { table = table };

                    var trios = new List<Table.Trio>();
                    var assigments = new List<Expression>();
                    var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
                    var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                    var paramPostfix = Expression.Parameter(typeof(string), "postfix");

                    var cast = Expression.Parameter(table.Type, "casted");
                    assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, table.Type)));

                    foreach (var item in table.Fields.Values)
                    {
                        item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramPostfix);
                    }
                    
                    if(table.Mixins != null)
                        foreach (var item in table.Mixins.Values)
                        {
                            item.CreateParameter(trios, assigments, cast, paramForbidden, paramPostfix);
                        }

                    result.SqlInsertPattern = (post) =>
                        "INSERT {0} ({1})\r\n VALUES ({2})".Formato(table.Name,
                        trios.ToString(p => p.SourceColumn.SqlScape(), ", "),
                        trios.ToString(p => p.ParameterName + post, ", "));

                    var expr = Expression.Lambda<Func<IdentifiableEntity, Forbidden, string, List<DbParameter>>>(
                        CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramForbidden, paramPostfix);

                    result.InsertParameters = expr.Compile();

                    return result;
                }
            }
        }

        class InsertCacheIdentity
        {
            internal Table table;

            public Func<string, bool, string> SqlInsertPattern;
            public Func<IdentifiableEntity, Forbidden, string, List<DbParameter>> InsertParameters;

            ConcurrentDictionary<int, Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>>> insertIdentityCache =
               new ConcurrentDictionary<int, Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>>>();

            internal Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetInserter(int numElements)
            {
                return insertIdentityCache.GetOrAdd(numElements, (int num) => num == 1 ? GetInsertIdentity() : GetInsertMultiIdentity(num));
            }

            Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetInsertIdentity()
            {
                string sqlSingle = SqlInsertPattern("", false) + ";SELECT CONVERT(Int,@@Identity) AS [newID]";

                return (list, graph) =>
                {
                    IdentifiableEntity ident = list.Single();

                    AssertNoId(ident);

                    Entity entity = ident as Entity;
                    if (entity != null)
                        entity.Ticks = TimeZoneManager.Now.Ticks;

                    table.SetToStrField(ident);

                    var forbidden = new Forbidden(graph, ident);

                    ident.id = (int)new SqlPreCommandSimple(sqlSingle, InsertParameters(ident, forbidden, "")).ExecuteScalar();

                    ident.IsNew = false;

                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.InsertCollections(new List<IdentifiableEntity> { ident }, forbidden);
                };
            }


            Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetInsertMultiIdentity(int num)
            {
                string sqlMulti = new StringBuilder()
                    .AppendLine("DECLARE @MyTable TABLE (Id INT);")
                    .AppendLines(Enumerable.Range(0, num).Select(i => SqlInsertPattern(i.ToString(), true)))
                    .AppendLine("SELECT Id from @MyTable").ToString();

                return (idents, graph) =>
                {
                    for (int i = 0; i < num; i++)
                    {
                        var ident = idents[i];
                        AssertNoId(ident);

                        Entity entity = ident as Entity;
                        if (entity != null)
                            entity.Ticks = TimeZoneManager.Now.Ticks;

                        table.SetToStrField(ident);
                    }

                    List<DbParameter> result = new List<DbParameter>();
                    for (int i = 0; i < idents.Count; i++)
                        result.AddRange(InsertParameters(idents[i], new Forbidden(graph, idents[i]), i.ToString()));

                    DataTable dt = new SqlPreCommandSimple(sqlMulti, result).ExecuteDataTable();

                    for (int i = 0; i < num; i++)
                    {
                        IdentifiableEntity ident = idents[i];

                        ident.id = (int)dt.Rows[i][0];
                        ident.IsNew = false;
                    }

                    if (table.saveCollections.Value != null)
                        table.saveCollections.Value.InsertCollections(idents, new Forbidden(graph, idents));
                };

            }

            internal static InsertCacheIdentity InitializeInsertIdentity(Table table)
            {
                using (HeavyProfiler.LogNoStackTrace("InitializeInsertIdentity", () => table.Type.TypeName()))
                {
                    InsertCacheIdentity result = new InsertCacheIdentity { table = table };

                    var trios = new List<Table.Trio>();
                    var assigments = new List<Expression>();
                    var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
                    var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                    var paramPostfix = Expression.Parameter(typeof(string), "postfix");

                    var cast = Expression.Parameter(table.Type, "casted");
                    assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, table.Type)));

                    foreach (var item in table.Fields.Values.Where(a => !(a.Field is FieldPrimaryKey)))
                    {
                        item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramPostfix);
                    }


                    if (table.Mixins != null)
                        foreach (var item in table.Mixins.Values)
                        {
                            item.CreateParameter(trios, assigments, cast, paramForbidden, paramPostfix);
                        }

                    result.SqlInsertPattern = (post, output) =>
                        "INSERT {0} ({1})\r\n{2} VALUES ({3})".Formato(table.Name,
                        trios.ToString(p => p.SourceColumn.SqlScape(), ", "),
                        output ? "OUTPUT INSERTED.Id into @MyTable \r\n" : null,
                        trios.ToString(p => p.ParameterName + post, ", "));


                    var expr = Expression.Lambda<Func<IdentifiableEntity, Forbidden, string, List<DbParameter>>>(
                        CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramForbidden, paramPostfix);

                    result.InsertParameters = expr.Compile();

                    return result;
                }
            }
        }


        static void AssertHasId(IdentifiableEntity ident)
        {
            if (ident.IdOrNull == null)
                throw new InvalidOperationException("{0} should have an Id, since the table has no Identity".Formato(ident, ident.IdOrNull));
        }

        static void AssertNoId(IdentifiableEntity ident)
        {
            if (ident.IdOrNull != null)
                throw new InvalidOperationException("{0} is new, but has Id {1}".Formato(ident, ident.IdOrNull));
        }


        public IColumn ToStrColumn
        {
            get
            {
                EntityField entity;

                if (Fields.TryGetValue("toStr", out entity))
                    return (IColumn)entity.Field;

                return null;
            }
        }

        private bool SetToStrField(IdentifiableEntity entity)
        {
            var toStrColumn = ToStrColumn;
            if (toStrColumn != null)
            {
                var newStr = entity.ToString();
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


        static FieldInfo fiId = ReflectionTools.GetFieldInfo((IdentifiableEntity i) => i.id);

        internal void UpdateMany(List<IdentifiableEntity> list, DirectedGraph<IdentifiableEntity> backEdges)
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
            public Func<IdentifiableEntity, long, Forbidden, string, List<DbParameter>> UpdateParameters;

            ConcurrentDictionary<int, Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>>> updateCache = 
                new ConcurrentDictionary<int, Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>>>();


            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetUpdater(int numElements)
            {
                return updateCache.GetOrAdd(numElements, num => num == 1 ? GenerateUpdate() : GetUpdateMultiple(num)); 
            }

            Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GenerateUpdate()
            {
                string sqlUpdate = SqlUpdatePattern("", false);

                if (typeof(Entity).IsAssignableFrom(table.Type))
                {
                    return (uniList, graph) =>
                    {
                        IdentifiableEntity ident = uniList.Single();
                        Entity entity = (Entity)ident;

                        long oldTicks = entity.Ticks;
                        entity.Ticks = TimeZoneManager.Now.Ticks;

                        table.SetToStrField(ident);

                        var forbidden = new Forbidden(graph, ident);

                        int num = (int)new SqlPreCommandSimple(sqlUpdate, UpdateParameters(ident, oldTicks, forbidden, "")).ExecuteNonQuery();
                        if (num != 1)
                            throw new ConcurrencyException(ident.GetType(), ident.Id);

                        if (table.saveCollections.Value != null)
                            table.saveCollections.Value.UpdateCollections(new List<IdentifiableEntity> { ident }, forbidden);
                    };
                }
                else
                {
                    return (uniList, graph) =>
                    {
                        IdentifiableEntity ident = uniList.Single();

                        table.SetToStrField(ident);

                        var forbidden = new Forbidden(graph, ident);

                        int num = (int)new SqlPreCommandSimple(sqlUpdate, UpdateParameters(ident, -1, forbidden, "")).ExecuteNonQuery();
                        if (num != 1)
                            throw new EntityNotFoundException(ident.GetType(), ident.Id);

                        if (table.saveCollections.Value != null)
                            table.saveCollections.Value.UpdateCollections(new List<IdentifiableEntity> { ident }, forbidden);
                    };
                }
            }

            Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetUpdateMultiple(int num)
            {
                string sqlMulti = new StringBuilder()
                      .AppendLine("DECLARE @NotFound TABLE (Id INT);")
                      .AppendLines(Enumerable.Range(0, num).Select(i => SqlUpdatePattern(i.ToString(), true)))
                      .AppendLine("SELECT Id from @NotFound").ToString();

                if (typeof(Entity).IsAssignableFrom(table.Type))
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
                            throw new ConcurrencyException(table.Type, dt.Rows.Cast<DataRow>().Select(r => (int)r[0]).ToArray());

                        if (table.saveCollections.Value != null)
                            table.saveCollections.Value.UpdateCollections(idents, new Forbidden(graph, idents));
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
                            throw new EntityNotFoundException(table.Type, dt.Rows.Cast<DataRow>().Select(r => (int)r[0]).ToArray());

                        for (int i = 0; i < num; i++)
                        {
                            IdentifiableEntity ident = idents[i];
                        }

                        if (table.saveCollections.Value != null)
                            table.saveCollections.Value.UpdateCollections(idents, new Forbidden(graph, idents));
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
                    var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
                    var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
                    var paramOldTicks = Expression.Parameter(typeof(long), "oldTicks");
                    var paramPostfix = Expression.Parameter(typeof(string), "postfix");

                    var cast = Expression.Parameter(table.Type);
                    assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, table.Type)));

                    foreach (var item in table.Fields.Values.Where(a => !(a.Field is FieldPrimaryKey)))
                        item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramPostfix);

                    var pb = Connector.Current.ParameterBuilder;

                    string idParamName = ParameterBuilder.GetParameterName("id");

                    string oldTicksParamName = ParameterBuilder.GetParameterName("old_ticks");

                    result.SqlUpdatePattern = (post, output) =>
                    {
                        string update = "UPDATE {0} SET \r\n{1}\r\n WHERE id = {2}".Formato(
                            table.Name,
                            trios.ToString(p => "{0} = {1}".Formato(p.SourceColumn.SqlScape(), p.ParameterName + post).Indent(2), ",\r\n"),
                            idParamName + post);

                        if (typeof(Entity).IsAssignableFrom(table.Type))
                            update += " AND ticks = {0}".Formato(oldTicksParamName + post);

                        if (!output)
                            return update;
                        else
                            return update + "\r\nIF @@ROWCOUNT = 0 INSERT INTO @NotFound (id) VALUES ({0})".Formato(idParamName + post);
                    };

                    List<Expression> parameters = trios.Select(a => (Expression)a.ParameterBuilder).ToList();

                    parameters.Add(pb.ParameterFactory(Trio.Concat(idParamName, paramPostfix), SqlBuilder.PrimaryKeyType, null, false, Expression.Field(paramIdent, fiId)));

                    if (typeof(Entity).IsAssignableFrom(table.Type))
                        parameters.Add(pb.ParameterFactory(Trio.Concat(oldTicksParamName, paramPostfix), SqlDbType.BigInt, null, false, paramOldTicks));

                    var expr = Expression.Lambda<Func<IdentifiableEntity, long, Forbidden, string, List<DbParameter>>>(
                        CreateBlock(parameters, assigments), paramIdent, paramOldTicks, paramForbidden, paramPostfix);

                    result.UpdateParameters = expr.Compile();

                    return result;
                }
            }

        }

        ResetLazy<UpdateCache> updater;

   
        class CollectionsCache
        {
            public Func<IdentifiableEntity, SqlPreCommand> InsertCollectionsSync;

            public Action<List<IdentifiableEntity>, Forbidden> InsertCollections;
            public Action<List<IdentifiableEntity>, Forbidden> UpdateCollections;

            internal static CollectionsCache InitializeCollections(Table table)
            {
                using (HeavyProfiler.LogNoStackTrace("InitializeCollections", () => table.Type.TypeName()))
                {
                    List<RelationalTable.IRelationalCache> caches =
                        (from ef in table.Fields.Values
                         where ef.Field is FieldMList
                         let rt = ((FieldMList)ef.Field).RelationalTable
                         select giCreateCache.GetInvoker(rt.Field.FieldType)(rt, ef.Getter)).ToList();

                    if (caches.IsEmpty())
                        return null;
                    else
                    {
                        return new CollectionsCache
                        {
                            InsertCollections = (idents, forbidden) =>
                            {
                                foreach (var rc in caches)
                                    rc.RelationalInserts(idents, forbidden);
                            },

                            UpdateCollections = (idents, forbidden) =>
                            {
                                foreach (var rc in caches)
                                    rc.RelationalUpdates(idents, forbidden);
                            },

                            InsertCollectionsSync = ident =>
                                caches.Select(rc => rc.RelationalUpdateSync(ident)).Combine(Spacing.Double)
                        };
                    }
                }
            }
        }

        ResetLazy<CollectionsCache> saveCollections;

        static GenericInvoker<Func<RelationalTable, Func<object, object>, RelationalTable.IRelationalCache>> giCreateCache =
            new GenericInvoker<Func<RelationalTable, Func<object, object>, RelationalTable.IRelationalCache>>(
            (RelationalTable rt, Func<object, object> d) => rt.CreateCache<int>(d));

        public SqlPreCommand InsertSqlSync(IdentifiableEntity ident, bool includeCollections = true, string comment = null)
        {
            bool dirty = false;
            ident.PreSaving(ref dirty);
            SetToStrField(ident);

            SqlPreCommandSimple insert = Identity ?
                new SqlPreCommandSimple(
                    inserterIdentity.Value.SqlInsertPattern("", false),
                    inserterIdentity.Value.InsertParameters(ident, new Forbidden(), "")).AddComment(comment) :
                new SqlPreCommandSimple(
                    inserterDisableIdentity.Value.SqlInsertPattern(""),
                    inserterDisableIdentity.Value.InsertParameters(ident, new Forbidden(), "")).AddComment(comment);

            if (!includeCollections)
                return insert;

            var cc = saveCollections.Value;
            if (cc == null)
                return insert;

            SqlPreCommand collections = cc.InsertCollectionsSync(ident);

            if (collections == null)
                return insert;

            SqlPreCommand setParent = new SqlPreCommandSimple("SET @idParent = @@Identity");

            return SqlPreCommand.Combine(Spacing.Simple, insert, setParent, collections);
        }



        public SqlPreCommand UpdateSqlSync(IdentifiableEntity ident, bool includeCollections = true, string comment = null)
        {
            bool dirty = false;
            ident.PreSaving(ref dirty);
            if (SetToStrField(ident))
                ident.SetSelfModified();

            if (ident.Modified == ModifiedState.Clean || ident.Modified == ModifiedState.Sealed)
                return null;

            var uc = updater.Value;
            SqlPreCommandSimple update = new SqlPreCommandSimple(uc.SqlUpdatePattern("", false),
                uc.UpdateParameters(ident, (ident as Entity).TryCS(a => a.Ticks) ?? -1, new Forbidden(), "")).AddComment(comment);

            if (!includeCollections)
                return update;

            var cc = saveCollections.Value;
            if (cc == null)
                return update;

            SqlPreCommand collections = cc.InsertCollectionsSync(ident);

            return SqlPreCommand.Combine(Spacing.Simple, update, collections);
        }

        public class Trio
        {
            public Trio(IColumn column, Expression value, Expression postfix)
            {
                this.SourceColumn = column.Name;
                this.ParameterName = Engine.ParameterBuilder.GetParameterName(column.Name);
                this.ParameterBuilder = Connector.Current.ParameterBuilder.ParameterFactory(Concat(this.ParameterName, postfix), column.SqlDbType, column.UdtTypeName, column.Nullable, value);
            }

            public string SourceColumn;
            public string ParameterName;
            public MemberInitExpression ParameterBuilder; //Expression<DbParameter>

            public override string ToString()
            {
                return "{0} {1} {2}".Formato(SourceColumn, ParameterName, ParameterBuilder.NiceToString());
            }

            static MethodInfo miConcat = ReflectionTools.GetMethodInfo(() => string.Concat("", ""));

            internal static Expression Concat(string baseName, Expression postfix)
            {
                return Expression.Call(null, miConcat, Expression.Constant(baseName), postfix);
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


    public partial class RelationalTable
    {
        internal interface IRelationalCache
        {
            SqlPreCommand RelationalUpdateSync(IdentifiableEntity parent);
            void RelationalInserts(List<IdentifiableEntity> idents, Forbidden forbidden);
            void RelationalUpdates(List<IdentifiableEntity> idents, Forbidden forbidden);
        }

        internal class RelationalCache<T> : IRelationalCache
        {
            public Func<string, string> sqlDelete;
            public Func<IdentifiableEntity, string, DbParameter> DeleteParameter;
            public ConcurrentDictionary<int, Action<List<IdentifiableEntity>>> deleteCache = new ConcurrentDictionary<int, Action<List<IdentifiableEntity>>>();

            Action<List<IdentifiableEntity>> GetDelete(int numElements)
            {
                return deleteCache.GetOrAdd(numElements, num =>
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

            public Func<string, string> sqlInsert;
            public Func<IdentifiableEntity, T, Forbidden, string, List<DbParameter>> InsertParameters;
            public ConcurrentDictionary<int, Action<List<MListPair<T>>, Forbidden>> insertCache = 
                new ConcurrentDictionary<int, Action<List<MListPair<T>>, Forbidden>>();

            Action<List<MListPair<T>>, Forbidden> GetInsert(int numElements)
            {
                return insertCache.GetOrAdd(numElements, num =>
                {
                    string sql = Enumerable.Range(0, num).ToString(i => sqlInsert(i.ToString()), ";\r\n");

                    return (list, forbidden) =>
                    {
                        List<DbParameter> parameters = new List<DbParameter>();
                        for (int i = 0; i < num; i++)
                        {
                            var pair = list[i];
                            parameters.AddRange(InsertParameters(pair.Entity, pair.Item, forbidden, i.ToString()));
                        }
                        new SqlPreCommandSimple(sql, parameters).ExecuteNonQuery();
                    };
                });
            }


            public Func<IdentifiableEntity, MList<T>> Getter;

            public void RelationalInserts(List<IdentifiableEntity> idents, Forbidden forbidden)
            {
                List<MListPair<T>> toInsert = new List<MListPair<T>>();

                foreach (var entity in idents)
                {
                    MList<T> collection = Getter(entity);

                    if (collection == null)
                        continue;

                    if (collection.Modified == ModifiedState.Clean)
                        continue;

                    foreach (var item in collection)
                        toInsert.Add(new MListPair<T>(entity, item));
                }

                toInsert.SplitStatements(list => GetInsert(list.Count)(list, forbidden));
            }

            public void RelationalUpdates(List<IdentifiableEntity> idents, Forbidden forbidden)
            {
                List<IdentifiableEntity> toDelete = new List<IdentifiableEntity>();
                List<MListPair<T>> toInsert = new List<MListPair<T>>();

                foreach (var entity in idents)
                {
                    MList<T> collection = Getter(entity);

                    if (collection == null)
                        toDelete.Add(entity);
                    else
                    {
                        if (collection.Modified == ModifiedState.Clean)
                            continue;

                        toDelete.Add(entity);

                        foreach (var item in collection)
                            toInsert.Add(new MListPair<T>(entity, item));
                    }
                }

                toDelete.SplitStatements(list => GetDelete(list.Count)(list));

                toInsert.SplitStatements(list => GetInsert(list.Count)(list, forbidden));
            }

            public SqlPreCommand RelationalUpdateSync(IdentifiableEntity parent)
            {
                MList<T> collection = Getter(parent);

                if (collection == null)
                    return null;

                if (collection.Modified == ModifiedState.Clean)
                    return null;

                var sqlIns = sqlInsert("");

                if (parent.IsNew)
                {
                    return collection.Select(e =>
                    {
                        var parameters = InsertParameters(parent, e, new Forbidden(new HashSet<IdentifiableEntity> { parent }), "");
                        parameters.RemoveAt(0);
                        return new SqlPreCommandSimple(sqlIns, parameters).AddComment(e.ToString());
                    }).Combine(Spacing.Simple);
                }
                else
                {
                    return SqlPreCommand.Combine(Spacing.Simple,
                        new SqlPreCommandSimple(sqlDelete(""), new List<DbParameter> { DeleteParameter(parent, "") }),
                        collection.Select(e => new SqlPreCommandSimple(sqlIns, InsertParameters(parent, e, new Forbidden(), "")).AddComment(e.ToString())).Combine(Spacing.Simple));
                }
            }
        }

        internal RelationalCache<T> CreateCache<T>(Func<object, object> getter)
        {
            RelationalCache<T> result = new RelationalCache<T>();

            result.Getter = ident => (MList<T>)getter(ident);

            result.sqlDelete = post => "DELETE {0} WHERE {1} = @{2}".Formato(Name, BackReference.Name.SqlScape(), BackReference.Name + post);

            var pb = Connector.Current.ParameterBuilder;
            result.DeleteParameter = (ident, post) => pb.CreateReferenceParameter(ParameterBuilder.GetParameterName(BackReference.Name + post), false, ident.Id);


            var trios = new List<Table.Trio>();
            var assigments = new List<Expression>();

            var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
            var paramItem = Expression.Parameter(typeof(T), "item");
            var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
            var paramPostfix = Expression.Parameter(typeof(string), "postfix");

            BackReference.CreateParameter(trios, assigments, paramIdent, paramForbidden, paramPostfix);
            Field.CreateParameter(trios, assigments, paramItem, paramForbidden, paramPostfix);

            result.sqlInsert = post => "INSERT {0} ({1})\r\n VALUES ({2})".Formato(Name,
                trios.ToString(p => p.SourceColumn.SqlScape(), ", "),
                trios.ToString(p => p.ParameterName + post, ", "));

            var expr = Expression.Lambda<Func<IdentifiableEntity, T, Forbidden, string, List<DbParameter>>>(
                Table.CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramItem, paramForbidden, paramPostfix);

            result.InsertParameters = expr.Compile();

            return result;
        }
        public struct MListPair<T>
        {
            public IdentifiableEntity Entity;
            public T Item;

            public MListPair(IdentifiableEntity ident, T item)
            {
                this.Entity = ident;
                this.Item = item;
            }
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
        protected internal virtual void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix) { }
    }

    public partial class FieldPrimaryKey
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix)
        {
            trios.Add(new Table.Trio(this, value, postfix));
        }
    }

    public partial class FieldValue
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix)
        {
            trios.Add(new Table.Trio(this, value, postfix));
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

        static int? GetIdForLite(Lite<IIdentifiable> lite, Forbidden forbidden)
        {
            if (lite == null)
                return null;

            if (lite.UntypedEntityOrNull == null)
                return lite.Id;

            if (forbidden.Contains(lite.UntypedEntityOrNull))
                return null;

            lite.RefreshId();

            return lite.Id;
        }

        static int? GetIdForLiteCleanEntity(Lite<IIdentifiable> lite, Forbidden forbidden)
        {
            if (lite == null)
                return null;

            if (lite.UntypedEntityOrNull == null)
                return lite.Id;

            if (forbidden.Contains(lite.UntypedEntityOrNull))
                return null;

            lite.RefreshId();
            lite.ClearEntity();

            return lite.Id;
        }

        static int? GetIdForEntity(IIdentifiable value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            IdentifiableEntity ie = (IdentifiableEntity)value;
            return forbidden.Contains(ie) ? (int?)null : ie.Id;
        }

        static MethodInfo miGetTypeForLite = ReflectionTools.GetMethodInfo(() => GetTypeForLite(null, new Forbidden()));
        static MethodInfo miGetTypeForEntity = ReflectionTools.GetMethodInfo(() => GetTypeForEntity(null, new Forbidden()));

        public static Expression GetTypeFactory(this IFieldReference fr, Expression value, Expression forbidden)
        {
            return Expression.Call(fr.IsLite ? miGetTypeForLite : miGetTypeForEntity, value, forbidden);
        }

        static Type GetTypeForLite(Lite<IIdentifiable> value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            Lite<IIdentifiable> l = (Lite<IIdentifiable>)value;
            return l.UntypedEntityOrNull == null ? l.EntityType :
                 forbidden.Contains(l.UntypedEntityOrNull) ? null :
                 l.EntityType;
        }

        static Type GetTypeForEntity(IIdentifiable value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            IdentifiableEntity ie = (IdentifiableEntity)value;
            return forbidden.Contains(ie) ? null : ie.GetType();
        }
    }

    public partial class FieldReference
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix)
        {
            trios.Add(new Table.Trio(this, this.GetIdFactory(value, forbidden), postfix));
        }
    }

    public partial class FieldEnum
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix)
        {
            trios.Add(new Table.Trio(this, Expression.Convert(value, Nullable ? typeof(int?) : typeof(int)), postfix));
        }
    }

    public partial class FieldMList
    {
    }

    public partial class FieldEmbedded
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix)
        {
            ParameterExpression embedded = Expression.Parameter(this.FieldType, "embedded");

            if (HasValue != null)
            {
                trios.Add(new Table.Trio(HasValue, Expression.NotEqual(value, Expression.Constant(null, FieldType)), postfix));

                assigments.Add(Expression.Assign(embedded, Expression.Convert(value, this.FieldType)));

                foreach (var ef in EmbeddedFields.Values)
                {
                    ef.Field.CreateParameter(trios, assigments,
                        Expression.Condition(
                            Expression.Equal(embedded, Expression.Constant(null, this.FieldType)),
                            Expression.Constant(null, ef.FieldInfo.FieldType.Nullify()),
                            Expression.Field(embedded, ef.FieldInfo).Nullify()), forbidden, postfix);
                }
            }
            else
            {

                assigments.Add(Expression.Assign(embedded, Expression.Convert(value.NodeType == ExpressionType.Conditional ? value : Expression.Call(Expression.Constant(this), miCheckNull, value), this.FieldType)));
                foreach (var ef in EmbeddedFields.Values)
                {
                    ef.Field.CreateParameter(trios, assigments,
                        Expression.Field(embedded, ef.FieldInfo), forbidden, postfix);
                }
            }
        }

        static MethodInfo miCheckNull = ReflectionTools.GetMethodInfo((FieldEmbedded fe) => fe.CheckNull(null));
        object CheckNull(object obj)
        {
            if (obj == null)
                throw new InvalidOperationException("Impossible to save 'null' on the not-nullable embedded field of type '{0}'".Formato(this.FieldType.Name));

            return obj;
        }
    }

    public partial class FieldMixin
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix)
        {
            ParameterExpression mixin = Expression.Parameter(this.FieldType, "mixin");

            assigments.Add(Expression.Assign(mixin, Expression.Call(value, MixinDeclarations.miMixin.MakeGenericMethod(this.FieldType))));
            foreach (var ef in Fields.Values)
            {
                ef.Field.CreateParameter(trios, assigments,
                    Expression.Field(mixin, ef.FieldInfo), forbidden, postfix);
            }
        }
    }

    public partial class FieldImplementedBy
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix)
        {
            ParameterExpression ibType = Expression.Parameter(typeof(Type), "ibType");
            ParameterExpression ibId = Expression.Parameter(typeof(int?), "ibId");

            assigments.Add(Expression.Assign(ibType, Expression.Call(Expression.Constant(this), miCheckType, this.GetTypeFactory(value, forbidden))));
            assigments.Add(Expression.Assign(ibId, this.GetIdFactory(value, forbidden)));

            var nullId = Expression.Constant(null, typeof(int?));

            foreach (var imp in ImplementationColumns)
            {
                trios.Add(new Table.Trio(imp.Value,
                    Expression.Condition(Expression.Equal(ibType, Expression.Constant(imp.Key)), ibId, Expression.Constant(null, typeof(int?))), postfix
                    ));
            }
        }

        static MethodInfo miCheckType = ReflectionTools.GetMethodInfo((FieldImplementedBy fe) => fe.CheckType(null));

        Type CheckType(Type type)
        {
            if (type != null && !ImplementationColumns.ContainsKey(type))
                throw new InvalidOperationException("Type {0} is not in the list of ImplementedBy:\r\n{1}".Formato(type.Name, ImplementationColumns.ToString(kvp => "{0} -> {1}".Formato(kvp.Key.Name, kvp.Value.Name), "\r\n")));

            return type;
        }
    }

    public partial class ImplementationColumn
    {

    }

    public partial class FieldImplementedByAll
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<Expression> assigments, Expression value, Expression forbidden, Expression postfix)
        {
            trios.Add(new Table.Trio(Column, this.GetIdFactory(value, forbidden), postfix));
            trios.Add(new Table.Trio(ColumnTypes, Expression.Call(Expression.Constant(this), miConvertType, this.GetTypeFactory(value, forbidden)), postfix));
        }

        static MethodInfo miConvertType = ReflectionTools.GetMethodInfo((FieldImplementedByAll fe) => fe.ConvertType(null));

        int? ConvertType(Type type)
        {
            if (type == null)
                return null;

            return Schema.Current.TypeToId.GetOrThrow(type, "{0} not registered in the schema");
        }
    }


}
