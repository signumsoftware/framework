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
using System.Diagnostics;
using System.Threading;
using Signum.Engine.Exceptions;
using Signum.Utilities.ExpressionTrees;
using System.Data;

namespace Signum.Engine.Cache
{
    public abstract class CachedTableBase
    {
        public abstract ITable Table { get; }

        List<CachedTableBase> subTables;
        public List<CachedTableBase> SubTables { get { return subTables; } }
        protected SqlPreCommandSimple query;
        ICacheLogicController controller;

        internal CachedTableBase(ICacheLogicController controller)
        {
            this.controller = controller;
        }

        protected void OnChange(object sender, SqlNotificationEventArgs args)
        {
            try
            {
                if (args.Info == SqlNotificationInfo.Invalid &&
                    args.Source == SqlNotificationSource.Statement &&
                    args.Type == SqlNotificationType.Subscribe)
                    throw new InvalidOperationException("Invalid query for SqlDependency") { Data = { { "query", query.PlainSql() } } };

                if (args.Info == SqlNotificationInfo.PreviousFire)
                    throw new InvalidOperationException("The same transaction that loaded the data is invalidating it!") { Data = { { "query", query.PlainSql() } } };

                if (CacheLogic.LogWriter != null)
                    CacheLogic.LogWriter.WriteLine("Change {0}".Formato(GetType().TypeName())); 

                Reset();

                Interlocked.Increment(ref invalidations);

                controller.OnChange(this, args);
            }
            catch (Exception e)
            {
                e.LogException();
            }
        }

        public void ResetAll(bool forceReset)
        {
            if (CacheLogic.LogWriter != null)
                CacheLogic.LogWriter.WriteLine("ResetAll {0}".Formato(GetType().TypeName()));

            Reset();

            if (forceReset)
            {
                invalidations = 0;
                hits = 0;
                loads = 0;
                sumLoadTime = 0;
            }
            else
            {
                Interlocked.Increment(ref invalidations);
            }

            if (subTables != null)
                foreach (var st in subTables)
                    st.ResetAll(forceReset);
        }


        internal void LoadAll()
        {
            Load();

            if (subTables != null)
                foreach (var st in subTables)
                    st.LoadAll();
        }

        protected abstract void Load();
        protected abstract void Reset();

        public abstract Type Type { get; }

        public abstract int? Count { get; }

        int invalidations;
        public int Invalidations { get { return invalidations; } }

        protected int hits;
        public int Hits { get { return hits; } }

        int loads;
        public int Loads { get { return loads; } }

        long sumLoadTime;
        public TimeSpan SumLoadTime
        {
            get { return TimeSpan.FromMilliseconds(sumLoadTime / PerfCounter.FrequencyMilliseconds); }
        }

        protected IDisposable MeasureLoad()
        {
            long start = PerfCounter.Ticks;

            return new Disposable(() =>
            {
                sumLoadTime += (PerfCounter.Ticks - start);
                Interlocked.Increment(ref loads);
            });
        }

        protected class CachedTableConstructor
        {
            public CachedTableBase cachedTable;
            public ITable table;

            public ParameterExpression origin;
        
            public AliasGenerator aliasGenerator;
            public Alias currentAlias;
            public Type tupleType;

            public string remainingJoins;

            public CachedTableConstructor(CachedTableBase cachedTable, AliasGenerator aliasGenerator)
            {
                this.cachedTable = cachedTable;
                this.table = cachedTable.Table;
                this.aliasGenerator = aliasGenerator;
                this.currentAlias = aliasGenerator.NextTableAlias(table.Name.Name);

                this.tupleType = TupleReflection.TupleChainType(table.Columns.Values.Select(GetColumnType));

                this.origin = Expression.Parameter(tupleType, "origin");
            }

            public Expression GetTupleProperty(IColumn column)
            {
                return TupleReflection.TupleChainProperty(origin, table.Columns.Values.IndexOf(column));
            }

            internal string CreatePartialInnerJoin(IColumn column)
            {
                return "INNER JOIN {0} {1} ON {1}.{2}=".Formato(table.Name.ToStringDbo(), currentAlias.Name.SqlEscape(), column.Name);
            }

