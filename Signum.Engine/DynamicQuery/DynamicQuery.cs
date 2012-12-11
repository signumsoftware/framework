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
using Signum.Entities.Basics;

namespace Signum.Engine.DynamicQuery
{
    public interface IDynamicQuery
    {
        object QueryName { get; set; } 

        ColumnDescriptionFactory EntityColumn();
        QueryDescription GetDescription(object queryName);
        ResultTable ExecuteQuery(QueryRequest request);
        int ExecuteQueryCount(QueryCountRequest request);
        Lite ExecuteUniqueEntity(UniqueEntityRequest request);
        Expression Expression { get; } //Optional
        ResetLazy<ColumnDescriptionFactory[]> StaticColumns { get; } 
    }

    public abstract class DynamicQuery<T> : IDynamicQuery
    {
        object queryName; 
        public object QueryName
        {
            get { return queryName; }
            set
            {
                if (queryName != null)
                    throw new InvalidOperationException("The query {0} has already been registered with quey {1}".Formato(
                        QueryUtils.GetQueryUniqueKey(queryName), QueryUtils.GetQueryUniqueKey(value)));

                queryName = value;

                if (StaticColumns.IsValueCreated)
                    AssertColumns(StaticColumns.Value);
            }
        }

        private void AssertColumns(ColumnDescriptionFactory[] columns)
        {
            columns.Where(sc => sc.IsEntity).SingleEx(() => "Entity column not foundon {0}".Formato(QueryUtils.GetQueryUniqueKey(QueryName)));

            var errors =  columns.Where(sc => sc.Implementations == null && sc.Type.CleanType().IsIIdentifiable()).ToString(a=>a.Name, ", ");

            if (errors.HasText())
                throw new InvalidOperationException("Column {0} of {1} does not have implementations deffined. Use Column extension method".Formato(errors, QueryUtils.GetQueryUniqueKey(QueryName)));
        }

        public ResetLazy<ColumnDescriptionFactory[]> StaticColumns { get; private set; } 

        public abstract ResultTable ExecuteQuery(QueryRequest request);
        public abstract int ExecuteQueryCount(QueryCountRequest request);
        public abstract Lite ExecuteUniqueEntity(UniqueEntityRequest request);

        public DynamicQuery()
        {
            StaticColumns = new ResetLazy<ColumnDescriptionFactory[]>(() =>
            {
                using (HeavyProfiler.Log("InitColums"))
                {
                    var result = InitializeColumns();
                    if (QueryName != null)
                        AssertColumns(result);
                    return result;
                }
            });
        }

        protected virtual ColumnDescriptionFactory[] InitializeColumns()
        {
            var result = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, null)).ToArray();

