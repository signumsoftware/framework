using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Collections.Concurrent;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.Engine.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Cache;
using System.Data.SqlClient;
using Signum.Engine.Basics;

namespace Signum.Engine.Cache
{
    abstract class CachedTableBase 
    {
        protected class ConstructContext
        {
            public ParameterExpression origin;
            public ParameterExpression originObject;
            public ParameterExpression retriever;

            public AliasGenerator aliasGenerator;
            public Alias currentAlias;
            public ITable table;
            public Type tupleType;

            public string remainingJoins;

            public ConstructContext(ITable table, AliasGenerator aliasGenerator)
            {
                this.table = table;
                this.aliasGenerator = aliasGenerator;
                this.currentAlias = aliasGenerator.NextTableAlias(table.Name.Name);

                this.tupleType = TupleReflection.TupleChainType(table.Columns.Values.Select(GetColumnType));

                this.retriever = Expression.Parameter(typeof(IRetriever), "retriever");
                this.originObject = Expression.Parameter(typeof(object), "origin");
                this.origin = Expression.Parameter(tupleType, "origin");
            }

            public Expression GetTupleProperty(IColumn column)
            {
                return TupleReflection.TupleChainProperty(origin, table.Columns.Values.IndexOf(column));
            }

            internal string CreatePartialInnerJoin(IColumn column)
            {
                return "INNER JOIN {0} {1} ON {1}.{2}=".Formato(table.Name, currentAlias.Name.SqlScape(), column.Name);
            }

            internal Type GetColumnType(IColumn column)
            {
                if (column is FieldValue)
                    return ((Field)column).FieldType;

                if (column is FieldEmbedded.EmbeddedHasValueColumn)
                    return typeof(bool);

                return column.Nullable ? typeof(int?) : typeof(int);
            }

            internal Func<FieldReader, object> GetRowReader()
            {
                ParameterExpression reader = Expression.Parameter(typeof(FieldReader));

                var tupleConstructor = TupleReflection.TupleChainConstructor(
                    table.Columns.Values.Select((c, i) => FieldReader.GetExpression(reader, i, GetColumnType(c)))
                    );

                return Expression.Lambda<Func<FieldReader, object>>(tupleConstructor, reader).Compile();
            }

            internal Func<object, T> GetGetter<T>(IColumn column)
            {
                return Expression.Lambda<Func<object, T>>(
                    TupleReflection.TupleChainProperty(Expression.Convert(originObject, tupleType), table.Columns.Values.IndexOf(column)), originObject).Compile();
            }
        }

        Dictionary<IColumn, CachedTableBase> subTables;
        Dictionary<FieldMList, CachedTableBase> collections;

        protected static Action<Modifiable> resetModifiedAction;

        static CachedTableBase()
        {
            Expression param = Expression.Parameter(typeof(Modifiable));
            resetModifiedAction = Expression.Lambda<Action<Modifiable>>(Expression.Assign(Expression.Property(param, piModified), Expression.Constant(null, typeof(bool?)))).Compile();
        }

        static PropertyInfo piModified = ReflectionTools.GetPropertyInfo((Modifiable me) => me.Modified);
        static MemberBinding resetModified = Expression.Bind(piModified, Expression.Constant(null, typeof(bool?)));

        static GenericInvoker<Func<AliasGenerator, string, string, CachedTableBase>> ciCachedTable =
            new GenericInvoker<Func<AliasGenerator, string, string, CachedTableBase>>((aliasGenerator, lastPartialJoin, remainingJoins) =>
                new CachedTable<IdentifiableEntity>(aliasGenerator, lastPartialJoin, remainingJoins));

        static GenericInvoker<Func<AliasGenerator, string, string, CachedTableBase>> ciCachedSemiTable =
          new GenericInvoker<Func<AliasGenerator, string, string, CachedTableBase>>((aliasGenerator, lastPartialJoin, remainingJoins) =>
              new CachedSemiLite<IdentifiableEntity>(aliasGenerator, lastPartialJoin, remainingJoins));

