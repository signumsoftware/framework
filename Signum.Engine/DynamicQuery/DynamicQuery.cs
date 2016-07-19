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
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Linq;
using Signum.Entities;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Services;
using Signum.Entities.Basics;
using DQ = Signum.Engine.DynamicQuery;

namespace Signum.Engine.DynamicQuery
{
    public class DynamicQueryBucket
    {
        public Lazy<IDynamicQueryCore> Core { get; private set; }

        public object QueryName { get; private set; }

        public Implementations EntityImplementations { get; private set; }

        public DynamicQueryBucket(object queryName, Func<IDynamicQueryCore> lazyQueryCore, Implementations entityImplementations)
        {
            if (queryName == null)
                throw new ArgumentNullException("queryName");

            if (lazyQueryCore == null)
                throw new ArgumentNullException("lazyQueryCore");

            this.QueryName = queryName;
            this.EntityImplementations = entityImplementations;

            this.Core = new Lazy<IDynamicQueryCore>(() =>
            {
                var core = lazyQueryCore();

                core.QueryName = QueryName;

                core.StaticColumns.Where(sc => sc.IsEntity).SingleEx(() => "Entity column on {0}".FormatWith(QueryUtils.GetQueryUniqueKey(QueryName)));

                core.EntityColumnFactory().Implementations = entityImplementations;

                var errors = core.StaticColumns.Where(sc => sc.Implementations == null && sc.Type.CleanType().IsIEntity()).ToString(a => a.Name, ", ");

                if (errors.HasText())
                    throw new InvalidOperationException("Column {0} of {1} does not have implementations deffined. Use Column extension method".FormatWith(errors, QueryUtils.GetQueryUniqueKey(QueryName)));

                return core;
            });
        }


        public QueryDescription GetDescription()
        {
            return Core.Value.GetQueryDescription();
        }
    }


    public interface IDynamicQueryCore
    {
        object QueryName { get; set; }
        ColumnDescriptionFactory[] StaticColumns { get; }
        Expression Expression { get; } //Optional

        ColumnDescriptionFactory EntityColumnFactory();
        QueryDescription GetQueryDescription();

