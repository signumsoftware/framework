using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using Signum.Engine.Properties;
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

namespace Signum.Engine.Maps
{
    public struct Forbidden
    {
        public Forbidden(IdentifiableEntity entity)
        {
            this.set = new HashSet<IdentifiableEntity> { entity }; 
        }

        public Forbidden(DirectedGraph<IdentifiableEntity> graph, IdentifiableEntity entity)
        {
            this.set = graph == null ? null : graph.TryRelatedTo(entity);
        }

        HashSet<IdentifiableEntity> set;

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
        class InsertCache
        {
            public Func<string, bool, string> SqlInsertPattern;
            public Func<IdentifiableEntity, Forbidden, string, List<DbParameter>> InsertParameters;
            public Action<IdentifiableEntity, DirectedGraph<IdentifiableEntity>> Insert;
            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> Insert2;
            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> Insert4;
            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> Insert8;
            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> Insert16;

            public List<DbParameter> InsertParametersMany(List<IdentifiableEntity> entities, DirectedGraph<IdentifiableEntity> graph)
            {
                List<DbParameter> result = new List<DbParameter>();
                int i = 0;
                foreach (var item in entities)
                    result.AddRange(InsertParameters(item, new Forbidden(graph, item), (i++).ToString()));
                return result;
            }
        }

        ResetLazy<InsertCache> inserter;