            internal Type GetColumnType(IColumn column)
            {
                if (column is FieldValue)
                {
                    var type = ((Field)column).FieldType;
                    return column.Nullable ? type.Nullify() : type.UnNullify();
                }

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

            static GenericInvoker<Func<ICacheLogicController, AliasGenerator, string, string, CachedTableBase>> ciCachedTable =
             new GenericInvoker<Func<ICacheLogicController, AliasGenerator, string, string, CachedTableBase>>((controller, aliasGenerator, lastPartialJoin, remainingJoins) =>
                 new CachedTable<IdentifiableEntity>(controller, aliasGenerator, lastPartialJoin, remainingJoins));

            static GenericInvoker<Func<ICacheLogicController, AliasGenerator, string, string, CachedTableBase>> ciCachedSemiTable =
              new GenericInvoker<Func<ICacheLogicController, AliasGenerator, string, string, CachedTableBase>>((controller, aliasGenerator, lastPartialJoin, remainingJoins) =>
                  new CachedLiteTable<IdentifiableEntity>(controller, aliasGenerator, lastPartialJoin, remainingJoins));

            static GenericInvoker<Func<ICacheLogicController, TableMList, AliasGenerator, string, string, CachedTableBase>> ciCachedTableMList =
              new GenericInvoker<Func<ICacheLogicController, TableMList, AliasGenerator, string, string, CachedTableBase>>((controller, relationalTable, aliasGenerator, lastPartialJoin, remainingJoins) =>
                  new CachedTableMList<IdentifiableEntity>(controller, relationalTable, aliasGenerator, lastPartialJoin, remainingJoins));

            static Expression NullId = Expression.Constant(null, typeof(int?));

            public Expression MaterializeField(Field field)
            {
                if (field is FieldValue)
                {
                    var value = GetTupleProperty((IColumn)field);
                    return value.Type == field.FieldType ? value : Expression.Convert(value, field.FieldType);
                }

                if (field is FieldEnum)
                    return Expression.Convert(GetTupleProperty((IColumn)field), field.FieldType);

                if (field is IFieldReference)
                {
                    var nullRef = Expression.Constant(null, field.FieldType);
                    bool isLite = ((IFieldReference)field).IsLite;

                    if (field is FieldReference)
                    {
                        IColumn column = (IColumn)field;

                        return GetEntity(isLite, column, field.FieldType.CleanType());
                    }

                    if (field is FieldImplementedBy)
                    {
                        var ib = (FieldImplementedBy)field;

                        var call = ib.ImplementationColumns.Aggregate((Expression)nullRef, (acum, kvp) =>
                        {
                            IColumn column = (IColumn)kvp.Value;

                            Expression entity = GetEntity(isLite, column, kvp.Key);

                            return Expression.Condition(Expression.NotEqual(GetTupleProperty(column), NullId),
                                Expression.Convert(entity, field.FieldType),
                                acum);
                        });

                        return call;
                    }

                    if (field is FieldImplementedByAll)
                    {
                        var iba = (FieldImplementedByAll)field;

                        Expression id = GetTupleProperty(iba.Column);
                        Expression typeId = GetTupleProperty(iba.ColumnType);

                        if (isLite)
                        {
                            var liteCreate = Expression.Call(miLiteCreate,
                                SchemaGetType(typeId.UnNullify()),
                                id.UnNullify(),
                                Expression.Constant(null, typeof(string)));

                            var liteRequest = Expression.Call(retriever, miRequestLite.MakeGenericMethod(Lite.Extract(field.FieldType)), liteCreate);

                            return Expression.Condition(Expression.NotEqual(id, NullId), liteRequest, nullRef);
                        }
                        else
                        {
                            return Expression.Call(retriever, miRequestIBA.MakeGenericMethod(field.FieldType), id, typeId);
                        }
                    }
                }

                if (field is FieldEmbedded)
                {
                    var fe = (FieldEmbedded)field;

                    Expression ctor = Expression.MemberInit(Expression.New(fe.FieldType),
                        fe.EmbeddedFields.Values.Select(f => Expression.Bind(f.FieldInfo, MaterializeField(f.Field)))
                        .And(Expression.Bind(piModified, retrieverModifiedState)));

                    if (fe.HasValue == null)
                        return ctor;

                    return Expression.Condition(
                        Expression.Equal(GetTupleProperty(fe.HasValue), Expression.Constant(true)),
                        ctor,
                        Expression.Constant(null, field.FieldType));
                }


                if (field is FieldMList)
                {
                    var mListField = (FieldMList)field;

                    var idColumn = table.Columns.Values.OfType<FieldPrimaryKey>().First();

                    string lastPartialJoin = CreatePartialInnerJoin(idColumn);

                    Type elementType = field.FieldType.ElementType();

                    CachedTableBase ctb = ciCachedTableMList.GetInvoker(elementType)(cachedTable.controller, mListField.TableMList, aliasGenerator, lastPartialJoin, remainingJoins);

                    if (cachedTable.subTables == null)
                        cachedTable.subTables = new List<CachedTableBase>();

                    cachedTable.subTables.Add(ctb);

                    return Expression.Call(Expression.Constant(ctb), ctb.GetType().GetMethod("GetMList"), GetTupleProperty(idColumn), retriever);
                }

                throw new InvalidOperationException("Unexpected {0}".Formato(field.GetType().Name));
            }

            private Expression GetEntity(bool isLite, IColumn column, Type type)
            {
                Expression id = GetTupleProperty(column);

                if (isLite)
                {
                    Expression lite;
                    switch (CacheLogic.GetCacheType(type))
                    {
                        case CacheType.Cached:
                            {
                                lite = Expression.Call(retriever, miRequestLite.MakeGenericMethod(type),
                                    Lite.NewExpression(type, id.UnNullify(), Expression.Constant(null, typeof(string)), peModified));

                                break;
                            }
                        case CacheType.Semi:
                            {
                                string lastPartialJoin = CreatePartialInnerJoin(column);

                                CachedTableBase ctb = ciCachedSemiTable.GetInvoker(type)(cachedTable.controller, aliasGenerator, lastPartialJoin, remainingJoins);

                                if (cachedTable.subTables == null)
                                    cachedTable.subTables = new List<CachedTableBase>();

                                cachedTable.subTables.Add(ctb);

                                lite = Expression.Call(Expression.Constant(ctb), ctb.GetType().GetMethod("GetLite"), id.UnNullify(), retriever);

                                break;
                            }
                        default: throw new InvalidOperationException("{0} should be cached at this stage".Formato(type));
                    }

                    if (!id.Type.IsNullable())
                        return lite;

                    return Expression.Condition(Expression.Equal(id, NullId), Expression.Constant(null, Lite.Generate(type)), lite);
                }
                else
                {
                    switch (CacheLogic.GetCacheType(type))
                    {
                        case CacheType.Cached: return Expression.Call(retriever, miRequest.MakeGenericMethod(type), id.Nullify());
                        case CacheType.Semi:
                            {
                                string lastPartialJoin = CreatePartialInnerJoin(column);

                                CachedTableBase ctb = ciCachedTable.GetInvoker(type)(cachedTable.controller, aliasGenerator, lastPartialJoin, remainingJoins);

                                if (cachedTable.subTables == null)
                                    cachedTable.subTables = new List<CachedTableBase>();

                                cachedTable.subTables.Add(ctb);

                                var entity = Expression.Parameter(type);
                                LambdaExpression lambda = Expression.Lambda(typeof(Action<>).MakeGenericType(type),
                                    Expression.Call(Expression.Constant(ctb), ctb.GetType().GetMethod("Complete"), entity, retriever),
                                    entity);

                                return Expression.Call(retriever, miComplete.MakeGenericMethod(type), id.Nullify(), lambda);
                            }
                        default: throw new InvalidOperationException("{0} should be cached at this stage".Formato(type));
                    }
                }
            }

            static MethodInfo miRequestLite = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestLite<IdentifiableEntity>(null)).GetGenericMethodDefinition();
            static MethodInfo miRequestIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestIBA<IdentifiableEntity>(null, null)).GetGenericMethodDefinition();
            static MethodInfo miRequest = ReflectionTools.GetMethodInfo((IRetriever r) => r.Request<IdentifiableEntity>(null)).GetGenericMethodDefinition();
            static MethodInfo miComplete = ReflectionTools.GetMethodInfo((IRetriever r) => r.Complete<IdentifiableEntity>(0, null)).GetGenericMethodDefinition();