        ResultTable ExecuteQuery(QueryRequest request);
        int ExecuteQueryCount(QueryCountRequest request);
        Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request);
        ResultTable ExecuteQueryGroup(QueryGroupRequest request);
    }

    public abstract class DynamicQueryCore<T> : IDynamicQueryCore
    {
        public object QueryName { get; set; }

        public ColumnDescriptionFactory[] StaticColumns { get; protected set; }

        public abstract ResultTable ExecuteQuery(QueryRequest request);
        public abstract int ExecuteQueryCount(QueryCountRequest request);
        public abstract Lite<Entity> ExecuteUniqueEntity(UniqueEntityRequest request);
        public abstract ResultTable ExecuteQueryGroup(QueryGroupRequest request);

        protected virtual ColumnDescriptionFactory[] InitializeColumns()
        {
            var result = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, null)).ToArray();

            return result;
        }

        public DynamicQueryCore<T> ColumnDisplayName<S>(Expression<Func<T, S>> column, Enum messageValue)
        {
            return this.Column(column, c => c.OverrideDisplayName = () => messageValue.NiceToString());
        }
      
        public DynamicQueryCore<T> ColumnDisplayName<S>(Expression<Func<T, S>> column, Func<string> messageValue)
        {
            return this.Column(column, c => c.OverrideDisplayName = messageValue);
        }

        public DynamicQueryCore<T> ColumnProperyRoutes<S>(Expression<Func<T, S>> column, params PropertyRoute[] routes)
        {
            return this.Column(column, c => c.PropertyRoutes = routes);
        }

        public DynamicQueryCore<T> Column<S>(Expression<Func<T, S>> column, Action<ColumnDescriptionFactory> change)
        {
            MemberInfo member = ReflectionTools.GetMemberInfo(column);
            ColumnDescriptionFactory col = StaticColumns.SingleEx(a => a.Name == member.Name);
            change(col);

            return this;
        }

        public ColumnDescriptionFactory EntityColumnFactory()
        {
            return StaticColumns.Where(c => c.IsEntity).SingleEx(() => "Entity column on {0}".FormatWith(QueryUtils.GetQueryUniqueKey(QueryName)));
        }

        public virtual Expression Expression
        {
            get { return null; }
        }

        public QueryDescription GetQueryDescription()
        {
            var entity = EntityColumnFactory();
            string allowed = entity.IsAllowed();
            if (allowed != null)
                throw new InvalidOperationException(
                    "Not authorized to see Entity column on {0} because {1}".FormatWith(QueryUtils.GetQueryUniqueKey(QueryName), allowed));

            var columns = StaticColumns.Where(f => f.IsAllowed() == null).Select(f => f.BuildColumnDescription()).ToList();

            return new QueryDescription(QueryName, columns);
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
        public DEnumerableCount(IEnumerable<object> collection, BuildExpressionContext context, int? totalElements) :
            base(collection, context)
        {
            this.TotalElements = totalElements;
        }

        public int? TotalElements {get; private set;}
    }

    public static class DynamicQuery
    {
        public static AutoDynamicQueryCore<T> Auto<T>(IQueryable<T> query)
        {
            return new AutoDynamicQueryCore<T>(query); 
        }

        public static ManualDynamicQueryCore<T> Manual<T>(Func<QueryRequest, QueryDescription, DEnumerableCount<T>> execute)
        {
            return new ManualDynamicQueryCore<T>(execute); 
        }


        #region ToDQueryable

        public static DQueryable<T> ToDQueryable<T>(this IQueryable<T> query, QueryDescription description)
        {
            ParameterExpression pe = Expression.Parameter(typeof(object));

            var dic = description.Columns.ToDictionary(
                cd => (QueryToken)new ColumnToken(cd, description.QueryName),
                cd => Expression.PropertyOrField(Expression.Convert(pe, typeof(T)), cd.Name).BuildLiteNulifyUnwrapPrimaryKey(cd.PropertyRoutes));

            return new DQueryable<T>(query.Select(a => (object)a), new BuildExpressionContext(typeof(T), pe, dic));
        }


        public static DEnumerableCount<T> AllQueryOperations<T>(this DQueryable<T> query, QueryRequest request)
        {
            return query
                .SelectMany(request.Multiplications)
                .Where(request.Filters)
                .OrderBy(request.Orders)
                .Select(request.Columns)
                .TryPaginate(request.Pagination);
        }

        #endregion 

        #region Select

        public static DQueryable<T> Select<T>(this DQueryable<T> query, List<Column> columns)
        {
            return Select<T>(query, new HashSet<QueryToken>(columns.Select(c => c.Token)));
        }

        public static DQueryable<T> Select<T>(this DQueryable<T> query, HashSet<QueryToken> columns)
        {
            BuildExpressionContext newContext; 
            var selector = TupleConstructor(query.Context, columns, out newContext);

            return new DQueryable<T>(query.Query.Select(selector), newContext);
        }

        public static DEnumerable<T> Select<T>(this DEnumerable<T> query, List<Column> columns)
        {
            return Select<T>(query, new HashSet<QueryToken>(columns.Select(c => c.Token)));
        }

        public static DEnumerable<T> Select<T>(this DEnumerable<T> collection, HashSet<QueryToken> columns)
        {
            BuildExpressionContext newContext;
            var selector = TupleConstructor(collection.Context, columns, out newContext);

            return new DEnumerable<T>(collection.Collection.Select(selector.Compile()), newContext);
        }

        public static DEnumerable<T> Concat<T>(this DEnumerable<T> collection, DEnumerable<T> other)
        {
            if (collection.Context.TupleType != other.Context.TupleType)
                throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\r\n Enumerable: {0}: \r\n Other:  {1}".FormatWith(
                    collection.Context.TupleType.TypeName(),
                    other.Context.TupleType.TypeName()));

            return new DEnumerable<T>(collection.Collection.Concat(other.Collection), collection.Context); 
        }

        public static DEnumerableCount<T> Concat<T>(this DEnumerableCount<T> collection, DEnumerableCount<T> other)
        {
            if (collection.Context.TupleType != other.Context.TupleType)
                throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\r\n Enumerable: {0}: \r\n Other:  {1}".FormatWith(
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

        public static DEnumerable<T> ToDEnumerable<T>(this DQueryable<T> query)
        {
            return new DEnumerable<T>(query.Query.ToList(), query.Context);
        }

        #region SelectMany
        public static DQueryable<T> SelectMany<T>(this DQueryable<T> query, List<CollectionElementToken> elementTokens)
        {
            foreach (var cet in elementTokens)
            {
                query = query.SelectMany(cet);
            }

            return query;
        }

        static MethodInfo miSelectMany = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().SelectMany(t => t.Namespace, (t, c) => t)).GetGenericMethodDefinition();
        static MethodInfo miDefaultIfEmptyE = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().AsEnumerable().DefaultIfEmpty()).GetGenericMethodDefinition();

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

        public static DQueryable<T> Where<T>(this DQueryable<T> query, Expression<Func<object, bool>> filter)
        {
            return new DQueryable<T>(query.Query.Where(filter), query.Context);
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

        public static DEnumerable<T> Where<T>(this DEnumerable<T> collection, Func<object, bool> filter)
        {
            return new DEnumerable<T>(collection.Collection.Where(filter).ToList(), collection.Context);
        }
        
        static Expression<Func<object, bool>> GetWhereExpression(BuildExpressionContext context, List<Filter> filters)
        {
            if (filters == null || filters.Count == 0)
                return null;

            string str = filters.Select(f => QueryUtils.CanFilter(f.Token)).NotNull().ToString("\r\n");
            if (str == null)
                throw new ApplicationException(str);

            FilterBuildExpressionContext filterContext = new FilterBuildExpressionContext(context);

            foreach (var f in filters)
            {
                f.GenerateCondition(filterContext);
            }

            Expression body = filterContext.Filters.Select(f => f.ToExpression(filterContext)).AggregateAnd();

            return Expression.Lambda<Func<object, bool>>(body, context.Parameter);
        }

        #endregion

        #region OrderBy

        static MethodInfo miOrderByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id).ThenBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miOrderByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderByDescending(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByDescendingQ = ReflectionTools.GetMethodInfo(() => Database.Query<TypeEntity>().OrderBy(t => t.Id).ThenByDescending(t => t.Id)).GetGenericMethodDefinition();

        public static DQueryable<T> OrderBy<T>(this DQueryable<T> query, List<Order> orders)
        {
            string str = orders.Select(f => QueryUtils.CanOrder(f.Token)).NotNull().ToString("\r\n");
            if (str == null)
                throw new ApplicationException(str);

            var pairs = orders.Select(o => Tuple.Create(
                     Expression.Lambda(OnAddaptForOrderBy(o.Token.BuildExpression(query.Context)), query.Context.Parameter),
                    o.OrderType)).ToList();

            return new DQueryable<T>(query.Query.OrderBy(pairs), query.Context);
        }

        static Expression OnAddaptForOrderBy(Expression exp)
        {
            foreach (var item in AddaptForOrderBy.GetInvocationListTyped())
            {
                exp = item(exp);
            }

            return exp;
        }

        public static Func<Expression, Expression> AddaptForOrderBy = e => e; 

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
                    Expression.Lambda(OnAddaptForOrderBy(o.Token.BuildExpression(collection.Context)), collection.Context.Parameter),
                   o.OrderType)).ToList();


            return new DEnumerable<T>(collection.Collection.OrderBy(pairs), collection.Context);
        }

        public static DEnumerableCount<T> OrderBy<T>(this DEnumerableCount<T> collection, List<Order> orders)
        {
            var pairs = orders.Select(o => Tuple.Create(
                    Expression.Lambda(OnAddaptForOrderBy(o.Token.BuildExpression(collection.Context)), collection.Context.Parameter),
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


        #region TryPaginate

        public static DEnumerableCount<T> TryPaginate<T>(this DQueryable<T> query, Pagination pagination)
        {
            if (pagination == null)
                throw new ArgumentNullException("pagination");

            if (pagination is Pagination.All)
            {
                var allList = query.Query.ToList();

                return new DEnumerableCount<T>(allList, query.Context, allList.Count);
            }
            else if(pagination is Pagination.Firsts)
            {
                var top = (Pagination.Firsts)pagination;
                var topList = query.Query.Take(top.TopElements).ToList();

                return new DEnumerableCount<T>(topList, query.Context, null); 
            }
            else if (pagination is Pagination.Paginate)
            {
                var pag = (Pagination.Paginate)pagination;
               
                int? totalElements = null;

                var q = query.Query.OrderAlsoByKeys();

                if (pag.CurrentPage != 1)
                    q = q.Skip((pag.CurrentPage - 1) * pag.ElementsPerPage);

                q = q.Take(pag.ElementsPerPage);

                var list = q.ToList();

                if (list.Count < pag.ElementsPerPage && pag.CurrentPage == 1)
                    totalElements = list.Count;

                return new DEnumerableCount<T>(list, query.Context, totalElements ?? query.Query.Count());
            }

            throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name)); 
        }

        public static DEnumerableCount<T> TryPaginate<T>(this DEnumerable<T> collection, Pagination pagination)
        {
            if (pagination == null)
                throw new ArgumentNullException("pagination");

            if (pagination is Pagination.All)
            {
                var allList = collection.Collection.ToList();

                return new DEnumerableCount<T>(allList, collection.Context, allList.Count);
            }
            else if (pagination is Pagination.Firsts)
            {
                var top = (Pagination.Firsts)pagination;
                var topList = collection.Collection.Take(top.TopElements).ToList();

                return new DEnumerableCount<T>(topList, collection.Context, null);
            }
            else if (pagination is Pagination.Paginate)
            {
                var pag = (Pagination.Paginate)pagination;

                int? totalElements = null;

                var q = collection.Collection;
                if (pag.CurrentPage != 1)
                    q = q.Skip((pag.CurrentPage - 1) * pag.ElementsPerPage);

                q = q.Take(pag.ElementsPerPage);

                var list = q.ToList();

                if (list.Count < pag.ElementsPerPage && pag.CurrentPage == 1)
                    totalElements = list.Count;

                return new DEnumerableCount<T>(list, collection.Context, totalElements ?? collection.Collection.Count());
            }

            throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name)); 
        }

        public static DEnumerableCount<T> TryPaginate<T>(this DEnumerableCount<T> collection, Pagination pagination)
        {
            if (pagination == null)
                throw new ArgumentNullException("pagination");

            if (pagination is Pagination.All)
            {
                return new DEnumerableCount<T>(collection.Collection, collection.Context, collection.TotalElements);
            }
            else if (pagination is Pagination.Firsts)
            {
                var top = (Pagination.Firsts)pagination;
                var topList = collection.Collection.Take(top.TopElements).ToList();

                return new DEnumerableCount<T>(topList, collection.Context, null);
            }
            else if (pagination is Pagination.Paginate)
            {
                var pag = (Pagination.Paginate)pagination;

                var c = collection.Collection;
                if (pag.CurrentPage != 1)
                    c = c.Skip((pag.CurrentPage - 1) * pag.ElementsPerPage);

                c = c.Take(pag.ElementsPerPage);

                return new DEnumerableCount<T>(c, collection.Context, collection.TotalElements);
            }

            throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name)); 
        }

        #endregion

