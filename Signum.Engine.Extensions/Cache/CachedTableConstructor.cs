using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Entities.Internal;

namespace Signum.Engine.Cache
{
    internal class CachedTableConstructor
    {
        public CachedTableBase cachedTable;
        public ITable table;

        public ParameterExpression origin;

        public AliasGenerator? aliasGenerator;
        public Alias? currentAlias;
        public Type tupleType;

        public string? remainingJoins;

        public CachedTableConstructor(CachedTableBase cachedTable, AliasGenerator? aliasGenerator)
        {
            this.cachedTable = cachedTable;
            this.table = cachedTable.Table;

            if (aliasGenerator != null)
            {
                this.aliasGenerator = aliasGenerator;
                this.currentAlias = aliasGenerator.NextTableAlias(table.Name.Name);
            }

            this.tupleType = TupleReflection.TupleChainType(table.Columns.Values.Select(GetColumnType));

            this.origin = Expression.Parameter(tupleType, "origin");
        }

        public Expression GetTupleProperty(IColumn column)
        {
            return TupleReflection.TupleChainProperty(origin, table.Columns.Values.IndexOf(column));
        }

        internal string CreatePartialInnerJoin(IColumn column)
        {
            return "INNER JOIN {0} {1} ON {1}.{2}=".FormatWith(table.Name.ToString(), currentAlias, column.Name);
        }

        internal Type GetColumnType(IColumn column)
        {
            return column.Type;
        }

        internal Func<FieldReader, object> GetRowReader()
        {
            ParameterExpression reader = Expression.Parameter(typeof(FieldReader));

            var tupleConstructor = TupleReflection.TupleChainConstructor(
                table.Columns.Values.Select((c, i) => FieldReader.GetExpression(reader, i, GetColumnType(c)))
                );

            return Expression.Lambda<Func<FieldReader, object>>(tupleConstructor, reader).Compile();
        }

        internal Func<object, PrimaryKey> GetPrimaryKeyGetter(IColumn column)
        {
            var access = TupleReflection.TupleChainProperty(Expression.Convert(originObject, tupleType), table.Columns.Values.IndexOf(column));

            var primaryKey = NewPrimaryKey(access);

            return Expression.Lambda<Func<object, PrimaryKey>>(primaryKey, originObject).Compile();
        }

        internal Func<object, PrimaryKey?> GetPrimaryKeyNullableGetter(IColumn column)
        {
            var access = TupleReflection.TupleChainProperty(Expression.Convert(originObject, tupleType), table.Columns.Values.IndexOf(column));

            var primaryKey = WrapPrimaryKey(access);

            return Expression.Lambda<Func<object, PrimaryKey?>>(primaryKey, originObject).Compile();
        }

        static ConstructorInfo ciPrimaryKey = ReflectionTools.GetConstuctorInfo(() => new PrimaryKey(1));

        internal static Expression NewPrimaryKey(Expression expression)
        {
            return Expression.New(ciPrimaryKey, Expression.Convert(expression, typeof(IComparable)));
        }


        static GenericInvoker<Func<ICacheLogicController, AliasGenerator?, string, string?, CachedTableBase>> ciCachedTable =
         new GenericInvoker<Func<ICacheLogicController, AliasGenerator?, string, string?, CachedTableBase>>((controller, aliasGenerator, lastPartialJoin, remainingJoins) =>
             new CachedTable<Entity>(controller, aliasGenerator, lastPartialJoin, remainingJoins));

        static GenericInvoker<Func<ICacheLogicController, AliasGenerator, string, string?, CachedTableBase>> ciCachedSemiTable =
          new GenericInvoker<Func<ICacheLogicController, AliasGenerator, string, string?, CachedTableBase>>((controller, aliasGenerator, lastPartialJoin, remainingJoins) =>
              new CachedLiteTable<Entity>(controller, aliasGenerator, lastPartialJoin, remainingJoins));

        static GenericInvoker<Func<ICacheLogicController, TableMList, AliasGenerator?, string, string?, CachedTableBase>> ciCachedTableMList =
          new GenericInvoker<Func<ICacheLogicController, TableMList, AliasGenerator?, string, string?, CachedTableBase>>((controller, relationalTable, aliasGenerator, lastPartialJoin, remainingJoins) =>
              new CachedTableMList<Entity>(controller, relationalTable, aliasGenerator, lastPartialJoin, remainingJoins));