            internal static ParameterExpression originObject = Expression.Parameter(typeof(object), "originObject");
            internal static ParameterExpression retriever = Expression.Parameter(typeof(IRetriever), "retriever");

            static PropertyInfo piModified = ReflectionTools.GetPropertyInfo((Modifiable me) => me.Modified);
            static MemberExpression retrieverModifiedState = Expression.Property(retriever, ReflectionTools.GetPropertyInfo((IRetriever re) => re.ModifiedState));

                
            static MethodInfo miLiteCreate = ReflectionTools.GetMethodInfo(() => Lite.Create(null, 0, null));

            public static MemberExpression peModified = Expression.Property(retriever, ReflectionTools.GetPropertyInfo((IRetriever me) => me.ModifiedState));

            public static ConstructorInfo ciKVPIntString = ReflectionTools.GetConstuctorInfo(() => new KeyValuePair<int, string>(1, ""));


            public static Action<IRetriever, Modifiable> resetModifiedAction; 

            static CachedTableConstructor()
            {
                ParameterExpression modif = Expression.Parameter(typeof(Modifiable));

                resetModifiedAction = Expression.Lambda<Action<IRetriever, Modifiable>>(Expression.Assign(
                    Expression.Property(modif, ReflectionTools.GetPropertyInfo((Modifiable me) => me.Modified)), 
                    CachedTableConstructor.peModified),
                    CachedTableConstructor.retriever, modif).Compile();
            }