#region GroupBy

        static GenericInvoker<Func<IEnumerable<object>, Delegate, Delegate, IEnumerable<object>>> giGroupByE =
            new GenericInvoker<Func<IEnumerable<object>, Delegate, Delegate, IEnumerable<object>>>(
                (col, ks, rs) => (IEnumerable<object>)Enumerable.GroupBy<string, int, double>((IEnumerable<string>)col, (Func<string, int>)ks, (Func<int, IEnumerable<string>, double>)rs));
        public static DEnumerable<T> GroupBy<T>(this DEnumerable<T> collection, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens)
        {
            var keySelector = KeySelector(collection.Context, keyTokens);

            BuildExpressionContext newContext;
            LambdaExpression resultSelector = ResultSelectSelectorAndContext(collection.Context, keyTokens, aggregateTokens, keySelector.Body.Type, out newContext);

            var resultCollection = giGroupByE.GetInvoker(typeof(object), keySelector.Body.Type, typeof(object))(collection.Collection, keySelector.Compile(), resultSelector.Compile());

            return new DEnumerable<T>(resultCollection, newContext);
        }

        static MethodInfo miGroupByQ = ReflectionTools.GetMethodInfo(() => Queryable.GroupBy<string, int, double>((IQueryable<string>)null, (Expression<Func<string, int>>)null, (Expression<Func<int, IEnumerable<string>, double>>)null)).GetGenericMethodDefinition();
        public static DQueryable<T> GroupBy<T>(this DQueryable<T> query, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens)
        {
            var keySelector = KeySelector(query.Context, keyTokens);

            BuildExpressionContext newContext;
            LambdaExpression resultSelector = ResultSelectSelectorAndContext(query.Context, keyTokens, aggregateTokens, keySelector.Body.Type, out newContext);

            var resultQuery = (IQueryable<object>)query.Query.Provider.CreateQuery<object>(Expression.Call(null, miGroupByQ.MakeGenericMethod(typeof(object), keySelector.Body.Type, typeof(object)),
                new Expression[] { query.Query.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector) }));

            return new DQueryable<T>(resultQuery, newContext);
        }

        static LambdaExpression ResultSelectSelectorAndContext(BuildExpressionContext context, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens, Type keyTupleType, out BuildExpressionContext newContext)
        {
            Dictionary<QueryToken, Expression> resultExpressions = new Dictionary<QueryToken, Expression>();
            ParameterExpression pk = Expression.Parameter(keyTupleType, "key");
            resultExpressions.AddRange(keyTokens.Select((kt, i) => KVP.Create(kt,
                TupleReflection.TupleChainProperty(pk, i))));

            ParameterExpression pe = Expression.Parameter(typeof(IEnumerable<object>), "e");
            resultExpressions.AddRange(aggregateTokens.Select(at => KVP.Create((QueryToken)at,
                BuildAggregateExpression(pe, at, context))));

            var resultConstructor = TupleReflection.TupleChainConstructor(resultExpressions.Values);

            ParameterExpression pg = Expression.Parameter(typeof(object), "gr");
            newContext = new BuildExpressionContext(resultConstructor.Type, pg,
                resultExpressions.Keys.Select((t, i) => KVP.Create(t, TupleReflection.TupleChainProperty(Expression.Convert(pg, resultConstructor.Type), i))).ToDictionary());

            return Expression.Lambda(Expression.Convert(resultConstructor, typeof(object)), pk, pe);
        }

        static LambdaExpression KeySelector(BuildExpressionContext context, HashSet<QueryToken> keyTokens)
        {
            var keySelector = Expression.Lambda(
              TupleReflection.TupleChainConstructor(keyTokens.Select(t => t.BuildExpression(context)).ToList()),
              context.Parameter);
            return keySelector;
        }

        static Expression BuildAggregateExpression(Expression collection, AggregateToken at, BuildExpressionContext context)
        {
            Type groupType = collection.Type.GetGenericInterfaces(typeof(IEnumerable<>)).SingleEx(() => "IEnumerable<T> implementations on {0}".FormatWith(collection.Type)).GetGenericArguments()[0];

            if (at.AggregateFunction == Signum.Entities.DynamicQuery.AggregateFunction.Count)
                return Expression.Call(typeof(Enumerable), "Count", new[] { groupType }, new[] { collection });

            var body = at.Parent.BuildExpression(context);

            var type = at.Type;

            if (body.Type != type)
                body = body.TryConvert(type);

            var lambda = Expression.Lambda(body, context.Parameter);

            if (at.AggregateFunction == Signum.Entities.DynamicQuery.AggregateFunction.Min || at.AggregateFunction == Signum.Entities.DynamicQuery.AggregateFunction.Max)
                return Expression.Call(typeof(Enumerable), at.AggregateFunction.ToString(), new[] { groupType, lambda.Body.Type }, new[] { collection, lambda });

            return Expression.Call(typeof(Enumerable), at.AggregateFunction.ToString(), new[] { groupType }, new[] { collection, lambda });
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

            return ToResultTable(array, columnAccesors, collection.TotalElements, req.Pagination);
        }

        public static ResultTable ToResultTable(this object[] result, List<Tuple<Column, LambdaExpression>> columnAccesors, int? totalElements,  Pagination pagination)
        {
            var columnValues = columnAccesors.Select(c => new ResultColumn(
                c.Item1,
                miGetValues.GetInvoker(c.Item1.Type)(result, c.Item2.Compile()))
             ).ToArray();

            return new ResultTable(columnValues, totalElements, pagination);
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