        static GenericInvoker<Func<RelationalTable, AliasGenerator, string, string, CachedTableBase>> ciCachedRelationalTable =
          new GenericInvoker<Func<RelationalTable, AliasGenerator, string, string, CachedTableBase>>((relationalTable, aliasGenerator, lastPartialJoin, remainingJoins) =>
              new CachedRelationalTable<IdentifiableEntity>(relationalTable, aliasGenerator, lastPartialJoin, remainingJoins));

        static Expression NullId = Expression.Constant(null, typeof(int?));

        public CacheLogic.ICacheLogicController controller;

        protected void OnChange(SqlNotificationEventArgs args)
        {
            controller.Invalidate(isClean: false);
        }

        protected Expression Materialize( Field field, ConstructContext ctx)
        {
            if (field is FieldValue)
                return ctx.GetTupleProperty((IColumn)field);

            if (field is FieldEnum)
                return Expression.Convert(ctx.GetTupleProperty((IColumn)field), field.FieldType);

            if (field is IFieldReference)
            {
                var nullRef = Expression.Constant(null, field.FieldType);
                bool isLite = ((IFieldReference)field).IsLite;

                if (field is FieldReference)
                {
                    IColumn column = (IColumn)field;

                    return GetEntity(isLite, column, field.FieldType, ctx);
                }

                if (field is FieldImplementedBy)
                {
                    var ib = (FieldImplementedBy)field;

                    var call = ib.ImplementationColumns.Aggregate((Expression)nullRef, (acum, kvp) => 
                    {
                        IColumn column = (IColumn)kvp.Value;

                        Expression entity = GetEntity(isLite, column, kvp.Key, ctx);

                        return Expression.Condition(Expression.NotEqual(ctx.GetTupleProperty(column), NullId), entity, acum); 
                    });

                    return call;
                }

                if (field is FieldImplementedByAll)
                {
                    var iba = (FieldImplementedByAll)field;

                    Expression id = ctx.GetTupleProperty(iba.Column);
                    Expression typeId = ctx.GetTupleProperty(iba.ColumnTypes);

                    if (isLite)
                    {
                        var liteCreate = Expression.Call(miLiteCreate, SchemaGetType(typeId), Expression.Convert(id, typeof(int)), Expression.Constant(null, typeof(string)));
                        var liteRequest = Expression.Call(ctx.retriever, miRequestLite.MakeGenericMethod(Lite.Extract(field.FieldType)), liteCreate);

                        return Expression.Condition(Expression.NotEqual(id, NullId), liteRequest, nullRef);
                    }
                    else
                    {
                        return Expression.Call(ctx.retriever, miRequestIBA.MakeGenericMethod(Lite.Extract(field.FieldType)), id, typeId);
                    }
                }
            }

            if (field is FieldEmbedded)
            {
                var nullref = Expression.Constant(null, field.FieldType);

                var fe = (FieldEmbedded)field;

                Expression ctor = Expression.MemberInit(Expression.New(fe.FieldType),
                    fe.EmbeddedFields.Values.Select(f => Expression.Bind(f.FieldInfo, Materialize(f.Field, ctx)))
                    .And(resetModified));

                return Expression.Condition(Expression.Equal(ctx.origin, nullref), nullref, ctor);
            }

            if (field is FieldMList)
            {
                var mListField = (FieldMList)field;

                var idColumn = ctx.table.Columns.OfType<FieldPrimaryKey>().First();

                string lastPartialJoin = ctx.CreatePartialInnerJoin(idColumn);

                Type elementType = field.FieldType.ElementType();

                CachedTableBase ctb = ciCachedRelationalTable.GetInvoker(elementType)(mListField.RelationalTable, ctx.aliasGenerator, lastPartialJoin, ctx.remainingJoins);

                if (collections == null)
                    collections = new Dictionary<FieldMList, CachedTableBase>();

                collections.Add(mListField, ctb);

                return Expression.Call(Expression.Constant(ctb), miGetMListTable.MakeGenericMethod(elementType), ctx.GetTupleProperty(idColumn));
            }

            throw new InvalidOperationException("Unexpected {0}".Formato(field.GetType().Name));
        }