        InsertCache InitializeInsert()
        {
            InsertCache result = new InsertCache();

            var trios = new List<Table.Trio>();
            var assigments = new List<Expression>();
            var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
            var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
            var paramPostfix = Expression.Parameter(typeof(string), "postfix");

            var cast = Expression.Parameter(Type, "casted");
            assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, Type)));

            foreach (var item in Fields.Values.Where(a=>!Identity || !(a.Field is FieldPrimaryKey)))
            {
                item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramPostfix);
            }

            result.SqlInsertPattern = (post, output) =>
                "INSERT {0} ({1})\r\n{2} VALUES ({3})".Formato(Name,
                trios.ToString(p => p.SourceColumn.SqlScape(), ", "),
                output ? "OUTPUT INSERTED.Id into @MyTable \r\n" : null,
                trios.ToString(p => p.ParameterName + post, ", "));


            var expr = Expression.Lambda<Func<IdentifiableEntity, Forbidden, string, List<DbParameter>>>(
                CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramForbidden, paramPostfix);

            result.InsertParameters = expr.Compile();

            result.Insert = GetInsert(result);
            result.Insert2 = GetInsertMulti(2, result);
            result.Insert4 = GetInsertMulti(4, result);
            result.Insert8 = GetInsertMulti(8, result);
            result.Insert16 = GetInsertMulti(16, result);

            return result;
        }

        public IColumn ToStrColumn
        {
            get
            {
                EntityField entity;
                
                if(Fields.TryGetValue("toStr", out entity))
                    return (IColumn)entity.Field;

                return null;
            }
        }

        private Action<IdentifiableEntity, DirectedGraph<IdentifiableEntity>> GetInsert(InsertCache result)
        {
            if (Identity)
            {
                string sqlSingle = result.SqlInsertPattern("", false) + ";SELECT CONVERT(Int,@@Identity) AS [newID]";

                return (ident, graph) =>
                {
                    AssertNoId(ident);

                    Entity entity = ident as Entity;
                    if (entity != null)
                        entity.Ticks = TimeZoneManager.Now.Ticks;

                    SetToStrField(ident);

                    var forbidden = new Forbidden(graph, ident);

                    ident.id = (int)new SqlPreCommandSimple(sqlSingle, result.InsertParameters(ident, forbidden, "")).ExecuteScalar();

                    ident.IsNew = false;
                    FinishInsert(ident, forbidden);
                };
            }
            else
            {
                string sqlSingle = result.SqlInsertPattern("", false);

                return (ident, graph) =>
                {
                    AssertHasId(ident);

                    Entity entity = ident as Entity;
                    if (entity != null)
                        entity.Ticks = TimeZoneManager.Now.Ticks;

                    SetToStrField(ident);

                    var forbidden = new Forbidden(graph, ident);

                    new SqlPreCommandSimple(sqlSingle, result.InsertParameters(ident, forbidden, "")).ExecuteNonQuery();

                    ident.IsNew = false;
                    FinishInsert(ident, forbidden);
                };
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

        private Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetInsertMulti(int num, InsertCache result)
        {
            if (Identity)
            {
                string sqlMulti = new StringBuilder()
                    .AppendLine("DECLARE @MyTable TABLE (Id INT);")
                    .AppendLines(Enumerable.Range(0, num).Select(i=>result.SqlInsertPattern(i.ToString(), true)))
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

                        SetToStrField(ident);
                    }

                    DataTable table = new SqlPreCommandSimple(sqlMulti, result.InsertParametersMany(idents, graph)).ExecuteDataTable();

                    for (int i = 0; i < num; i++)
                    {
                        var forbidden = new Forbidden(graph, idents[i]);
                        idents[i].id = (int)table.Rows[i][0];
                        FinishInsert(idents[i], forbidden);
                    }
                };

            }
            else
            {
                string sqlMulti = Enumerable.Range(0, num).ToString(i => result.SqlInsertPattern(i.ToString(), false), ";\r\n");

                return (idents, graph) =>
                {
                    for (int i = 0; i < num; i++)
                    {
                        var ident = idents[i];
                        AssertHasId(ident);
                        Entity entity = ident as Entity;
                        if (entity != null)
                            entity.Ticks = TimeZoneManager.Now.Ticks;

                        SetToStrField(ident);
                    }

                    new SqlPreCommandSimple(sqlMulti, result.InsertParametersMany(idents, graph)).ExecuteNonQuery();
                    for (int i = 0; i < num; i++)
                    {
                        FinishInsert(idents[i], new Forbidden(graph, idents[i]));
                    }
                };

            }
        }

        private void AssertHasId(IdentifiableEntity ident)
        {
            if (ident.IdOrNull == null)
                throw new InvalidOperationException("{0} should have an Id, since the table has no Identity".Formato(ident, ident.IdOrNull));
        }

        private void AssertNoId(IdentifiableEntity ident)
        {
            if (ident.IdOrNull != null)
                throw new InvalidOperationException("{0} is new, but has Id {1}".Formato(ident, ident.IdOrNull));
        }

        static FieldInfo fiId = ReflectionTools.GetFieldInfo((IdentifiableEntity i) => i.id);
        static FieldInfo fiTicks = ReflectionTools.GetFieldInfo((Entity i) => i.ticks);

        class UpdateCache
        {
            public Func<string, bool, string> SqlUpdatePattern;
            public Func<IdentifiableEntity, long, Forbidden, string, List<DbParameter>> UpdateParameters;
            public Action<IdentifiableEntity, DirectedGraph<IdentifiableEntity>> Update;
            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> Update2;
            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> Update4;
            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> Update8;
            public Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> Update16; 
        }

        ResetLazy<UpdateCache> updater;

        UpdateCache InitializeUpdate()
        {
            UpdateCache result = new UpdateCache();

            var trios = new List<Trio>();
            var assigments = new List<Expression>();
            var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
            var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
            var paramOldTicks = Expression.Parameter(typeof(long), "oldTicks");
            var paramPostfix = Expression.Parameter(typeof(string), "postfix");

            var cast = Expression.Parameter(Type);
            assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, Type)));

            foreach (var item in Fields.Values.Where(a =>!(a.Field is FieldPrimaryKey)))
            {
                item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden, paramPostfix);
            }

            var pb = Connector.Current.ParameterBuilder;

            string idParamName = ParameterBuilder.GetParameterName("id");

            string oldTicksParamName = ParameterBuilder.GetParameterName("old_ticks");

            result.SqlUpdatePattern = (post, output) =>
            {
                string update = "UPDATE {0} SET \r\n{1}\r\n WHERE id = {2}".Formato(
                    Name,
                    trios.ToString(p => "{0} = {1}".Formato(p.SourceColumn.SqlScape(), p.ParameterName + post).Indent(2), ",\r\n"),
                    idParamName + post);

                if (typeof(Entity).IsAssignableFrom(this.Type))
                    update += " AND ticks = {0}".Formato(oldTicksParamName + post); 

                if (!output)
                    return update;
                else
                    return update + "\r\nIF @@ROWCOUNT = 0 INSERT INTO @NotFound (id) VALUES ({0})".Formato(idParamName + post);
            };

            List<Expression> parameters = trios.Select(a => (Expression)a.ParameterBuilder).ToList();

            parameters.Add(pb.ParameterFactory(Trio.Concat(idParamName, paramPostfix), SqlBuilder.PrimaryKeyType, null, false, Expression.Field(paramIdent, fiId)));

            if (typeof(Entity).IsAssignableFrom(this.Type))
                parameters.Add(pb.ParameterFactory(Trio.Concat(oldTicksParamName, paramPostfix), SqlDbType.BigInt, null, false, paramOldTicks));

            var expr = Expression.Lambda<Func<IdentifiableEntity, long, Forbidden, string, List<DbParameter>>>(
                CreateBlock(parameters, assigments), paramIdent, paramOldTicks, paramForbidden, paramPostfix);

            result.UpdateParameters = expr.Compile();

            result.Update = GetUpdate(result);
            result.Update2 = GetUpdateMultiple(result, 2);
            result.Update4 = GetUpdateMultiple(result, 4);
            result.Update8 = GetUpdateMultiple(result, 8);
            result.Update16 = GetUpdateMultiple(result, 16);

            return result;
        }


        private Action<IdentifiableEntity, DirectedGraph<IdentifiableEntity>> GetUpdate(UpdateCache result)
        {
            string sqlUpdate = result.SqlUpdatePattern("", false);

            if (typeof(Entity).IsAssignableFrom(this.Type))
            {
                return (ident, graph) =>
                {
                    Entity entity = (Entity)ident;

                    long oldTicks = entity.Ticks;
                    entity.Ticks = TimeZoneManager.Now.Ticks;

                    SetToStrField(ident);

                    var forbidden = new Forbidden(graph, ident);

                    int num = (int)new SqlPreCommandSimple(sqlUpdate, result.UpdateParameters(ident, oldTicks, forbidden, "")).ExecuteNonQuery();
                    if (num != 1)
                        throw new ConcurrencyException(ident.GetType(), ident.Id);

                    FinishUpdate(ident, forbidden); 
                };
            }
            else
            {
                return (ident, graph) =>
                {
                    SetToStrField(ident);

                    var forbidden = new Forbidden(graph, ident);

                    int num = (int)new SqlPreCommandSimple(sqlUpdate, result.UpdateParameters(ident, -1, forbidden, "")).ExecuteNonQuery();
                    if (num != 1)
                        throw new EntityNotFoundException(ident.GetType(), ident.Id);

                    FinishUpdate(ident, forbidden); 
                };
            }
        }

        private Action<List<IdentifiableEntity>, DirectedGraph<IdentifiableEntity>> GetUpdateMultiple(UpdateCache result, int num)
        {
              string sqlMulti = new StringBuilder()
                    .AppendLine("DECLARE @NotFound TABLE (Id INT);")
                    .AppendLines(Enumerable.Range(0, num).Select(i=>result.SqlUpdatePattern(i.ToString(), true)))
                    .AppendLine("SELECT Id from @NotFound").ToString(); 

            if (typeof(Entity).IsAssignableFrom(this.Type))
            {
                return (idents, graph) =>
                {
                    List<DbParameter> parameters = new List<DbParameter>();
                    for (int i = 0; i < num; i++)
			        {    
                        Entity entity = (Entity)idents[i];

                        long oldTicks = entity.Ticks;
                        entity.Ticks = TimeZoneManager.Now.Ticks;

                        parameters.AddRange(result.UpdateParameters(entity, oldTicks, new Forbidden(graph, entity), i.ToString()));
			        }

                    DataTable table = new SqlPreCommandSimple(sqlMulti, parameters).ExecuteDataTable();

                    if (table.Rows.Count > 0)
                        throw new ConcurrencyException(Type, table.Rows.Cast<DataRow>().Select(r=>(int)r[0]).ToArray());

                    for (int i = 0; i < num; i++)
                    {
                        FinishUpdate(idents[i], new Forbidden(graph, idents[i])); 
                    }
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
                        parameters.AddRange(result.UpdateParameters(ident, -1, new Forbidden(graph, ident), i.ToString()));
			        }
  
                    DataTable table = new SqlPreCommandSimple(sqlMulti, parameters).ExecuteDataTable();

                    if (table.Rows.Count > 0)
                        throw new EntityNotFoundException(Type, table.Rows.Cast<DataRow>().Select(r => (int)r[0]).ToArray());

                    for (int i = 0; i < num; i++)
                    {
                        FinishUpdate(idents[i], new Forbidden(graph, idents[i]));
                    }
                };
            }
        }

        class CollectionsCache
        {
            public Func<IdentifiableEntity, SqlPreCommand> SaveCollectionsSync; 
            public Action<IdentifiableEntity, Forbidden, bool> SaveCollections;
        }

        ResetLazy<CollectionsCache> saveCollections;

        static GenericInvoker<Func<RelationalTable, RelationalTable.IInsertCache>> giCreateCache = new GenericInvoker<Func<RelationalTable, RelationalTable.IInsertCache>>(
            (RelationalTable rt)=>rt.CreateCache<int>());

        CollectionsCache InitializeCollections()
        {
            var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
            var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
            var paramIsNew = Expression.Parameter(typeof(bool), "isNew");

            var entity = Expression.Parameter(Type);

            var castEntity = Expression.Assign(entity, Expression.Convert(paramIdent, Type));

            var list = (from ef in Fields.Values
                        where ef.Field is FieldMList
                        let rt = ((FieldMList)ef.Field).RelationalTable
                        let cache = giCreateCache.GetInvoker(rt.Field.FieldType)(rt)
                        select new
                        {
                            saveCollection = (Expression)Expression.Call(Expression.Constant(cache),
                               cache.GetType().GetMethod("RelationalInserts", BindingFlags.NonPublic | BindingFlags.Instance),
                               Expression.Field(entity, ef.FieldInfo), paramIdent, paramIsNew, paramForbidden),

                            ef.Getter,
                            cache
                        }).ToList();

            if (list.IsEmpty())
                return null;
            else
            {
                var miniList = list.Select(a => new { a.Getter, a.cache }).ToList();
                return new CollectionsCache
                {
                    SaveCollections = Expression.Lambda<Action<IdentifiableEntity, Forbidden, bool>>(Expression.Block(new[] { entity },
                                list.Select(a => a.saveCollection).PreAnd(castEntity)), paramIdent, paramForbidden, paramIsNew).Compile(),

                    SaveCollectionsSync = ident => miniList.Select(a => a.cache.RelationalInsertsSync((Modifiable)a.Getter(ident), ident)).Combine(Spacing.Double)
                }; 
            }
        }

        internal void FinishInsert(IdentifiableEntity ident, Forbidden forbidden)
        {
            ident.IsNew = false;

            if (forbidden.IsEmpty)
                ident.Modified = null;

            if (saveCollections.Value != null)
                saveCollections.Value.SaveCollections(ident, forbidden, true);
        }

        internal void FinishUpdate(IdentifiableEntity ident, Forbidden forbidden)
        {
            if (forbidden.IsEmpty)
                ident.Modified = null;

            if (saveCollections.Value != null)
                saveCollections.Value.SaveCollections(ident, forbidden, false);
        }

        public SqlPreCommand InsertSqlSync(IdentifiableEntity ident, string comment = null)
        {
            bool dirty = false;
            ident.PreSaving(ref dirty);
            SetToStrField(ident);

            var ic = inserter.Value;
            SqlPreCommandSimple insert = new SqlPreCommandSimple(ic.SqlInsertPattern("", false), ic.InsertParameters(ident, new Forbidden(), "")).AddComment(comment);
            
            var cc = saveCollections.Value;
            if(cc == null)
                return insert;

            SqlPreCommand collections = cc.SaveCollectionsSync(ident);

            if (collections == null)
                return insert;

            SqlPreCommand setParent = new SqlPreCommandSimple("SET @idParent = @@Identity"); 

            return SqlPreCommand.Combine(Spacing.Simple, insert, setParent, collections); 
        }

        

        public SqlPreCommand UpdateSqlSync(IdentifiableEntity ident, string comment = null)
        {   
            bool dirty = false;
            ident.PreSaving(ref dirty);
            if (SetToStrField(ident)) 
                ident.SetSelfModified();

            if (!ident.SelfModified)
                return null;

            var uc = updater.Value;
            SqlPreCommandSimple update = new SqlPreCommandSimple(uc.SqlUpdatePattern("", false),
                uc.UpdateParameters(ident, (ident as Entity).TryCS(a => a.Ticks) ?? -1, new Forbidden(), "")).AddComment(comment);

            var cc = saveCollections.Value;
            if (cc == null)
                return update;

            SqlPreCommand collections = cc.SaveCollectionsSync(ident);

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
        internal interface IInsertCache
        {
            SqlPreCommand RelationalInsertsSync(Modifiable mlist, IdentifiableEntity parent);
        }

        internal class InsertCache<T> : IInsertCache
        {
            public string sqlDelete;
            public Func<string, string> sqlInsert;
            public Func<IdentifiableEntity, T, Forbidden, string, List<DbParameter>> InsertParameters;
            public Func<IdentifiableEntity, DbParameter> DeleteParameter;

            public Action<List<T>, IdentifiableEntity, Forbidden> Insert1;
            public Action<List<T>, IdentifiableEntity, Forbidden> Insert2;
            public Action<List<T>, IdentifiableEntity, Forbidden> Insert4;
            public Action<List<T>, IdentifiableEntity, Forbidden> Insert8;
            public Action<List<T>, IdentifiableEntity, Forbidden> Insert16;

            internal void RelationalInserts(MList<T> collection, IdentifiableEntity ident, bool newEntity, Forbidden forbidden)
            {
                if (collection == null)
                {
                    if (!newEntity)
                        new SqlPreCommandSimple(sqlDelete, new List<DbParameter> { DeleteParameter(ident) }).ExecuteNonQuery();
                }
                else
                {
                    if (collection.Modified == false)
                        return;

                    if (forbidden.IsEmpty)
                        collection.Modified = null;

                    if (!newEntity)
                        new SqlPreCommandSimple(sqlDelete, new List<DbParameter> { DeleteParameter(ident) }).ExecuteNonQuery();

                    if (!Connector.Current.AllowsMultipleQueries)
                    {
                        List<T> uniList = new List<T>() { default(T) };
                        foreach (var item in collection)
                        {
                            uniList[0] = item;
                            Insert1(uniList, ident, forbidden);
                        }
                    }
                    else
                    {
                        foreach (var list in collection.Split_1_2_4_8_16())
                        {
                            switch (list.Count)
                            {
                                case 1: Insert1(list, ident, forbidden); break;
                                case 2: Insert2(list, ident, forbidden); break;
                                case 4: Insert4(list, ident, forbidden); break;
                                case 8: Insert8(list, ident, forbidden); break;
                                case 16: Insert16(list, ident, forbidden); break;
                                default: throw new InvalidOperationException("Unexpected list.Count {0}".Formato(list.Count));
                            }
                        }
                    }
                }
            }

            public SqlPreCommand RelationalInsertsSync(Modifiable mlist, IdentifiableEntity parent)
            {
                var list = (MList<T>)mlist;

                if (list.Modified == false)
                    return null;

                var sqlIns = sqlInsert(""); 

                if (parent.IsNew)
                {
                    return list.Select(e =>
                    {
                        var parameters = InsertParameters(parent, e, new Forbidden(parent), "");
                        parameters.RemoveAt(0);
                        return new SqlPreCommandSimple(sqlIns, parameters).AddComment(e.ToString());
                    }).Combine(Spacing.Simple);
                }
                else
                {
                    return SqlPreCommand.Combine(Spacing.Simple,
                        new SqlPreCommandSimple(sqlDelete, new List<DbParameter> { DeleteParameter(parent) }),
                        list.Select(e => new SqlPreCommandSimple(sqlIns, InsertParameters(parent, e, new Forbidden(), "")).AddComment(e.ToString())).Combine(Spacing.Simple)); 
                }
            }
        }

        internal InsertCache<T> CreateCache<T>()
        {
            InsertCache<T> result = new InsertCache<T>();

            result.sqlDelete = "DELETE {0} WHERE {1} = @{1}".Formato(Name, BackReference.Name);
            result.DeleteParameter = ident => Connector.Current.ParameterBuilder.CreateReferenceParameter(ParameterBuilder.GetParameterName(BackReference.Name), false, ident.Id);

            var trios = new List<Table.Trio>();
            var assigments = new List<Expression>();

            var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
            var paramItem = Expression.Parameter(typeof(T), "item");
            var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
            var paramPostfix = Expression.Parameter(typeof(string), "postfix");

            BackReference.CreateParameter(trios, assigments, paramIdent, paramForbidden, paramPostfix);
            Field.CreateParameter(trios, assigments, paramItem, paramForbidden, paramPostfix);

            result.sqlInsert = post=> "INSERT {0} ({1})\r\n VALUES ({2})".Formato(Name,
                trios.ToString(p => p.SourceColumn.SqlScape(), ", "),
                trios.ToString(p => p.ParameterName + post, ", "));

            var expr = Expression.Lambda<Func<IdentifiableEntity, T, Forbidden, string, List<DbParameter>>>(
                Table.CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramItem, paramForbidden, paramPostfix);

            result.InsertParameters = expr.Compile();

            result.Insert1 = GetInsert(result, 1);
            result.Insert2 = GetInsert(result, 2);
            result.Insert4 = GetInsert(result, 4);
            result.Insert8 = GetInsert(result, 8);
            result.Insert16 = GetInsert(result, 16);

            return result;
        }

        private Action<List<T>, IdentifiableEntity, Forbidden> GetInsert<T>(InsertCache<T> result, int num)
        {
            string sql = Enumerable.Range(0, num).ToString(i => result.sqlInsert(i.ToString()), ";\r\n");

            return (list, ident, forbidden) =>
            {
                List<DbParameter> parameters = new List<DbParameter>();
                for (int i = 0; i < num; i++)
                {
                    parameters.AddRange(result.InsertParameters(ident, list[i], forbidden, i.ToString()));
                }
                new SqlPreCommandSimple(sql, parameters).ExecuteNonQuery();
            };
        }
    }

    internal static class SaveUtils
    {
        public static IEnumerable<List<T>> Split_1_2_4_8_16<T>(this IList<T> list)
        { 
            List<T> result = new List<T>(16);
            int i = 0;
            for (; i <= list.Count - 16; i += 16)
                yield return Fill(result, list, i, 16);

            for (; i <= list.Count - 8; i += 8)
                yield return Fill(result, list, i, 8);

            for (; i <= list.Count - 4; i += 4)
                yield return Fill(result, list, i, 4);

            for (; i <= list.Count - 2; i += 2)
                yield return Fill(result, list, i, 2);

            for (; i <= list.Count - 1; i += 1)
                yield return Fill(result, list, i, 1);
        }

        public static List<T> Fill<T>(List<T> holder, IList<T> source, int pos, int count)
        {
            holder.Clear();
            int max = pos + count;
            for (int i = pos; i < max; i++)
                holder.Add(source[i]);
            return holder;
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

            Expression notModifiedCondition = Expression.Property(forbidden, "IsEmpty"); 

            if (HasValue != null)
            {
                trios.Add(new Table.Trio(HasValue, Expression.NotEqual(value, Expression.Constant(null, FieldType)), postfix));

                assigments.Add(Expression.Assign(embedded, Expression.Convert(value, this.FieldType)));
             
                notModifiedCondition = Expression.And(Expression.NotEqual(embedded, Expression.Constant(null, this.FieldType)), notModifiedCondition); 

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
  
                assigments.Add(Expression.Assign(embedded, Expression.Convert(value.NodeType == ExpressionType.Conditional? value: Expression.Call(Expression.Constant(this), miCheckNull, value), this.FieldType)));
                assigments.Add(Expression.IfThen(Expression.Property(forbidden, "IsEmpty"),
                    Expression.Assign(Expression.Property(embedded, "Modified"), Expression.Constant(null, typeof(bool?)))));
                foreach (var ef in EmbeddedFields.Values)
                {
                    ef.Field.CreateParameter(trios, assigments,
                        Expression.Field(embedded, ef.FieldInfo), forbidden, postfix);
                }
            }

            assigments.Add(Expression.IfThen(notModifiedCondition,
                 Expression.Assign(Expression.Property(embedded, "Modified"), Expression.Constant(null, typeof(bool?)))));
        }

        static MethodInfo miCheckNull = ReflectionTools.GetMethodInfo((FieldEmbedded fe) => fe.CheckNull(null));
        object CheckNull(object obj)
        {
            if(obj == null)
                throw new InvalidOperationException("Impossible to save 'null' on the not-nullable embedded field of type '{0}'".Formato(this.FieldType.Name));

            return obj;
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
            if(type != null && !ImplementationColumns.ContainsKey(type))
                throw new InvalidOperationException("Type {0} is not in the list of ImplementedBy:\r\n{1}".Formato(type.Name, ImplementationColumns.ToString(kvp =>"{0} -> {1}".Formato(kvp.Key.Name, kvp.Value.Name), "\r\n")));

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
