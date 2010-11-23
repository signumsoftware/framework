using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Engine.Properties;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Linq;
using Signum.Entities;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Services;

namespace Signum.Engine.DynamicQuery
{
    public interface IDynamicQuery
    {
        ColumnDescriptionFactory EntityColumn();
        QueryDescription GetDescription(object queryName);
        ResultTable ExecuteQuery(QueryRequest request);
        int ExecuteQueryCount(QueryCountRequest request);
        Lite ExecuteUniqueEntity(UniqueEntityRequest request);
        Expression Expression { get; } //Optional
        ColumnDescriptionFactory[] StaticColumns { get; } 
    }

    public abstract class DynamicQuery<T> : IDynamicQuery
    {
        public ColumnDescriptionFactory[] StaticColumns { get; private set; } 

        public abstract ResultTable ExecuteQuery(QueryRequest request);
        public abstract int ExecuteQueryCount(QueryCountRequest request);
        public abstract Lite ExecuteUniqueEntity(UniqueEntityRequest request);

        protected void InitializeColumns(Func<MemberInfo, Meta> getMeta)
        {
            this.StaticColumns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, getMeta(e.MemberInfo))).ToArray();

            StaticColumns.Where(a => a.IsEntity).Single("Entity column not found"); 
        }

        public QueryDescription GetDescription(object queryName)
        {
            return new QueryDescription
            {
                QueryName = queryName,
                Columns = StaticColumns.Where(f => f.IsAllowed()).Select(f => f.BuildColumnDescription()).ToList()
            };
        }

        public DynamicQuery<T> Column<S>(Expression<Func<T, S>> column, Action<ColumnDescriptionFactory> change)
        {
            MemberInfo member = ReflectionTools.GetMemberInfo(column);
            ColumnDescriptionFactory col = StaticColumns.Single(a => a.Name == member.Name);
            change(col);

            return this;
        }

        public ColumnDescriptionFactory EntityColumn()
        {
            return StaticColumns.Where(c => c.IsEntity).Single("There's no Entity column");
        }

        public virtual Expression Expression
        {
            get { return null; }
        }
    }

    public interface IDynamicInfo
    {
        Type TupleType { get;  }
        Dictionary<QueryToken, int> TokenIndices { get; }
    }

    public class DQueryable<T> : IDynamicInfo
    {
        public DQueryable(IQueryable<object> query, Type tupletype, Dictionary<QueryToken, int> tokenIndices)
        {
            this.Query= query;
            this.TupleType = tupletype;
            this.TokenIndices = tokenIndices; 
        }

        public IQueryable<object> Query{ get; private set; }
        public Type TupleType { get; private set; }
        public Dictionary<QueryToken, int> TokenIndices { get; private set; }
    }

    public class DEnumerable<T> : IDynamicInfo
    {
        public DEnumerable(IEnumerable<object> collection, Type tupletype, Dictionary<QueryToken, int> tokenIndices)
        {
            this.Collection = collection;
            this.TupleType = tupletype;
            this.TokenIndices = tokenIndices; 
        }

        public IEnumerable<object> Collection{ get; private set; }
        public Type TupleType { get; private set; }
        public Dictionary<QueryToken, int> TokenIndices { get; private set; }
    }

    public static class DynamicQuery
    {
        public static DynamicQuery<T> ToDynamic<T>(this IQueryable<T> query)
        {
            return new AutoDynamicQuery<T>(query); 
        }

        public static DynamicQuery<T> Manual<T>(Func<QueryRequest, DEnumerable<T>> execute)
        {
            return new ManualDynamicQuery<T>(execute); 
        }

        #region SelectExpandable

        public static DQueryable<T> SelectDynamic<T>(this IQueryable<T> query, List<Column> columns, List<Order> orders)
        {
            HashSet<QueryToken> tokens = new HashSet<QueryToken>(columns.Select(c => c.Token));
            if (orders != null)
                tokens.AddRange(orders.Select(o => o.Token));

            TupleResult<T> result = TupleConstructor<T>(tokens);

            return new DQueryable<T>(query.Select(result.TupleConstructor), result.TupleType, result.TokenIndices);
        }

        public static DEnumerable<T> SelectExpandable<T>(this IEnumerable<T> query, List<Column> columns, List<Order> orders)
        {
            HashSet<QueryToken> tokens = new HashSet<QueryToken>(columns.Select(c => c.Token));
              if (orders != null)
                tokens.AddRange(orders.Select(o => o.Token));

              TupleResult<T> result = TupleConstructor<T>(tokens);

              return new DEnumerable<T>(query.Select(result.TupleConstructor.Compile()), result.TupleType, result.TokenIndices);
        }

        class TupleResult<T>
        {
            public Expression<Func<T, object>> TupleConstructor;
            public Type TupleType;
            public Dictionary<QueryToken, int> TokenIndices;
        }

        static TupleResult<T> TupleConstructor<T>(HashSet<QueryToken> tokens)
        {
            ParameterExpression param = Expression.Parameter(typeof(T), "p");
            List<Expression> expressions = tokens.Select(c => c.BuildExpression(param)).ToList();
            Expression ctor = TupleReflection.TupleChainConstructor(expressions);

            return new TupleResult<T>
            {
                TupleType = ctor.Type,
                TokenIndices = tokens.Select((t, i) => new { t, i }).ToDictionary(t => t.t, t => t.i),
                TupleConstructor = Expression.Lambda<Func<T, object>>(
                    (Expression)Expression.Convert(ctor, typeof(object)), param),
            };
        }

        public static DEnumerable<T> Concat<T>(this DEnumerable<T> enumerable, DEnumerable<T> other)
        {
            if (enumerable.TupleType != other.TupleType)
                throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\r\n Enumerable: {0}: \r\n Other:  {1}".Formato(
                    enumerable.TupleType.TypeName(),
                    other.TupleType.TypeName()));

            return new DEnumerable<T>(enumerable.Collection.Concat(other.Collection), enumerable.TupleType, other.TokenIndices); 
        }
        
	    #endregion

        #region Where
        static Expression<Func<T, bool>> GetWhereExpression<T>(List<Filter> filters)
        {
            if (filters == null || filters.Count == 0)
                return null;

            ParameterExpression pe = Expression.Parameter(typeof(T), "p");

            Expression body = filters.Select(f => f.GetCondition(pe)).Aggregate((e1, e2) => Expression.And(e1, e2));

            return Expression.Lambda<Func<T,  bool>>(body, pe);
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> query, params Filter[] filters)
        {
            return Where(query, filters.NotNull().ToList()); 
        }

        public static IQueryable<T> Where<T>(this IQueryable<T> query, List<Filter> filters)
        {
            var where = GetWhereExpression<T>(filters);

            if (where != null)
                return query.Where(where);

            return query; 
        }

        public static IEnumerable<T> Where<T>(this IEnumerable<T> sequence, params Filter[] filters)
        {
            return Where(sequence, filters.NotNull().ToList()); 
        }

        public static IEnumerable<T> Where<T>(this IEnumerable<T> sequence, List<Filter> filters)
        {
            var where = GetWhereExpression<T>(filters);

            if (where != null)
                return sequence.Where(where.Compile());
            return sequence;
        }

        #endregion

        #region OrederBy

        static MethodInfo miOrderByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id).ThenBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miOrderByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderByDescending(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id).ThenByDescending(t => t.Id)).GetGenericMethodDefinition();

        public static DQueryable<T> OrderBy<T>(this DQueryable<T> query, List<Order> orders)
        {
            var pairs = orders.Select(o => Tuple.Create(
                    TupleReflection.TupleChainPropertyLambda(query.TupleType, query.TokenIndices[o.Token]),
                    o.OrderType)).ToList();

            return new DQueryable<T>(query.Query.OrderBy(pairs), query.TupleType, query.TokenIndices);
        }

        public static IQueryable<object> OrderBy(this IQueryable<object> query, List<Tuple<LambdaExpression, OrderType>> orders)
        {
            if (orders == null || orders.Count == 0)
                return query;

            IOrderedQueryable<object> result = query.OrderBy(orders[0].Item1, orders[0].Item2);

            foreach (var order in orders.Skip(1))
            {
                result = result.ThenBy(order.Item1, order.Item2);
            }

            return result;
        }

        static IOrderedQueryable<object> OrderBy(this IQueryable<object> query, LambdaExpression lambda, OrderType orderType)
        {
            MethodInfo mi = (orderType == OrderType.Ascending ? miOrderByQ : miOrderByDescendingQ).MakeGenericMethod(lambda.Type.GetGenericArguments());

            return (IOrderedQueryable<object>)query.Provider.CreateQuery<object>(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
        }

        static IOrderedQueryable<object> ThenBy(this IOrderedQueryable<object> query, LambdaExpression lambda, OrderType orderType)
        {
            MethodInfo mi = (orderType == OrderType.Ascending ? miThenByQ : miThenByDescendingQ).MakeGenericMethod(lambda.Type.GetGenericArguments());

            return (IOrderedQueryable<object>)query.Provider.CreateQuery<object>(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
        }

        static GenericInvoker miOrderByE = GenericInvoker.Create(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id));
        static GenericInvoker miThenByE = GenericInvoker.Create(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id).ThenBy(t => t.Id));
        static GenericInvoker miOrderByDescendingE = GenericInvoker.Create(() => Database.Query<TypeDN>().ToList().OrderByDescending(t => t.Id));
        static GenericInvoker miThenByDescendingE = GenericInvoker.Create(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id).ThenByDescending(t => t.Id));

        public static DEnumerable<T> OrderBy<T>(this DEnumerable<T> collection, List<Order> orders)
        {
            var pairs = orders.Select(o => Tuple.Create(
                 TupleReflection.TupleChainPropertyLambda(collection.TupleType, collection.TokenIndices[o.Token]),
                 o.OrderType)).ToList();

            return new DEnumerable<T>(collection.Collection.OrderBy(pairs), collection.TupleType, collection.TokenIndices);
        }

        public static IEnumerable<object> OrderBy(this IEnumerable<object> collection, List<Tuple<LambdaExpression, OrderType>> orders)
        {
            if (orders == null || orders.Count == 0)
                return collection;

            IOrderedEnumerable<object> result = collection.OrderBy(orders[0].Item1, orders[0].Item2);

            foreach (var order in orders.Skip(1))
            {
                result = result.ThenBy(order.Item1, order.Item2);
            }

            return result;
        }

        static IOrderedEnumerable<object> OrderBy(this IEnumerable<object> collection, LambdaExpression lambda, OrderType orderType)
        {
            GenericInvoker mi = orderType == OrderType.Ascending ? miOrderByE : miOrderByDescendingE;

            return (IOrderedEnumerable<object>)mi.GetInvoker(lambda.Type.GetGenericArguments())(collection, lambda.Compile());
        }

        static IOrderedEnumerable<object> ThenBy(this IOrderedEnumerable<object> collection, LambdaExpression lambda, OrderType orderType)
        {
            GenericInvoker mi = orderType == OrderType.Ascending ? miThenByE : miThenByDescendingE;

            return (IOrderedEnumerable<object>)mi.GetInvoker(lambda.Type.GetGenericArguments())(collection, lambda.Compile());
        }

        #endregion

        #region Unique

        public static T Unique<T>(this IEnumerable<T> collection, UniqueType uniqueType)
        {
            switch (uniqueType)
            {
                case UniqueType.First: return collection.First();
                case UniqueType.FirstOrDefault: return collection.FirstOrDefault();
                case UniqueType.Single: return collection.Single();
                case UniqueType.SingleOrDefault: return collection.SingleOrDefault();
                case UniqueType.SingleOrMany: return collection.SingleOrMany();
                case UniqueType.Only: return collection.Only();
                default: throw new InvalidOperationException();
            }
        }

        #endregion

        #region TryTake
        public static IQueryable<T> TryTake<T>(this IQueryable<T> query, int? num)
        {
            if (num.HasValue)
                return query.Take(num.Value);
            return query;
        }

        public static IEnumerable<T> TryTake<T>(this IEnumerable<T> sequence, int? num)
        {
            if (num.HasValue)
                return sequence.Take(num.Value);
            return sequence;
        }

        public static DQueryable<T> TryTake<T>(this DQueryable<T> query, int? num)
        {
            if (num.HasValue)
                return new DQueryable<T>(query.Query.Take(num.Value), query.TupleType, query.TokenIndices);
            return query;
        }

        public static DEnumerable<T> TryTake<T>(this DEnumerable<T> sequence, int? num)
        {
            if (num.HasValue)
                return new DEnumerable<T>(sequence.Collection.Take(num.Value), sequence.TupleType, sequence.TokenIndices);
            return sequence;
        }
        #endregion

        public static Dictionary<string, Meta> QueryMetadata(IQueryable query)
        {
            return MetadataVisitor.GatherMetadata(query.Expression); 
        }

        public static DEnumerable<T> AsEnumerable<T>(this DQueryable<T> query)
        {
            return new DEnumerable<T>(query.Query.AsEnumerable(), query.TupleType, query.TokenIndices);
        }

        public static DEnumerable<T> ToArray<T>(this DQueryable<T> query)
        {
            return new DEnumerable<T>(query.Query.ToArray(), query.TupleType, query.TokenIndices);
        }

        public static DEnumerable<T> ToArray<T>(this DEnumerable<T> query)
        {
            return new DEnumerable<T>(query.Collection.ToArray(), query.TupleType, query.TokenIndices);
        }

        public static ResultTable ToResultTable<T>(this DEnumerable<T> collection, List<Column> columns)
        {
            object[] array = collection.Collection as object[] ?? collection.Collection.ToArray();

            return ToResultTable(array, columns.Select(c => Tuple.Create(c, 
                TupleReflection.TupleChainPropertyLambda(collection.TupleType, collection.TokenIndices[c.Token]))).ToList());
        }

        public static ResultTable ToResultTable(this object[] result, List<Tuple<Column, LambdaExpression>> columnAccesors)
        {
            var columnValues = columnAccesors.Select(c => new ResultColumn(
                c.Item1,
                (Array)miGetValues.GetInvoker(c.Item1.Type)(result, c.Item2.Compile()))
             ).ToArray();

            return new ResultTable(columnValues);
        }

        static GenericInvoker miGetValues = GenericInvoker.Create(() => GetValues<int>(null, null));
        static Array GetValues<S>(object[] collection, Func<object, S> getter)
        {
            S[] array = new S[collection.Length];
            for (int i = 0; i < collection.Length; i++)
            {
                array[i] = getter(collection[i]);
            }
            return array;
        }
    }
}