        private Expression GetEntity(bool isLite, IColumn column, Type type, ConstructContext ctx)
        {
            Expression id = ctx.GetTupleProperty(column);

            if (isLite)
            {
                if (CacheLogic.IsCached(type))
                    return Expression.Call(ctx.retriever, miRequestLite.MakeGenericMethod(type), id);

                string lastPartialJoin = ctx.CreatePartialInnerJoin(column);

                CachedTableBase ctb = ciCachedSemiTable.GetInvoker(type)(ctx.aliasGenerator, lastPartialJoin, ctx.remainingJoins);

                if (subTables == null)
                    subTables = new Dictionary<IColumn, CachedTableBase>();

                subTables.Add(column, ctb);

                return Expression.Condition(Expression.Equal(id, NullId), Expression.Constant(null, Lite.Generate(type)),
                    Expression.Call(Expression.Constant(ctb), miGetLiteTable.MakeGenericMethod(type), id));
            }
            else
            {
                if (CacheLogic.IsCached(type))
                    return Expression.Call(ctx.retriever, miRequest.MakeGenericMethod(type), id);

                string lastPartialJoin = ctx.CreatePartialInnerJoin(column);

                CachedTableBase ctb = ciCachedTable.GetInvoker(type)(ctx.aliasGenerator, lastPartialJoin, ctx.remainingJoins);

                if (subTables == null)
                    subTables = new Dictionary<IColumn, CachedTableBase>();

                subTables.Add(column, ctb);

                var entity = Expression.Parameter(type);
                LambdaExpression lambda = Expression.Lambda(typeof(Action<>).MakeGenericType(type),
                    Expression.Call(Expression.Constant(ctb), miCompleteTable.MakeGenericMethod(type), entity, ctx.retriever),
                    entity);

                return Expression.Call(ctx.retriever, miComplete.MakeGenericMethod(type), id, lambda);
            }
        }


        