            return result;
        }

        public QueryDescription GetDescription(object queryName)
        {
            return new QueryDescription
            {
                QueryName = queryName,
                Columns = GetColumnDescriptions()
            };
        }

        public List<ColumnDescription> GetColumnDescriptions()
        {
            var entity = StaticColumns.Value.Single(f => f.IsEntity);
            string allowed = entity.IsAllowed();
            if (allowed != null)
                throw new InvalidOperationException(
                    "Not authorized to see Entity column of {0} because {1}".Formato(QueryUtils.GetQueryUniqueKey(QueryName), allowed));

            return StaticColumns.Value.Where(f => f.IsAllowed() == null).Select(f => f.BuildColumnDescription()).ToList();
        }

        public DynamicQuery<T> Column<S>(Expression<Func<T, S>> column, Action<ColumnDescriptionFactory> change)
        {
            MemberInfo member = ReflectionTools.GetMemberInfo(column);
            ColumnDescriptionFactory col = StaticColumns.Value.SingleEx(a => a.Name == member.Name);
            change(col);

            return this;
        }

        public ColumnDescriptionFactory EntityColumn()
        {
            return StaticColumns.Value.Where(c => c.IsEntity).SingleEx(()=>"There's no Entity column");
        }

        public virtual Expression Expression
        {
            get { return null; }
        }

      
    }

    public interface IDynamicInfo
    {
        BuildExpressionContext Context { get; }
    }

    public class DQueryable<T> : IDynamicInfo
    {
        public DQueryable(IQueryable<object> query, BuildExpressionContext context)
        {
            this.Query= query;
            this.Context = context;
        }

        public IQueryable<object> Query{ get; private set; }
        public BuildExpressionContext Context { get; private set; }
    }

    public class DQueryableCount<T> : DEnumerable<T>
    {
        public DQueryableCount(IQueryable<object> query, BuildExpressionContext context, int totalElements) :
            base(query, context)
        {
            this.TotalElements = totalElements;
        }

        public int TotalElements { get; private set; }
    }

    public class DEnumerable<T> : IDynamicInfo
    {
        public DEnumerable(IEnumerable<object> collection, BuildExpressionContext context)
        {
            this.Collection = collection;
            this.Context = context;
        }

        public IEnumerable<object> Collection{ get; private set; }
        public BuildExpressionContext Context { get; private set; }
    }

    public class DEnumerableCount<T> : DEnumerable<T> 
    {
        public DEnumerableCount(IEnumerable<object> collection, BuildExpressionContext context, int totalElements) :
            base(collection, context)
        {
            this.TotalElements = totalElements;
        }

        public int TotalElements {get; private set;}
    }

    public static class DynamicQuery
    {
        public static DynamicQuery<T> ToDynamic<T>(this IQueryable<T> query)
        {
            return new AutoDynamicQuery<T>(query); 
        }

        public static DynamicQuery<T> Manual<T>(Func<QueryRequest, List<ColumnDescription>, DEnumerableCount<T>> execute)
        {
            return new ManualDynamicQuery<T>(execute); 
        }


        #region ToDQueryable

        public static DQueryable<T> ToDQueryable<T>(this IQueryable<T> query, List<ColumnDescription> descriptions)
        {
            ParameterExpression pe = Expression.Parameter(typeof(object));

            var dic = descriptions.ToDictionary(cd => (QueryToken)new ColumnToken(cd), cd => Expression.PropertyOrField(Expression.Convert(pe, typeof(T)), cd.Name).BuildLite());

            return new DQueryable<T>(query.Select(a => (object)a), new BuildExpressionContext(typeof(T), pe, dic));
        }

        #endregion 

        #region Select

        public static DQueryable<T> Select<T>(this DQueryable<T> query, List<Column> columns)
        {
            HashSet<QueryToken> tokens = new HashSet<QueryToken>(columns.Select(c => c.Token));

            BuildExpressionContext newContext; 
            var selector = TupleConstructor(query.Context, tokens, out newContext);

            return new DQueryable<T>(query.Query.Select(selector), newContext);
        }

        public static DEnumerable<T> Select<T>(this DEnumerable<T> collection, List<Column> columns)
        {
            HashSet<QueryToken> tokens = new HashSet<QueryToken>(columns.Select(c => c.Token));

            BuildExpressionContext newContext;
            var selector = TupleConstructor(collection.Context, tokens, out newContext);

            return new DEnumerable<T>(collection.Collection.Select(selector.Compile()), newContext);
        }

        public static DEnumerable<T> Concat<T>(this DEnumerable<T> collection, DEnumerable<T> other)
        {
            if (collection.Context.TupleType != other.Context.TupleType)
                throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\r\n Enumerable: {0}: \r\n Other:  {1}".Formato(
                    collection.Context.TupleType.TypeName(),
                    other.Context.TupleType.TypeName()));

            return new DEnumerable<T>(collection.Collection.Concat(other.Collection), collection.Context); 
        }

        public static DEnumerableCount<T> Concat<T>(this DEnumerableCount<T> collection, DEnumerableCount<T> other)
        {
            if (collection.Context.TupleType != other.Context.TupleType)
                throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\r\n Enumerable: {0}: \r\n Other:  {1}".Formato(
                    collection.Context.TupleType.TypeName(),
                    other.Context.TupleType.TypeName()));

            return new DEnumerableCount<T>(collection.Collection.Concat(other.Collection), collection.Context, collection.TotalElements + other.TotalElements);
        }


        static Expression<Func<object, object>> TupleConstructor(BuildExpressionContext context, HashSet<QueryToken> tokens, out BuildExpressionContext newContext)
        {
            string str = tokens.Select(t => QueryUtils.CanColumn(t)).NotNull().ToString("\r\n");
            if (str == null)
                throw new ApplicationException(str);
           
            List<Expression> expressions = tokens.Select(t => t.BuildExpression(context)).ToList();
            Expression ctor = TupleReflection.TupleChainConstructor(expressions);

            var pe = Expression.Parameter(typeof(object));

            newContext =  new BuildExpressionContext(
                    ctor.Type,pe, 
                    tokens.Select((t, i) => new { Token = t, Expr = TupleReflection.TupleChainProperty(Expression.Convert(pe, ctor.Type), i) }).ToDictionary(t => t.Token, t => t.Expr));

            return Expression.Lambda<Func<object, object>>(
                    (Expression)Expression.Convert(ctor, typeof(object)), context.Parameter);
        }
        
	    #endregion

        #region SelectMany
        public static DQueryable<T> SelectMany<T>(this DQueryable<T> query, List<CollectionElementToken> elementTokens)
        {
            foreach (var cet in elementTokens)
            {
                query = query.SelectMany(cet);
            }

            return query;
        }

        static MethodInfo miSelectMany = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().SelectMany(t => t.Namespace, (t, c) => t)).GetGenericMethodDefinition();
        static MethodInfo miDefaultIfEmptyE = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().AsEnumerable().DefaultIfEmpty()).GetGenericMethodDefinition();

        public static DQueryable<T> SelectMany<T>(this DQueryable<T> query, CollectionElementToken cet)
        {
            Type elementType = cet.Parent.Type.ElementType();

            var collectionSelector = Expression.Lambda(typeof(Func<,>).MakeGenericType(typeof(object), typeof(IEnumerable<>).MakeGenericType(elementType)),
                Expression.Call(miDefaultIfEmptyE.MakeGenericMethod(elementType),
                    cet.Parent.BuildExpression(query.Context)),
                query.Context.Parameter);

            var elementParameter = cet.CreateParameter();

            var properties = query.Context.Replacemens.Values.And(cet.CreateExpression(elementParameter));

            var ctor = TupleReflection.TupleChainConstructor(properties);

            var resultSelector = Expression.Lambda(Expression.Convert(ctor, typeof(object)), query.Context.Parameter, elementParameter);

            var resultQuery = query.Query.Provider.CreateQuery<object>(Expression.Call(null, miSelectMany.MakeGenericMethod(typeof(object), elementType, typeof(object)),
                new Expression[] { query.Query.Expression, Expression.Quote(collectionSelector), Expression.Quote(resultSelector) }));

            var parameter = Expression.Parameter(typeof(object));


            var newReplacements = query.Context.Replacemens.Keys.And(cet).Select((a, i) => new
            {
                Token = a,
                Expression = TupleReflection.TupleChainProperty(Expression.Convert(parameter, ctor.Type), i)
            }).ToDictionary(a => a.Token, a => a.Expression);

            return new DQueryable<T>(resultQuery,
                new BuildExpressionContext(ctor.Type, parameter, newReplacements));
        }

        #endregion

        #region Where

        public static DQueryable<T> Where<T>(this DQueryable<T> query, params Filter[] filters)
        {
            return Where(query, filters.NotNull().ToList()); 
        }

        public static DQueryable<T> Where<T>(this DQueryable<T> query, List<Filter> filters)
        {
            Expression<Func<object, bool>> where = GetWhereExpression(query.Context, filters);

            if (where == null)
                return query;

            return new DQueryable<T>(query.Query.Where(where), query.Context);
        }

        public static DEnumerable<T> Where<T>(this DEnumerable<T> collection, params Filter[] filters)
        {
            return Where(collection, filters.NotNull().ToList()); 
        }

        public static DEnumerable<T> Where<T>(this DEnumerable<T> collection, List<Filter> filters)
        {
            Expression<Func<object, bool>> where = GetWhereExpression(collection.Context, filters);

            if (where == null)
                return collection;

            return new DEnumerable<T>(collection.Collection.Where(where.Compile()), collection.Context);
        }

        static Expression<Func<object, bool>> GetWhereExpression(BuildExpressionContext context, List<Filter> filters)
        {
            if (filters == null || filters.Count == 0)
                return null;

            string str = filters.Select(f => QueryUtils.CanFilter(f.Token)).NotNull().ToString("\r\n");
            if (str == null)
                throw new ApplicationException(str);

            Expression body = filters.Select(f => f.GetCondition(context)).AggregateAnd();

            return Expression.Lambda<Func<object, bool>>(body, context.Parameter);
        }

        #endregion

        #region OrederBy

        static MethodInfo miOrderByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id).ThenBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miOrderByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderByDescending(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id).ThenByDescending(t => t.Id)).GetGenericMethodDefinition();

        public static DQueryable<T> OrderBy<T>(this DQueryable<T> query, List<Order> orders)
        {
            string str = orders.Select(f => QueryUtils.CanOrder(f.Token)).NotNull().ToString("\r\n");
            if (str == null)
                throw new ApplicationException(str);

            var pairs = orders.Select(o => Tuple.Create(
                     Expression.Lambda(o.Token.BuildExpression(query.Context), query.Context.Parameter),
                    o.OrderType)).ToList();

            return new DQueryable<T>(query.Query.OrderBy(pairs), query.Context);
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

        static GenericInvoker<Func<IEnumerable<object>, Delegate, IOrderedEnumerable<object>>> miOrderByE = new GenericInvoker<Func<IEnumerable<object>, Delegate, IOrderedEnumerable<object>>>((col, del) => col.OrderBy((Func<object, object>)del));
        static GenericInvoker<Func<IOrderedEnumerable<object>, Delegate, IOrderedEnumerable<object>>> miThenByE = new GenericInvoker<Func<IOrderedEnumerable<object>, Delegate, IOrderedEnumerable<object>>>((col, del) => col.ThenBy((Func<object, object>)del));
        static GenericInvoker<Func<IEnumerable<object>, Delegate, IOrderedEnumerable<object>>> miOrderByDescendingE = new GenericInvoker<Func<IEnumerable<object>, Delegate, IOrderedEnumerable<object>>>((col, del) => col.OrderByDescending((Func<object, object>)del));
        static GenericInvoker<Func<IOrderedEnumerable<object>, Delegate, IOrderedEnumerable<object>>> miThenByDescendingE = new GenericInvoker<Func<IOrderedEnumerable<object>, Delegate, IOrderedEnumerable<object>>>((col, del) => col.ThenByDescending((Func<object, object>)del));

        public static DEnumerable<T> OrderBy<T>(this DEnumerable<T> collection, List<Order> orders)
        {
            var pairs = orders.Select(o => Tuple.Create(
                    Expression.Lambda(o.Token.BuildExpression(collection.Context), collection.Context.Parameter),
                   o.OrderType)).ToList();


            return new DEnumerable<T>(collection.Collection.OrderBy(pairs), collection.Context);
        }

        public static DEnumerableCount<T> OrderBy<T>(this DEnumerableCount<T> collection, List<Order> orders)
        {
            var pairs = orders.Select(o => Tuple.Create(
                    Expression.Lambda(o.Token.BuildExpression(collection.Context), collection.Context.Parameter),
                   o.OrderType)).ToList();


            return new DEnumerableCount<T>(collection.Collection.OrderBy(pairs), collection.Context, collection.TotalElements);
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
            var mi = orderType == OrderType.Ascending ? miOrderByE : miOrderByDescendingE;

            return mi.GetInvoker(lambda.Type.GetGenericArguments())(collection, lambda.Compile());
        }

        static IOrderedEnumerable<object> ThenBy(this IOrderedEnumerable<object> collection, LambdaExpression lambda, OrderType orderType)
        {
            var mi = orderType == OrderType.Ascending ? miThenByE : miThenByDescendingE;

            return mi.GetInvoker(lambda.Type.GetGenericArguments())(collection, lambda.Compile());
        }

        #endregion

        #region Unique

        public static T Unique<T>(this IEnumerable<T> collection, UniqueType uniqueType)
        {
            switch (uniqueType)
            {
                case UniqueType.First: return collection.FirstEx();
                case UniqueType.FirstOrDefault: return collection.FirstOrDefault();
                case UniqueType.Single: return collection.SingleEx();
                case UniqueType.SingleOrDefault: return collection.SingleOrDefaultEx();
                case UniqueType.SingleOrMany: return collection.SingleOrMany();
                case UniqueType.Only: return collection.Only();
                default: throw new InvalidOperationException();
            }
        }

        #endregion

        #region TryTake
        public static DQueryable<T> TryTake<T>(this DQueryable<T> query, int? num)
        {
            if (num.HasValue)
                return new DQueryable<T>(query.Query.Take(num.Value), query.Context);
            return query;
        }

        public static DEnumerable<T> TryTake<T>(this DEnumerable<T> collection, int? num)
        {
            if (num.HasValue)
                return new DEnumerable<T>(collection.Collection.Take(num.Value), collection.Context);
            return collection;
        }
        #endregion

        #region TryPaginatePartial
        public static DEnumerableCount<T> TryPaginatePartial<T>(this DQueryable<T> query, int? maxElementIndex)
        {
            int count = query.Query.Count();

            if (maxElementIndex.HasValue)
                return new DEnumerableCount<T>(query.Query.Take(maxElementIndex.Value).ToList(), query.Context, count);

            return new DEnumerableCount<T>(query.Query.ToList(), query.Context, count);
        }

        public static DEnumerableCount<T> TryPaginatePartial<T>(this DEnumerable<T> collection, int? maxElementIndex)
        {
            int count = collection.Collection.Count(); 

            if (maxElementIndex.HasValue)
                return new DEnumerableCount<T>(collection.Collection.Take(maxElementIndex.Value), collection.Context, count);

            return new DEnumerableCount<T>(collection.Collection, collection.Context, count);
        }
        #endregion

        #region Paginate

        public static DEnumerableCount<T> TryPaginate<T>(this DQueryable<T> query, int elementsPerPage, int currentPage)
        {
            if (elementsPerPage == QueryRequest.AllElements)
            {
                var array = query.Query.ToArray();
                return new DEnumerableCount<T>(array, query.Context, array.Length);
            }

            if (currentPage <= 0)
                throw new InvalidOperationException("currentPage should be greater than zero");

            int totalElements = query.Query.Count();

            var q = query.Query;
            if (currentPage != 1)
                q = q.Skip((currentPage - 1) * elementsPerPage);

            q = q.Take(elementsPerPage);

            return new DEnumerableCount<T>(q.ToArray(), query.Context, totalElements);
        }

        public static DEnumerableCount<T> TryPaginate<T>(this DEnumerable<T> collection, int? elementsPerPage, int currentPage)
        {
            if (elementsPerPage == QueryRequest.AllElements)
                return new DEnumerableCount<T>(collection.Collection, collection.Context, collection.Collection.Count());

            if (currentPage <= 0)
                throw new InvalidOperationException("currentPage should be greater than zero");

            int totalElements = collection.Collection.Count();
            var c = collection.Collection;
            if (currentPage != 1)
                c = c.Skip((currentPage - 1) * elementsPerPage.Value);

            c = c.Take(elementsPerPage.Value);

            return new DEnumerableCount<T>(c, collection.Context, totalElements);
        }

        public static DEnumerableCount<T> TryPaginate<T>(this DEnumerableCount<T> collection, int elementsPerPage, int currentPage)
        {
            if (elementsPerPage == QueryRequest.AllElements)
                return new DEnumerableCount<T>(collection.Collection, collection.Context, collection.TotalElements);

            if (currentPage <= 0)
                throw new InvalidOperationException("currentPage should be greater than zero");

            var c = collection.Collection;
            if (currentPage != 1)
                c = c.Skip((currentPage - 1) * elementsPerPage);

            c = c.Take(elementsPerPage);

            return new DEnumerableCount<T>(c, collection.Context, collection.TotalElements);
        }

        #endregion


        public static Dictionary<string, Meta> QueryMetadata(IQueryable query)
        {
            return MetadataVisitor.GatherMetadata(query.Expression); 
        }


        public static ResultTable ToResultTable<T>(this DEnumerableCount<T> collection, QueryRequest req)
        {
            object[] array = collection.Collection as object[] ?? collection.Collection.ToArray();

            var columnAccesors = req.Columns.Select(c => Tuple.Create(c,
                Expression.Lambda(c.Token.BuildExpression(collection.Context), collection.Context.Parameter))).ToList();

            return ToResultTable(array, columnAccesors, collection.TotalElements, req.CurrentPage, req.ElementsPerPage);
        }

        public static ResultTable ToResultTable(this object[] result, List<Tuple<Column, LambdaExpression>> columnAccesors, int totalElements, int currentPage, int elementsPerPage)
        {
            var columnValues = columnAccesors.Select(c => new ResultColumn(
                c.Item1,
                miGetValues.GetInvoker(c.Item1.Type)(result, c.Item2.Compile()))
             ).ToArray();

            return new ResultTable(columnValues, totalElements, currentPage, elementsPerPage);
        }

        static GenericInvoker<Func<object[], Delegate, Array>> miGetValues = new GenericInvoker<Func<object[], Delegate, Array>>((objs, del) => GetValues<int>(objs, (Func<object, int>)del));
        static S[] GetValues<S>(object[] collection, Func<object, S> getter)
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