            static MethodInfo miGetType = ReflectionTools.GetMethodInfo((Schema s) => s.GetType(1));
            MethodCallExpression SchemaGetType(Expression idtype)
            {
                return Expression.Call(Expression.Constant(Schema.Current), miGetType, idtype);
            }


            static readonly MethodInfo miMixin = ReflectionTools.GetMethodInfo((IdentifiableEntity i) => i.Mixin<CorruptMixin>()).GetGenericMethodDefinition();
            Expression GetMixin(ParameterExpression me, Type mixinType)
            {
                return Expression.Call(me, miMixin.MakeGenericMethod(mixinType));
            }

            internal BlockExpression MaterializeEntity(ParameterExpression me, Table table)
            {
                List<Expression> instructions = new List<Expression>();
                instructions.Add(Expression.Assign(origin, Expression.Convert(CachedTableConstructor.originObject, tupleType)));

                foreach (var f in table.Fields.Values.Where(f => !(f.Field is FieldPrimaryKey)))
                {
                    Expression value = MaterializeField(f.Field);
                    var assigment = Expression.Assign(Expression.Field(me, f.FieldInfo), value);
                    instructions.Add(assigment);
                }

                if (table.Mixins != null)
                {
                    foreach (var mixin in table.Mixins.Values)
                    {
                        ParameterExpression mixParam = Expression.Parameter(mixin.FieldType);

                        var mixBlock = MaterializeMixin(me, mixin, mixParam);

                        instructions.Add(mixBlock);
                    }
                }

                var block = Expression.Block(new[] { origin }, instructions);

                return block;
            }