        static Expression NullId = Expression.Constant(null, typeof(PrimaryKey?));

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

                if (field is FieldImplementedBy ib)
                {
                    var call = ib.ImplementationColumns.Aggregate((Expression)nullRef, (acum, kvp) =>
                    {
                        IColumn column = (IColumn)kvp.Value;

                        Expression entity = GetEntity(isLite, column, kvp.Key);

                        return Expression.Condition(Expression.NotEqual(WrapPrimaryKey(GetTupleProperty(column)), NullId),
                            Expression.Convert(entity, field.FieldType),
                            acum);
                    });

                    return call;
                }

                if (field is FieldImplementedByAll iba)
                {
                    Expression id = GetTupleProperty(iba.Column);
                    Expression typeId = GetTupleProperty(iba.ColumnType);

                    if (isLite)
                    {
                        var liteCreate = Expression.Call(miGetIBALite.MakeGenericMethod(field.FieldType.CleanType()),
                            Expression.Constant(Schema.Current),
                            NewPrimaryKey(typeId.UnNullify()),
                            id.UnNullify());

                        var liteRequest = Expression.Call(retriever, miRequestLite.MakeGenericMethod(Lite.Extract(field.FieldType)!), liteCreate);

                        return Expression.Condition(Expression.NotEqual(WrapPrimaryKey(id), NullId), liteRequest, nullRef);
                    }
                    else
                    {
                        return Expression.Call(retriever, miRequestIBA.MakeGenericMethod(field.FieldType), typeId, id);
                    }
                }
            }

            if (field is FieldEmbedded fe)
            {
                Expression ctor = Expression.MemberInit(Expression.New(fe.FieldType),
                    fe.EmbeddedFields.Values.Select(f => Expression.Bind(f.FieldInfo, MaterializeField(f.Field))));

                var result = Expression.Call(retriever, miModifiablePostRetrieving.MakeGenericMethod(ctor.Type), ctor);

                if (fe.HasValue == null)
                    return result;

                return Expression.Condition(
                    Expression.Equal(GetTupleProperty(fe.HasValue), Expression.Constant(true)),
                    result,
                    Expression.Constant(null, field.FieldType));
            }


            if (field is FieldMList mListField)
            {
                var idColumn = table.Columns.Values.OfType<FieldPrimaryKey>().First();

                string lastPartialJoin = CreatePartialInnerJoin(idColumn);

                Type elementType = field.FieldType.ElementType()!;

                CachedTableBase ctb = ciCachedTableMList.GetInvoker(elementType)(cachedTable.controller, mListField.TableMList, aliasGenerator, lastPartialJoin, remainingJoins);

                if (cachedTable.subTables == null)
                    cachedTable.subTables = new List<CachedTableBase>();

                cachedTable.subTables.Add(ctb);

                return Expression.Call(Expression.Constant(ctb), ctb.GetType().GetMethod("GetMList"), NewPrimaryKey(GetTupleProperty(idColumn)), retriever);
            }