        static MethodInfo miRequestLite = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestLite<IdentifiableEntity>(null)).GetGenericMethodDefinition();
        static MethodInfo miRequestIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestIBA<IdentifiableEntity>(null, null)).GetGenericMethodDefinition();
        static MethodInfo miRequest = ReflectionTools.GetMethodInfo((IRetriever r) => r.Request<IdentifiableEntity>(null)).GetGenericMethodDefinition();
        static MethodInfo miComplete = ReflectionTools.GetMethodInfo((IRetriever r) => r.Complete<IdentifiableEntity>(0, null)).GetGenericMethodDefinition();

        static MethodInfo miCompleteTable = ReflectionTools.GetMethodInfo((CachedTable<IdentifiableEntity> ct) => ct.Complete(null, null)).GetGenericMethodDefinition();
        static MethodInfo miGetLiteTable = ReflectionTools.GetMethodInfo((CachedSemiLite<IdentifiableEntity> ct) => ct.GetLite(1)).GetGenericMethodDefinition();
        static MethodInfo miGetMListTable = ReflectionTools.GetMethodInfo((CachedRelationalTable<IdentifiableEntity> ct) => ct.GetMList(1, null)).GetGenericMethodDefinition();


        static MethodInfo miLiteCreate = ReflectionTools.GetMethodInfo(() => Lite.Create(null, 0, null));

        protected static ConstructorInfo ciKVPIntString = ReflectionTools.GetConstuctorInfo(() => new KeyValuePair<int, string>(1, ""));

        static MethodInfo miGetType = ReflectionTools.GetMethodInfo((Schema s) => s.GetType(1));
        private MethodCallExpression SchemaGetType(Expression idtype)
        {
            return Expression.Call(Expression.Constant(Schema.Current), miGetType, idtype);
        }
    }

    class CachedTable<T> : CachedTableBase where T : IdentifiableEntity
    {
        ResetLazy<Dictionary<int, object>> rows;

        Func<FieldReader, object> rowReader;
        SqlPreCommandSimple query;
        Action<object, IRetriever, T> completer;
        Func<object, int> idGetter;
        Func<object, string> toStrGetter;

        public CachedTable(AliasGenerator aliasGenerator, string lastPartialJoin, string remainingJoins)
        {
            Table table = Schema.Current.Table(typeof(T));

            ConstructContext ctx = new ConstructContext(table, aliasGenerator);

            //Query
            {
                string select = "SELECT\r\n{0}\r\nFROM {1} {2}\r\n".Formato(
                    ctx.table.Columns.Values.ToString(c => ctx.currentAlias.Name.SqlScape() + "." + c.Name.SqlScape(), ",\r\n"),
                    table.Name,
                    ctx.currentAlias.Name.SqlScape());

                ctx.remainingJoins = lastPartialJoin == null ? null : lastPartialJoin + ctx.currentAlias.Name.SqlScape() + ".Id\r\n" + remainingJoins;

                if (ctx.remainingJoins != null)
                    select += ctx.remainingJoins;

                query = new SqlPreCommandSimple(select);
            }

            //Reader
            {
                rowReader = ctx.GetRowReader();
            }

            //Completer
            {
                ParameterExpression me = Expression.Parameter(typeof(T), "me");

                List<Expression> instructions = new List<Expression>();
                instructions.Add(Expression.Assign(ctx.origin, Expression.Convert(ctx.originObject, ctx.tupleType)));

                foreach (var f in table.Fields.Values.Where(f => !(f.Field is FieldPrimaryKey)))
                {
                    Expression value = Materialize(f.Field, ctx);
                    var assigment = Expression.Assign(Expression.Field(me, f.FieldInfo), value);
                    instructions.Add(assigment);
                }

                var block = Expression.Block(new[] { ctx.origin }, instructions);
                var lambda = Expression.Lambda<Action<object, IRetriever, T>>(block, me, ctx.originObject, ctx.retriever);

                completer = lambda.Compile();

                idGetter = ctx.GetGetter<int>(table.Fields.Values.OfType<FieldPrimaryKey>().Single());
                toStrGetter = ctx.GetGetter<string>((IColumn)table.Fields[SchemaBuilder.GetToStringFieldInfo(typeof(T)).Name]);
            }

            rows = new ResetLazy<Dictionary<int, object>>(() =>
            {
                Dictionary<int, object> result = new Dictionary<int, object>();
                ((SqlConnector)Connector.Current).ExecuteDataReaderDependency(query, OnChange, fr =>
                {
                    object obj = rowReader(fr);
                    result.Add(idGetter(obj), obj);
                });

                return result;
            });
        }

        protected void OnChange(object sender, SqlNotificationEventArgs args)
        {
            rows.Reset();

            OnChange(args);
        }


        public string GetToString(int id)
        {
            return toStrGetter(rows.Value[id]);
        }

        public void Complete(T entity, IRetriever retriever)
        {
            completer(rows.Value[entity.Id], retriever, entity);
        }
    }

    class CachedRelationalTable<T> : CachedTableBase
    {
        ResetLazy<Dictionary<int, List<object>>> relationalRows;

        Func<FieldReader, object> rowReader;
        SqlPreCommandSimple query;
        Func<object, IRetriever, T> activator;
        Func<object, int> parentIdGetter;

        public CachedRelationalTable(RelationalTable table, AliasGenerator aliasGenerator, string lastPartialJoin, string remainingJoins)
        {
            ConstructContext ctx = new ConstructContext(table, aliasGenerator);

            //Query
            {
                string select = "SELECT\r\n{0},\r\n{1}\r\nFROM {2} {3}\r\n".Formato(
                    ctx.table.Columns.Values.ToString(c => ctx.currentAlias.Name.SqlScape() + "." + c.Name.SqlScape(), ",\r\n"),
                    table.Name,
                    ctx.currentAlias.Name.SqlScape());

                ctx.remainingJoins = lastPartialJoin + ctx.currentAlias.Name.SqlScape() + "."+table.BackReference.Name.SqlScape() +"\r\n" + remainingJoins;

                query = new SqlPreCommandSimple(select);
            }

            //Reader
            {
                rowReader = ctx.GetRowReader();
            }

            //Completer
            {
                List<Expression> instructions = new List<Expression>();

                instructions.Add(Expression.Assign(ctx.origin, Expression.Convert(ctx.originObject, ctx.tupleType)));
                instructions.Add(Materialize(table.Field, ctx));

                var block = Expression.Block(table.Field.FieldType, new[] { ctx.origin }, instructions);

                var lambda = Expression.Lambda<Func<object, IRetriever, T>>(block, ctx.originObject, ctx.retriever);

                activator = lambda.Compile();

                parentIdGetter = ctx.GetGetter<int>(table.BackReference);
            }

            relationalRows = new ResetLazy<Dictionary<int, List<object>>>(() =>
            {
                Dictionary<int, List<object>> result = new Dictionary<int, List<object>>();
                ((SqlConnector)Connector.Current).ExecuteDataReaderDependency(query, OnChange, fr =>
                {
                    object obj = rowReader(fr);
                    int parentId = parentIdGetter(obj);
                    var list = result.TryGetC(parentId);
                    if (list == null)
                        result[parentId] = list = new List<object>();

                    list.Add(obj);
                });

                return result;
            });
        }

        protected void OnChange(object sender, SqlNotificationEventArgs args)
        {
            relationalRows.Reset();

            OnChange(args);
        }

        public MList<T> GetMList(int id, IRetriever retriever)
        {
            MList<T> result;
            var list = relationalRows.Value.TryGetC(id);
            if (list == null)
                result = new MList<T>();
            else
            {
                result = new MList<T>(list.Count);
                foreach (var obj in list)
                {
                    result.Add(activator(obj, retriever));
                }
            }

            resetModifiedAction(result);

            return result;
        }
    }

    class CachedSemiLite<T> : CachedTableBase where T:IdentifiableEntity
    {
        Func<FieldReader, KeyValuePair<int, string>> rowReader;
        SqlPreCommandSimple query;
        ResetLazy<Dictionary<int, string>> toStrings;

        public CachedSemiLite(AliasGenerator aliasGenerator, string lastPartialJoin, string remainingJoins)
        {
            Table table = Schema.Current.Table(typeof(T));

            Alias alias = aliasGenerator.NextTableAlias(table.Name.Name);

            ConstructContext ctx = new ConstructContext(table, aliasGenerator);

            IColumn colId = (IColumn)table.Fields["id"];
            IColumn colToStr = (IColumn)table.Fields[SchemaBuilder.GetToStringFieldInfo(typeof(T)).Name];

            //Query
            {
                string select = "SELECT {0} {1} FROM {1} {2}\r\n".Formato(
                    ctx.currentAlias.Name.SqlScape() + "." + colId.Name.SqlScape(),
                    ctx.currentAlias.Name.SqlScape() + "." + colToStr.Name.SqlScape(),
                    table.Name,
                    ctx.currentAlias.Name.SqlScape());

                select += lastPartialJoin + ctx.currentAlias.Name.SqlScape() + ".Id\r\n" + remainingJoins;

                query = new SqlPreCommandSimple(select);
            }

            //Reader
            {
                ParameterExpression reader = Expression.Parameter(typeof(FieldReader));

                var kvpConstructor = Expression.New(ciKVPIntString,
                    FieldReader.GetExpression(reader, 0, typeof(int)),
                    FieldReader.GetExpression(reader, 1, typeof(string)));

                rowReader = Expression.Lambda<Func<FieldReader, KeyValuePair<int, string>>>(kvpConstructor, reader).Compile();
            }

            toStrings = new ResetLazy<Dictionary<int, string>>(() =>
            {
                Dictionary<int, string> result = new Dictionary<int, string>();
                ((SqlConnector)Connector.Current).ExecuteDataReaderDependency(query, OnChange, fr =>
                {
                    var kvp = rowReader(fr);
                    result.Add(kvp.Key, kvp.Value);
                });

                return result;
            });
        }

        protected void OnChange(object sender, SqlNotificationEventArgs args)
        {
            toStrings.Reset();

            OnChange(args);
        }

        public Lite<T> GetLite(int id)
        {
            return Lite.Create<T>(id, toStrings.Value[id]);
        }
    }
}