            private BlockExpression MaterializeMixin(ParameterExpression me, FieldMixin mixin, ParameterExpression mixParam)
            {
                List<Expression> mixBindings = new List<Expression>();
                mixBindings.Add(Expression.Assign(mixParam, GetMixin(me, mixin.FieldType)));

                foreach (var f in mixin.Fields.Values)
                {
                    Expression value = MaterializeField(f.Field);
                    var assigment = Expression.Assign(Expression.Field(mixParam, f.FieldInfo), value);
                    mixBindings.Add(assigment);
                }

                mixBindings.Add(Expression.Assign(Expression.Property(mixParam, piModified), retrieverModifiedState));

                var mixBlock = Expression.Block(new[] { mixParam }, mixBindings);
                return mixBlock;
            }
        }
    }



    class CachedTable<T> : CachedTableBase where T : IdentifiableEntity
    {
        Table table;

        ResetLazy<Dictionary<int, object>> rows;

        Func<FieldReader, object> rowReader;
        Action<object, IRetriever, T> completer;
        Expression<Action<object, IRetriever, T>> completerExpression;
        Func<object, int> idGetter;
        Func<object, string> toStrGetter;

        public CachedTable(ICacheLogicController controller, AliasGenerator aliasGenerator, string lastPartialJoin, string remainingJoins)
            : base(controller)
        {
            this.table = Schema.Current.Table(typeof(T));

            CachedTableConstructor ctr = new CachedTableConstructor(this, aliasGenerator);

            //Query
            using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
            {
                string select = "SELECT\r\n{0}\r\nFROM {1} {2}\r\n".Formato(
                    Table.Columns.Values.ToString(c => ctr.currentAlias.Name.SqlEscape() + "." + c.Name.SqlEscape(), ",\r\n"),
                    table.Name.ToStringDbo(),
                    ctr.currentAlias.Name.SqlEscape());

                ctr.remainingJoins = lastPartialJoin == null ? null : lastPartialJoin + ctr.currentAlias.Name.SqlEscape() + ".Id\r\n" + remainingJoins;

                if (ctr.remainingJoins != null)
                    select += ctr.remainingJoins;

                query = new SqlPreCommandSimple(select);
            }
            

            //Reader
            {
                rowReader = ctr.GetRowReader();
            }

            //Completer
            {
                ParameterExpression me = Expression.Parameter(typeof(T), "me");

                var block = ctr.MaterializeEntity(me, table);

                completerExpression = Expression.Lambda<Action<object, IRetriever, T>>(block, CachedTableConstructor.originObject, CachedTableConstructor.retriever, me);

                completer = completerExpression.Compile();

                idGetter = ctr.GetGetter<int>((IColumn)table.Fields.Values.Select(ef => ef.Field).Single(f => f is FieldPrimaryKey));
                toStrGetter = ctr.GetGetter<string>((IColumn)table.Fields[SchemaBuilder.GetToStringFieldInfo(typeof(T)).Name].Field);
            }

            rows = new ResetLazy<Dictionary<int, object>>(() =>
            {
                CacheLogic.OnStart();

                var connector = (SqlConnector)Connector.Current;
                Table table = connector.Schema.Table(typeof(T));

                var subConnector = connector.ForDatabase(table.Name.Schema.Try(s => s.Database));

                Dictionary<int, object> result = new Dictionary<int, object>();
                using (MeasureLoad())
                using (Connector.Override(subConnector))
                using (Transaction tr = Transaction.ForceNew(IsolationLevel.ReadCommitted))
                {
                    if (CacheLogic.LogWriter != null)
                        CacheLogic.LogWriter.WriteLine("Load {0}".Formato(GetType().TypeName()));

                    ((SqlConnector)Connector.Current).ExecuteDataReaderOpionalDependency(query, OnChange, fr =>
                    {
                        object obj = rowReader(fr);
                        result[idGetter(obj)] = obj; //Could be repeated joins
                    });
                    tr.Commit();
                }

                return result;
            }, mode: LazyThreadSafetyMode.ExecutionAndPublication);
        }


        protected override void Reset()
        {
            if (CacheLogic.LogWriter != null)
                CacheLogic.LogWriter.WriteLine((rows.IsValueCreated ? "RESET {0}" : "Reset {0}").Formato(GetType().TypeName()));

            rows.Reset();
        }

        protected override void Load()
        {
            rows.Load();
        }

        public string GetToString(int id)
        {
            Interlocked.Increment(ref hits);
            var origin = rows.Value.TryGetC(id);
            if (origin == null)
                throw new EntityNotFoundException(typeof(T), id);

            return toStrGetter(origin);
        }

        public string TryGetToString(int id)
        {
            Interlocked.Increment(ref hits);
            var origin = rows.Value.TryGetC(id);
            if (origin == null)
                return null;

            return toStrGetter(rows.Value[id]);
        }

        public void Complete(T entity, IRetriever retriever)
        {
            Interlocked.Increment(ref hits);

            var origin = rows.Value.TryGetC(entity.Id);
            if (origin == null)
                throw new EntityNotFoundException(typeof(T), entity.Id);

            completer(origin, retriever, entity);
        }

        internal IEnumerable<int> GetAllIds()
        {
            Interlocked.Increment(ref hits);
            return rows.Value.Keys;
        }

        public override int? Count
        {
            get { return rows.IsValueCreated ? rows.Value.Count : (int?)null; }
        }

        public override Type Type
        {
            get { return typeof(T); }
        }

        public override ITable Table
        {
            get { return table; }
        }
    }

    class CachedTableMList<T> : CachedTableBase
    {
        TableMList table;

        ResetLazy<Dictionary<int, Dictionary<int, object>>> relationalRows;

        static ParameterExpression result = Expression.Parameter(typeof(T));

        Func<FieldReader, object> rowReader;
        Expression<Func<object, IRetriever, MList<T>.RowIdValue>> activatorExpression;
        Func<object, IRetriever, MList<T>.RowIdValue> activator;
        Func<object, int> parentIdGetter;
        Func<object, int> rowIdGetter;

        public CachedTableMList(ICacheLogicController controller, TableMList table, AliasGenerator aliasGenerator, string lastPartialJoin, string remainingJoins)
            : base(controller)
        {
            this.table = table;

            CachedTableConstructor ctr = new CachedTableConstructor(this, aliasGenerator);

            //Query
            using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
            {
                string select = "SELECT\r\n{0}\r\nFROM {1} {2}\r\n".Formato(
                    ctr.table.Columns.Values.ToString(c => ctr.currentAlias.Name.SqlEscape() + "." + c.Name.SqlEscape(), ",\r\n"),
                    table.Name.ToStringDbo(),
                    ctr.currentAlias.Name.SqlEscape());

                ctr.remainingJoins = lastPartialJoin + ctr.currentAlias.Name.SqlEscape() + "." + table.BackReference.Name.SqlEscape() + "\r\n" + remainingJoins;

                query = new SqlPreCommandSimple(select);
            }

            //Reader
            {
                rowReader = ctr.GetRowReader();
            }

            //Completer
            {
                List<Expression> instructions = new List<Expression>();

                instructions.Add(Expression.Assign(ctr.origin, Expression.Convert(CachedTableConstructor.originObject, ctr.tupleType)));
                instructions.Add(Expression.Assign(result, ctr.MaterializeField(table.Field)));

                var ci = typeof(MList<T>.RowIdValue).GetConstructor(new []{typeof(T), typeof(int), typeof(int?)});

                var order = table.Order == null ? Expression.Constant(null, typeof(int?)) : 
                     ctr.GetTupleProperty(table.Order).Nullify();

                instructions.Add(Expression.New(ci, result, ctr.GetTupleProperty(table.PrimaryKey), order));

                var block = Expression.Block(typeof(MList<T>.RowIdValue), new[] { ctr.origin, result }, instructions);

                activatorExpression = Expression.Lambda<Func<object, IRetriever, MList<T>.RowIdValue>>(block, CachedTableConstructor.originObject, CachedTableConstructor.retriever);

                activator = activatorExpression.Compile();

                parentIdGetter = ctr.GetGetter<int>(table.BackReference);
                rowIdGetter = ctr.GetGetter<int>(table.PrimaryKey);
            }

            relationalRows = new ResetLazy<Dictionary<int, Dictionary<int, object>>>(() =>
            {
                CacheLogic.OnStart();

                var connector = (SqlConnector)Connector.Current;

                var subConnector = connector.ForDatabase(table.Name.Schema.Try(s => s.Database));

                Dictionary<int, Dictionary<int, object>> result = new Dictionary<int, Dictionary<int, object>>();

                using (MeasureLoad())
                using (Connector.Override(subConnector))
                using (Transaction tr = Transaction.ForceNew(IsolationLevel.ReadCommitted))
                {
                    if (CacheLogic.LogWriter != null)
                        CacheLogic.LogWriter.WriteLine("Load {0}".Formato(GetType().TypeName()));

                    ((SqlConnector)Connector.Current).ExecuteDataReaderOpionalDependency(query, OnChange, fr =>
                    {
                        object obj = rowReader(fr);
                        int parentId = parentIdGetter(obj);
                        var dic = result.TryGetC(parentId);
                        if (dic == null)
                            result[parentId] = dic = new Dictionary<int, object>();

                        dic[rowIdGetter(obj)] = obj;
                    });
                    tr.Commit();
                }

                return result;
            }, mode: LazyThreadSafetyMode.ExecutionAndPublication);
        }

        protected override void Reset()
        {
            if (CacheLogic.LogWriter != null)
                CacheLogic.LogWriter.WriteLine((relationalRows.IsValueCreated ? "RESET {0}" : "Reset {0}").Formato(GetType().TypeName()));


            relationalRows.Reset();
        }

        protected override void Load()
        {
            relationalRows.Load();
        }

        public MList<T> GetMList(int id, IRetriever retriever)
        {
            Interlocked.Increment(ref hits);

            MList<T> result;
            var dic = relationalRows.Value.TryGetC(id);
            if (dic == null)
                result = new MList<T>();
            else
            {
                result = new MList<T>(dic.Count);
                var innerList = ((IMListPrivate<T>)result).InnerList;
                foreach (var obj in dic.Values)
                {
                    innerList.Add(activator(obj, retriever));
                }
                ((IMListPrivate)result).ExecutePostRetrieving();
                
            }

            CachedTableConstructor.resetModifiedAction(retriever, result);

            return result;
        }

        public override int? Count
        {
            get { return relationalRows.IsValueCreated ? relationalRows.Value.Count : (int?)null; }
        }

        public override Type Type
        {
            get { return typeof(MList<T>); }
        }

        public override ITable Table
        {
            get { return table; }
        }
    }

    class CachedLiteTable<T> : CachedTableBase where T : IdentifiableEntity
    {
        Table table;

        Func<FieldReader, KeyValuePair<int, string>> rowReader;
        ResetLazy<Dictionary<int, string>> toStrings;

        public CachedLiteTable(ICacheLogicController controller, AliasGenerator aliasGenerator, string lastPartialJoin, string remainingJoins)
            : base(controller)
        {
            this.table = Schema.Current.Table(typeof(T));

            Alias currentAlias = aliasGenerator.NextTableAlias(table.Name.Name);

            IColumn colId = (IColumn)table.Fields["id"].Field;
            IColumn colToStr = (IColumn)table.Fields[SchemaBuilder.GetToStringFieldInfo(typeof(T)).Name].Field;

            //Query
            using (ObjectName.OverrideOptions(new ObjectNameOptions { AvoidDatabaseName = true }))
            {
                string select = "SELECT {0}, {1}\r\nFROM {2} {3}\r\n".Formato(
                    currentAlias.Name.SqlEscape() + "." + colId.Name.SqlEscape(),
                    currentAlias.Name.SqlEscape() + "." + colToStr.Name.SqlEscape(),
                    table.Name.ToStringDbo(),
                    currentAlias.Name.SqlEscape());

                select += lastPartialJoin + currentAlias.Name.SqlEscape() + ".Id\r\n" + remainingJoins;

                query = new SqlPreCommandSimple(select);
            }

            //Reader
            {
                ParameterExpression reader = Expression.Parameter(typeof(FieldReader));

                var kvpConstructor = Expression.New(CachedTableConstructor.ciKVPIntString,
                    FieldReader.GetExpression(reader, 0, typeof(int)),
                    FieldReader.GetExpression(reader, 1, typeof(string)));

                rowReader = Expression.Lambda<Func<FieldReader, KeyValuePair<int, string>>>(kvpConstructor, reader).Compile();
            }

            toStrings = new ResetLazy<Dictionary<int, string>>(() =>
            {
                CacheLogic.OnStart();

                var connector = (SqlConnector)Connector.Current;

                var subConnector = connector.ForDatabase(table.Name.Schema.Try(s => s.Database));

                Dictionary<int, string> result = new Dictionary<int, string>();

                using (MeasureLoad())
                using (Connector.Override(subConnector))
                using (Transaction tr = Transaction.ForceNew(IsolationLevel.ReadCommitted))
                {
                    if (CacheLogic.LogWriter != null)
                        CacheLogic.LogWriter.WriteLine("Load {0}".Formato(GetType().TypeName()));

                    ((SqlConnector)Connector.Current).ExecuteDataReaderOpionalDependency(query, OnChange, fr =>
                    {
                        var kvp = rowReader(fr);
                        result[kvp.Key] = kvp.Value;
                    });
                    tr.Commit();
                }

                return result;
            }, mode: LazyThreadSafetyMode.ExecutionAndPublication);
        }

        protected override void Reset()
        {
            if (CacheLogic.LogWriter != null)
                CacheLogic.LogWriter.WriteLine((toStrings.IsValueCreated ? "RESET {0}" : "Reset {0}").Formato(GetType().TypeName()));

            toStrings.Reset();
        }

        protected override void Load()
        {
            toStrings.Load();
        }


        public Lite<T> GetLite(int id, IRetriever retriever)
        {
            Interlocked.Increment(ref hits);

            return Lite.Create<T>(id, toStrings.Value[id], retriever.ModifiedState);
        }

        public override int? Count
        {
            get { return toStrings.IsValueCreated ? toStrings.Value.Count : (int?)null; }
        }

        public override Type Type
        {
            get { return typeof(Lite<T>); }
        }

        public override ITable Table
        {
            get { return table; }
        }
    }
}