            throw new InvalidOperationException("Unexpected {0}".FormatWith(field.GetType().Name));
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
                                Lite.NewExpression(type, NewPrimaryKey(id.UnNullify()), Expression.Constant(null, typeof(string))));

                            lite = Expression.Call(retriever, miModifiablePostRetrieving.MakeGenericMethod(typeof(LiteImp)), lite.TryConvert(typeof(LiteImp))).TryConvert(lite.Type);

                            break;
                        }
                    case CacheType.Semi:
                        {
                            string lastPartialJoin = CreatePartialInnerJoin(column);

                            CachedTableBase ctb = ciCachedSemiTable.GetInvoker(type)(cachedTable.controller, aliasGenerator!, lastPartialJoin, remainingJoins);

                            if (cachedTable.subTables == null)
                                cachedTable.subTables = new List<CachedTableBase>();

                            cachedTable.subTables.Add(ctb);

                            ctb.ParentColumn = column;

                            lite = Expression.Call(Expression.Constant(ctb), ctb.GetType().GetMethod("GetLite"), NewPrimaryKey(id.UnNullify()), retriever);

                            break;
                        }
                    default: throw new InvalidOperationException("{0} should be cached at this stage".FormatWith(type));
                }

                if (!id.Type.IsNullable())
                    return lite;

                return Expression.Condition(Expression.Equal(id, NullId), Expression.Constant(null, Lite.Generate(type)), lite);
            }
            else
            {
                switch (CacheLogic.GetCacheType(type))
                {
                    case CacheType.Cached: return Expression.Call(retriever, miRequest.MakeGenericMethod(type), WrapPrimaryKey(id.Nullify()));
                    case CacheType.Semi:
                        {
                            string lastPartialJoin = CreatePartialInnerJoin(column);

                            CachedTableBase ctb = ciCachedTable.GetInvoker(type)(cachedTable.controller, aliasGenerator, lastPartialJoin, remainingJoins);

                            if (cachedTable.subTables == null)
                                cachedTable.subTables = new List<CachedTableBase>();

                            cachedTable.subTables.Add(ctb);

                            ctb.ParentColumn = column;

                            var entity = Expression.Parameter(type);
                            LambdaExpression lambda = Expression.Lambda(typeof(Action<>).MakeGenericType(type),
                                Expression.Call(Expression.Constant(ctb), ctb.GetType().GetMethod("Complete"), entity, retriever),
                                entity);

                            return Expression.Call(retriever, miComplete.MakeGenericMethod(type), WrapPrimaryKey(id.Nullify()), lambda);
                        }
                    default: throw new InvalidOperationException("{0} should be cached at this stage".FormatWith(type));
                }
            }
        }

        static readonly MethodInfo miWrap = ReflectionTools.GetMethodInfo(() => PrimaryKey.Wrap(1));

        internal static Expression WrapPrimaryKey(Expression expression)
        {
            return Expression.Call(miWrap, Expression.Convert(expression, typeof(IComparable)));
        }

        static MethodInfo miRequestLite = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestLite<Entity>(null)).GetGenericMethodDefinition();
        static MethodInfo miRequestIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestIBA<Entity>(null, null)).GetGenericMethodDefinition();
        static MethodInfo miRequest = ReflectionTools.GetMethodInfo((IRetriever r) => r.Request<Entity>(null)).GetGenericMethodDefinition();
        static MethodInfo miComplete = ReflectionTools.GetMethodInfo((IRetriever r) => r.Complete<Entity>(0, null!)).GetGenericMethodDefinition();
        static MethodInfo miModifiablePostRetrieving = ReflectionTools.GetMethodInfo((IRetriever r) => r.ModifiablePostRetrieving<EmbeddedEntity>(null)).GetGenericMethodDefinition();

        internal static ParameterExpression originObject = Expression.Parameter(typeof(object), "originObject");
        internal static ParameterExpression retriever = Expression.Parameter(typeof(IRetriever), "retriever");


        static MethodInfo miGetIBALite = ReflectionTools.GetMethodInfo((Schema s) => GetIBALite<Entity>(null!, 1, "")).GetGenericMethodDefinition();
        public static Lite<T> GetIBALite<T>(Schema schema, PrimaryKey typeId, string id) where T : Entity
        {
            Type type = schema.GetType(typeId);

            return (Lite<T>)Lite.Create(type, PrimaryKey.Parse(id, type));
        }

        public static MemberExpression peModified = Expression.Property(retriever, ReflectionTools.GetPropertyInfo((IRetriever me) => me.ModifiedState));

        public static ConstructorInfo ciKVPIntString = ReflectionTools.GetConstuctorInfo(() => new KeyValuePair<PrimaryKey, string>(1, ""));


        public static Action<IRetriever, Modifiable> resetModifiedAction;

        static CachedTableConstructor()
        {
            ParameterExpression modif = Expression.Parameter(typeof(Modifiable));

            resetModifiedAction = Expression.Lambda<Action<IRetriever, Modifiable>>(Expression.Assign(
                Expression.Property(modif, ReflectionTools.GetPropertyInfo((Modifiable me) => me.Modified)),
                CachedTableConstructor.peModified),
                CachedTableConstructor.retriever, modif).Compile();
        }




        static readonly MethodInfo miMixin = ReflectionTools.GetMethodInfo((Entity i) => i.Mixin<CorruptMixin>()).GetGenericMethodDefinition();
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

            mixBindings.Add(Expression.Call(retriever, miModifiablePostRetrieving.MakeGenericMethod(mixin.FieldType), mixParam));

            var mixBlock = Expression.Block(new[] { mixParam }, mixBindings);
            return mixBlock;
        }
    }
}
