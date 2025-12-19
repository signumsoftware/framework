using Signum.Utilities.Reflection;
using Signum.Engine.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using Signum.Engine.Maps;
using Signum.DynamicQuery.Tokens;
using System.Linq;
using Azure.Core;
using Signum.Utilities.ExpressionTrees;
using System.Collections.Generic;

namespace Signum.DynamicQuery;

public interface IDynamicInfo
{
    BuildExpressionContext Context { get; }
}


/// <typeparam name="T">Unraleted with the content, only with the original anonymous type </typeparam>
public class DQueryable<T> : IDynamicInfo
{
    
    public DQueryable(IQueryable query, BuildExpressionContext context)
    {
        this.Query = query;
        this.Context = context;
    }

    public IQueryable Query { get; private set; }
    public BuildExpressionContext Context { get; private set; }
}

/// <typeparam name="T">Unraleted with the content, only with the original anonymous type </typeparam>
public class DEnumerable<T> : IDynamicInfo
{
    public DEnumerable(IEnumerable collection, BuildExpressionContext context)
    {
        this.Collection = collection;
        this.Context = context;
    }

    public IEnumerable Collection { get; private set; }
    public BuildExpressionContext Context { get; private set; }
}

/// <typeparam name="T">Unraleted with the content, only with the original anonymous type</typeparam>
public class DEnumerableCount<T> : DEnumerable<T>
{
    public DEnumerableCount(IEnumerable collection, BuildExpressionContext context, int? totalElements) :
        base(collection, context)
    {
        this.TotalElements = totalElements;
    }

    public int? TotalElements { get; private set; }
}


public static class DQueryable
{
    #region ToDQueryable

    public static DQueryable<T> ToDQueryable<T>(this IQueryable<T> query, QueryDescription description)
    {
        ParameterExpression pe = Expression.Parameter(typeof(T));

        var dic = description.Columns.ToDictionary(
            cd => (QueryToken)new ColumnToken(cd, description.QueryName),
            cd => new ExpressionBox(Expression.PropertyOrField(pe, cd.Name).BuildLiteNullifyUnwrapPrimaryKey(cd.PropertyRoutes!)));

        return new DQueryable<T>(query, new BuildExpressionContext(typeof(T), pe, dic, null, null, null));
    }


    public static Task<DEnumerableCount<T>> AllQueryOperationsAsync<T>(this DQueryable<T> query, QueryRequest request, CancellationToken token, bool forConcat)
    {
        var selectColumn = request.Columns.Select(a => a.Token).ToHashSet();
        if (forConcat)
            selectColumn.AddRange(request.Orders.Select(a => a.Token));

        return query
            .SelectMany(request.Multiplications(), request.FullTextTableFilters())
            .Where(request.Filters)
            .OrderBy(request.Orders, request.Pagination)
            .Select(selectColumn)
            .TryPaginateAsync(forConcat ? request.Pagination.ToBigPage() : request.Pagination, request.SystemTime, token);


    }


    public static DEnumerableCount<T> AllQueryOperations<T>(this DQueryable<T> query, QueryRequest request, bool forCombine)
    {
        var selectColumn = request.Columns.Select(a => a.Token).ToHashSet();
        if (forCombine)
            selectColumn.AddRange(request.Orders.Select(a => a.Token));

        return query
            .SelectMany(request.Multiplications(), request.FullTextTableFilters())
            .Where(request.Filters)
            .OrderBy(request.Orders, request.Pagination)
            .Select(selectColumn)
            .TryPaginate(forCombine ? request.Pagination.ToBigPage() : request.Pagination, request.SystemTime);
    }

    #endregion

    #region Select

    public static IEnumerable<object?> SelectOne<T>(this DEnumerable<T> collection, QueryToken token)
    {
        var exp = Expression.Lambda(Expression.Convert(token.BuildExpression(collection.Context), typeof(object)), collection.Context.Parameter);

        return (IEnumerable<object?>)Untyped.Select(collection.Collection, exp.Compile());
    }

    public static IQueryable<object?> SelectOne<T>(this DQueryable<T> query, QueryToken token)
    {
        var exp = Expression.Lambda(Expression.Convert(token.BuildExpression(query.Context), typeof(object)), query.Context.Parameter);

        return (IQueryable<object?>)Untyped.Select(query.Query, exp);
    }

    public static DQueryable<T> Select<T>(this DQueryable<T> query, HashSet<QueryToken> columns)
    {
        if (columns.Any(c => c.HasNested() != null))
        {
            return SelectWithNestedQueries(query, columns);
        }

        var selector = SelectTupleConstructor(query.Context, columns, out BuildExpressionContext newContext);

        return new DQueryable<T>(Untyped.Select(query.Query, selector), newContext);
    }

    public static DEnumerable<T> Select<T>(this DEnumerable<T> collection, List<Column> columns)
    {
        return Select<T>(collection, new HashSet<QueryToken>(columns.Select(c => c.Token)));
    }

    public static DEnumerable<T> Select<T>(this DEnumerable<T> collection, HashSet<QueryToken> columns)
    {
        var selector = SelectTupleConstructor(collection.Context, columns, out BuildExpressionContext newContext);

        return new DEnumerable<T>(Untyped.Select(collection.Collection, selector.Compile()), newContext);
    }


    static LambdaExpression SelectTupleConstructor(BuildExpressionContext context, HashSet<QueryToken> tokens, out BuildExpressionContext newContext)
    {
        string str = tokens.Select(t => QueryUtils.CanColumn(t)).NotNull().ToString("\n");
        if (str.HasText())
            throw new ApplicationException(str);

        List<Expression> expressions = tokens.Select(t => t.BuildExpression(context, searchToArray: true)).ToList();
        Expression ctor = TupleReflection.TupleChainConstructor(expressions);

        var pe = Expression.Parameter(ctor.Type);

        newContext = new BuildExpressionContext(
                ctor.Type, pe,
                tokens.Select((t, i) => new
                {
                    Token = t,
                    Expr = TupleReflection.TupleChainProperty(pe, i)
                }).ToDictionary(t => t.Token!, t => new ExpressionBox(t.Expr)),
                context.Filters,
                context.Orders,
                context.Pagination);

        return Expression.Lambda(ctor, context.Parameter);
    }

    static DQueryable<T> SelectWithNestedQueries<T>(this DQueryable<T> query, HashSet<QueryToken> columns)
    {
        var selector = SelectTupleWithNestedQueriesConstructor(query.Context, columns, out BuildExpressionContext newContext);

        return new DQueryable<T>(Untyped.Select(query.Query, selector), newContext);
    }

    static LambdaExpression SelectTupleWithNestedQueriesConstructor(BuildExpressionContext context, HashSet<QueryToken> tokens, out BuildExpressionContext newContext)
    {
        string str = tokens.Select(t => new { t, error = QueryUtils.CanColumn(t) }).Where(a => a.error != null).ToString(a => a.t.FullKey() + ": " + a.error, "\n");
        if (str.HasText())
            throw new ApplicationException(str);

        var tokenGroups = tokens.GroupBy(a => a.HasNested()).ToList();

        var tree = TreeHelper.ToTreeC(tokenGroups, gr =>
        {
            if (gr.Key == null)
                return null;

            foreach (var parentKey in gr.Key.Parent!.Follow(a => a.Parent).OfType<CollectionNestedToken>())
            {
                var groups = tokenGroups.SingleOrDefault(a => object.Equals(a.Key, parentKey));
                if (groups != null)
                    return groups;
            }

            return tokenGroups.SingleEx(a => a.Key == null);

        }).SingleEx();

        var filters = (from f in context.Filters.EmptyIfNull()
                       let nt = f.GetDeepestNestedToken()
                       where nt != null
                       group f by nt into g
                       select KeyValuePair.Create(g.Key, g.ToList())).ToDictionaryEx();


        var orders = (from o in context.Orders.EmptyIfNull()
                      let nt = o.Token.HasNested()
                       where nt != null
                       group o by nt into g
                       select KeyValuePair.Create(g.Key, g.ToList())).ToDictionaryEx();

        return NestedQueryConstructor(context, tree, filters, orders, out newContext);
    }

    public static MethodInfo miToList = ReflectionTools.GetMethodInfo(() => Enumerable.Empty<int>().ToList()).GetGenericMethodDefinition();

    static LambdaExpression NestedQueryConstructor(BuildExpressionContext context, Node<IGrouping<CollectionNestedToken?, QueryToken>> node, 
        Dictionary<CollectionNestedToken, List<Filter>> filters,
        Dictionary<CollectionNestedToken, List<Order>> orders,
        out BuildExpressionContext newContext)
    {
        var simpleTokens = node.Value.ToList();

        List<Expression> expressions = simpleTokens.Select(t => t.BuildExpression(context, searchToArray: true)).ToList();

        List<BuildExpressionContext> subContext = new List<BuildExpressionContext>();
        foreach (var child in node.Children)
        {
            var cet = child.Value.Key!;

            var collectionToken = cet.Parent!;
            var colExpre = GetCollectionExpression(context, collectionToken, out EntityPropertyToken? eptML);

            var elementType = colExpre.Type.ElementType()!;

            var pe2 = Expression.Parameter(elementType, elementType.CleanType().Name.Substring(0, 1).ToLower());

            var subQueryCtx = new BuildExpressionContext(pe2.Type, pe2, new Dictionary<QueryToken, ExpressionBox>
            {
                [cet] = new ExpressionBox(
                        pe2.BuildLiteNullifyUnwrapPrimaryKey(new[] { cet.GetPropertyRoute() }.NotNull().ToArray()),
                        mlistElementRoute: eptML != null ? cet.GetPropertyRoute() : null
                    )
            },
            context.Filters,
            context.Orders,
            context.Pagination);

            var myFilters = filters.TryGetC(cet);
            if(myFilters != null)
            {
                LambdaExpression? predicate = GetPredicateExpression(subQueryCtx, myFilters, inMemory: false);
                if (predicate != null)
                {
                    colExpre = Expression.Call(Untyped.miWhereQ.MakeGenericMethod(elementType), colExpre, predicate);
                }
            }
            var myOrders = orders.TryGetC(cet);
            if(myOrders != null && context.Pagination is not Pagination.All) 
            {
                colExpre = ApplyOrders(colExpre, myOrders, subQueryCtx);
            }

            var subQueryConstructor = NestedQueryConstructor(subQueryCtx, child, filters, orders, out var newSubContext);

            var tupleType = subQueryConstructor.Body.Type;

            var miSelect = colExpre.Type.IsInstanceOfType(typeof(IQueryable<>)) ? OverloadingSimplifier.miSelectQ : OverloadingSimplifier.miSelectE;
            var select = Expression.Call(miSelect.MakeGenericMethod(pe2.Type!, tupleType), colExpre, subQueryConstructor);


            var toList = Expression.Call(miToList.MakeGenericMethod(tupleType), select);

            expressions.Add(toList);
            subContext.Add(newSubContext);
        }

        Expression ctor = TupleReflection.TupleChainConstructor(expressions);

        var pe = Expression.Parameter(ctor.Type);

        var replacements = simpleTokens.Select((t, i) => new
        {
            Token = t,
            Expr = TupleReflection.TupleChainProperty(pe, i)
        }).ToDictionary(t => t.Token!, t => new ExpressionBox(t.Expr));

        for (int i = 0; i < node.Children.Count; i++)
        {
            var childNode = node.Children[i];
            CollectionNestedToken collectionElementToken = childNode.Value.Key!;

            var exp = TupleReflection.TupleChainProperty(pe, i + simpleTokens.Count);
            replacements.Add(collectionElementToken, new ExpressionBox(exp, subQueryContext: subContext[i]));
        }

        newContext = new BuildExpressionContext(ctor.Type, pe, replacements, context.Filters, context.Orders, context.Pagination);

        return Expression.Lambda(ctor, context.Parameter);
    }

    static MethodInfo miSelectMany = ReflectionTools.GetMethodInfo(() => Enumerable.SelectMany<string, char>(null!, a => a)).GetGenericMethodDefinition();
    private static Expression GetCollectionExpression(BuildExpressionContext context, QueryToken collectionToken, out EntityPropertyToken? eptML)
    {
        var cetParent = collectionToken.Follow(a => a.Parent).OfType<CollectionElementToken>().FirstOrDefault();

        if (cetParent == null || context.Replacements.ContainsKey(cetParent))
        {
            eptML = MListElementPropertyToken.AsMListEntityProperty(collectionToken);

            var colExpre = eptML != null ? MListElementPropertyToken.BuildMListElements(eptML, context) :
                collectionToken.BuildExpression(context);

            return colExpre;
        }
        else //Like Entity.Invoices.Element.InvoiceLine.Element.Product without shouwing any other token of the InvoiceLine 
        {
            var subCollection = GetCollectionExpression(context, cetParent.Parent!, out EntityPropertyToken? otherEptMl);

            var elementType = subCollection.Type.ElementType()!;

            var pe = Expression.Parameter(elementType, elementType.CleanType().Name.Substring(0, 1).ToLower());

            var subQueryCtx = new BuildExpressionContext(pe.Type, pe, new Dictionary<QueryToken, ExpressionBox>
            {
                [cetParent] = new ExpressionBox(
                    pe.BuildLiteNullifyUnwrapPrimaryKey(new[] { cetParent.GetPropertyRoute() }.NotNull().ToArray()),
                    mlistElementRoute: otherEptMl != null ? cetParent.GetPropertyRoute() : null
                )
            }, 
            context.Filters, 
            context.Orders, 
            context.Pagination);

            eptML = MListElementPropertyToken.AsMListEntityProperty(collectionToken);

            var colExpre = eptML != null ? MListElementPropertyToken.BuildMListElements(eptML, subQueryCtx) :
                collectionToken.BuildExpression(subQueryCtx);

            var lambda = Expression.Lambda(colExpre, pe);

            var selectMany = Expression.Call(miSelectMany.MakeGenericMethod(elementType, colExpre.Type.ElementType()!), subCollection, lambda);

            return selectMany;
        }
    }

    public static DEnumerable<T> Concat<T>(this DEnumerable<T> collection, DEnumerable<T> other)
    {
        if (collection.Context.ElementType != other.Context.ElementType)
            throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\n Enumerable: {0}: \n Other:  {1}".FormatWith(
                collection.Context.ElementType.TypeName(),
                other.Context.ElementType.TypeName()));

        return new DEnumerable<T>(Untyped.Concat(collection.Collection, other.Collection, collection.Context.ElementType), collection.Context);
    }

    public static DEnumerableCount<T> Concat<T>(this DEnumerableCount<T> collection, DEnumerableCount<T> other)
    {
        if (collection.Context.ElementType != other.Context.ElementType)
            throw new InvalidOperationException("Enumerable's TupleType does not match Other's one.\n Enumerable: {0}: \n Other:  {1}".FormatWith(
                collection.Context.ElementType.TypeName(),
                other.Context.ElementType.TypeName()));

        return new DEnumerableCount<T>(Untyped.Concat(collection.Collection, other.Collection, collection.Context.ElementType), collection.Context, collection.TotalElements + other.TotalElements);
    }
    #endregion

    public static DEnumerable<T> ToDEnumerable<T>(this DQueryable<T> query)
    {
        return new DEnumerable<T>(Untyped.ToList(query.Query, query.Context.ElementType), query.Context);
    }

    public static DEnumerable<T> ToDEnumerable<T>(this IEnumerable<T> query, QueryDescription description)
    {
        ParameterExpression pe = Expression.Parameter(typeof(T));

        var dic = description.Columns.ToDictionary(
            cd => (QueryToken)new ColumnToken(cd, description.QueryName),
            cd => new ExpressionBox(Expression.PropertyOrField(pe, cd.Name).BuildLiteNullifyUnwrapPrimaryKey(cd.PropertyRoutes!)));

        return new DEnumerable<T>(query, new BuildExpressionContext(typeof(T), pe, dic, null, null, null));
    }

    public static DEnumerableCount<T> WithCount<T>(this DEnumerable<T> result, int? totalElements)
    {
        return new DEnumerableCount<T>(result.Collection, result.Context, totalElements);
    }

    public static async Task<DEnumerable<T>> ToDEnumerableAsync<T>(this DQueryable<T> query, CancellationToken token)
    {
        var list = await Untyped.ToListAsync(query.Query, token, query.Context.ElementType);
        return new DEnumerable<T>(list, query.Context);
    }

    #region SelectMany
    public static DQueryable<T> SelectMany<T>(this DQueryable<T> query, List<CollectionElementToken> elementTokens, List<FilterSqlServerFullText> fullTextTableFilters)
    {
        foreach (var cet in elementTokens)
        {
            query = query.SelectMany(cet);
        }

        foreach (var fttf in fullTextTableFilters)
        {
            query = query.JoinWithFullText(fttf);
        }

        return query;
    }

    public static DQueryable<T> JoinWithFullText<T>(this DQueryable<T> query, FilterSqlServerFullText fft)
    {
        if (!fft.IsTable)
            throw new InvalidOperationException("IsTable should be true");

        IQueryable<FullTextResultTable> innerQuery = GetFullTextTableQuery(fft, out ITable table);

        QueryToken tableToken = fft.Tokens.Select(t =>
        {
            if (table is TableMList)
                return t.Follow(a => a.Parent).OfType<CollectionElementToken>().FirstEx();

            return t.Follow(a => a.Parent).FirstOrDefault(a => a.Type.IsLite()) ?? query.Context.Replacements.SingleEx(a => a.Key.FullKey() == "Entity").Key;
        }).Distinct().SingleEx();

        var replacement = query.Context.Replacements.ToDictionary(); //Necessary for SubQueries IEnumerable

        var ftrt = Expression.Parameter(typeof(FullTextResultTable), "ftrt");

        var idToken = tableToken is CollectionElementToken ? MListElementPropertyToken.RowId(tableToken, MListElementPropertyToken.AsMListEntityProperty(tableToken.Parent!)!) :
             EntityPropertyToken.IdProperty(tableToken);

        var idExpression = UndoUnwrapPrimaryKey(idToken.BuildExpression(query.Context));


        var properties = replacement.Values.Select(box => box.RawExpression).And(Expression.Field(ftrt, nameof(FullTextResultTable.Rank))).ToList();
        var ctor = TupleReflection.TupleChainConstructor(properties);

        var join = Untyped.Join(query.Query, innerQuery,
            outerKeySelector: Expression.Lambda(idExpression, query.Context.Parameter),
            innerKeySelector: Expression.Lambda(Expression.Field(ftrt, nameof(FullTextResultTable.Key)), ftrt),
            resultSelector: Expression.Lambda(ctor, query.Context.Parameter, ftrt));

        var parameter = Expression.Parameter(ctor.Type);

        var newReplacements = replacement.Select((kvp, i) => KeyValuePair.Create(kvp.Key,
            new ExpressionBox(TupleReflection.TupleChainProperty(parameter, i),
            mlistElementRoute: kvp.Value.MListElementRoute)
        )).ToDictionary();


        var rank = TupleReflection.TupleChainProperty(parameter, replacement.Count);

        newReplacements.AddRange(fft.Tokens, keySelector: t => new FullTextRankToken(t), valueSelector: t => new ExpressionBox(rank, mlistElementRoute: null));

        var newContext = new BuildExpressionContext(ctor.Type, parameter, newReplacements, 
            query.Context.Filters, 
            query.Context.Orders, 
            query.Context.Pagination);

        return new DQueryable<T>(join, newContext);
    }

    private static Expression UndoUnwrapPrimaryKey(Expression expression)
    {

        if (expression is UnaryExpression u && u.NodeType == ExpressionType.Convert && u.Operand.Type == typeof(IComparable))
            expression = u.Operand;

        if (expression is MemberExpression me && me.Member.Name == nameof(PrimaryKey.Object))
            expression = me.Expression!;

        return expression;
    }

    private static IQueryable<FullTextResultTable> GetFullTextTableQuery(FilterSqlServerFullText fft, out ITable table)
    {
        var schema = Schema.Current;
        table = fft.Tokens.Select(a => a.GetPropertyRoute()!).Select(pr =>
        {
            var mle = pr.GetMListItemsRoute();
            return mle != null ? ((FieldMList)schema.Field(mle.Parent!)).TableMList : (ITable)schema.Table(pr.RootType);
        }).Distinct().SingleEx();
        var columns = fft.Tokens.Select(a => (IColumn)schema.Field(a.GetPropertyRoute()!)).Distinct().ToArray();

        var query = fft.Operation == FullTextFilterOperation.ComplexCondition ?
            FullTextSearch.ContainsTable(table, columns, fft.SearchCondition, null) :
            FullTextSearch.FreeTextTable(table, columns, fft.SearchCondition, null);

        if (fft.TableOuterJoin)
            return query.DefaultIfEmpty() as IQueryable<FullTextResultTable>;

        return query;
    }

    static MethodInfo miDefaultIfEmptyE = ReflectionTools.GetMethodInfo(() => Enumerable.Empty<string>().DefaultIfEmpty()).GetGenericMethodDefinition();
    static MethodInfo miEmptyIfNull = ReflectionTools.GetMethodInfo(() => Enumerable.Empty<string>().EmptyIfNull()).GetGenericMethodDefinition();

    public static DQueryable<T> SelectMany<T>(this DQueryable<T> query, CollectionElementToken cet)
    {
        SelectManyConstructor(query.Context, cet,
            out LambdaExpression collectionSelector,
            out LambdaExpression resultSelector,
            out BuildExpressionContext newContext);

        var newQuery = Untyped.SelectMany(query.Query, collectionSelector, resultSelector);

        return new DQueryable<T>(newQuery, newContext);
    }

    private static void SelectManyConstructor(BuildExpressionContext context, CollectionElementToken cet, out LambdaExpression collectionSelector, out LambdaExpression resultSelector, out BuildExpressionContext newContext)
    {
        var eptML = MListElementPropertyToken.AsMListEntityProperty(cet.Parent!);

        Type elementType = eptML != null ?
            MListElementPropertyToken.MListElementType(eptML) :
            cet.Parent!.Type.ElementType()!;

        var collectionSelectorBody = Expression.Call(miDefaultIfEmptyE.MakeGenericMethod(elementType),
               eptML != null ? MListElementPropertyToken.BuildMListElements(eptML, context) :
                cet.Parent!.BuildExpression(context));

        collectionSelector = Expression.Lambda(
            typeof(Func<,>).MakeGenericType(context.ElementType, typeof(IEnumerable<>).MakeGenericType(elementType)),
            collectionSelectorBody,
            context.Parameter);

        var elementParameter = Expression.Parameter(elementType);

        var replacement = context.Replacements.ToDictionary(); //Necessary for SubQueries IEnumerable

        var properties = replacement.Values.Select(box => box.RawExpression).And(elementParameter.BuildLite().Nullify()).ToList();

        var ctor = TupleReflection.TupleChainConstructor(properties);

        resultSelector = Expression.Lambda(ctor, context.Parameter, elementParameter);
        var parameter = Expression.Parameter(ctor.Type);

        var newReplacements = replacement.Select((kvp, i) => KeyValuePair.Create(kvp.Key,
            new ExpressionBox(TupleReflection.TupleChainProperty(parameter, i),
            mlistElementRoute: kvp.Value.MListElementRoute)
        )).ToDictionary();

        newReplacements.Add(cet, new ExpressionBox(
            TupleReflection.TupleChainProperty(parameter, replacement.Count),
            mlistElementRoute: eptML != null ? cet.GetPropertyRoute() : null
        ));

        newContext = new BuildExpressionContext(ctor.Type, parameter, newReplacements, context.Filters, context.Orders, context.Pagination);
    }

    #endregion

    #region Where

    public static DQueryable<T> Where<T>(this DQueryable<T> query, params Filter[] filters)
    {
        return Where(query, filters.NotNull().ToList());
    }

    public static DQueryable<T> Where<T>(this DQueryable<T> query, List<Filter> filters)
    {
        if (filters.Count == 0)
            return query;

        var simpleFilters = filters.Where(a => a.GetDeepestNestedToken() == null).ToList();
        LambdaExpression? predicate = GetPredicateExpression(query.Context, simpleFilters, inMemory: false);

        var context = query.Context;

        var newContext = new BuildExpressionContext(context.ElementType,
            context.Parameter,
            context.Replacements,
            context.Filters.EmptyIfNull().Concat(filters).ToList(),
            context.Orders,
            context.Pagination);

        var newQuery = predicate == null ? query.Query : Untyped.Where(query.Query, predicate);

        return new DQueryable<T>(newQuery, newContext);
    }

    public static DQueryable<T> Where<T>(this DQueryable<T> query, Expression<Func<object, bool>> filter)
    {
        return new DQueryable<T>(Untyped.Where(query.Query, filter), query.Context);
    }

    public static DEnumerable<T> Where<T>(this DEnumerable<T> collection, params Filter[] filters)
    {
        return Where(collection, filters.NotNull().ToList());
    }

    public static DEnumerable<T> Where<T>(this DEnumerable<T> collection, List<Filter> filters)
    {
        LambdaExpression? where = GetPredicateExpression(collection.Context, filters, inMemory: true);
        if (where == null)
            return collection;

        return new DEnumerable<T>(Untyped.Where(collection.Collection, where.Compile()), collection.Context);
    }

    static LambdaExpression? GetPredicateExpression(BuildExpressionContext context, List<Filter> filters, bool inMemory)
    {
        if (filters == null || filters.Count == 0)
            return null;

        string str = filters
            .SelectMany(f => f.GetAllFilters())
            .SelectMany(f => f.GetTokens())
            .Select(t => QueryUtils.CanFilter(t))
            .NotNull()
            .ToString("\n");

        if (str.HasText())
            throw new ApplicationException(str);

        Expression body = filters.Select(f => f.GetExpression(context, inMemory)).AggregateAnd();

        return Expression.Lambda(body, context.Parameter);
    }

    #endregion

    #region OrderBy



    public static DQueryable<T> OrderBy<T>(this DQueryable<T> query, List<Order> orders, Pagination? pagination)
    {
        string str = orders.Select(f => QueryUtils.CanOrder(f.Token)).NotNull().ToString("\n");
        if (str.HasText())
            throw new ApplicationException(str);

        var pairs = orders.Where(a => a.Token.HasNested() == null).Select(o => (
            lambda: QueryUtils.CreateOrderLambda(o.Token, query.Context),
            orderType: o.OrderType
        )).ToList();

        if (orders.Any(a => a.Token.HasNested() != null))
        {
            var ctx = query.Context;
            return new DQueryable<T>(Untyped.OrderBy(query.Query, pairs), new BuildExpressionContext(
                ctx.ElementType,
                ctx.Parameter,
                ctx.Replacements,
                filters: ctx.Filters,
                orders: ctx.Orders.EmptyIfNull().Concat(orders).ToList(),
                pagination: pagination));
        }

        return new DQueryable<T>(Untyped.OrderBy(query.Query, pairs), query.Context);
    }


    public static DEnumerable<T> OrderBy<T>(this DEnumerable<T> collection, List<Order> orders)
    {
        var pairs = orders.Where(a => a.Token.HasNested() == null).Select(o => (
          lambda: QueryUtils.CreateOrderLambda(o.Token, collection.Context),
          orderType: o.OrderType
        )).ToList();

        var orderedColl = Untyped.OrderBy(collection.Collection, pairs);

        var nestedOrders = orders.Where(a => a.Token.HasNested() != null).ToList();

        if (!nestedOrders.Any())
            return new DEnumerable<T>(orderedColl, collection.Context);

        var nestedOrdersDictionary = nestedOrders.GroupToDictionary(a => a.Token.HasNested()!);

        LambdaExpression selector = GetSelector(collection.Context, nestedOrdersDictionary);

        var nestedOrderedCol = Untyped.Select(orderedColl, selector.Compile());

        return new DEnumerable<T>(nestedOrderedCol, collection.Context);
    }

    static MethodInfo miSelect = ReflectionTools.GetMethodInfo(() => Enumerable.Select<int, long>(null!, a => a)).GetGenericMethodDefinition();
    private static LambdaExpression GetSelector(BuildExpressionContext context, Dictionary<CollectionNestedToken, List<Order>> nestedOrdersDictionary)
    {  
        return Expression.Lambda(
            TupleReflection.TupleChainConstructor(context.Replacements.Select(r =>
            {
                if (r.Value.SubQueryContext != null)
                {
                    var query = r.Value.RawExpression;
                    var type = query.Type.ElementType()!;

                    if (r.Value.SubQueryContext.Replacements.Any(a => a.Value.SubQueryContext != null))
                    {
                        var getSelector = GetSelector(r.Value.SubQueryContext!, nestedOrdersDictionary);
                        var miSel = miSelect.MakeGenericMethod(type, type);
                        query = Expression.Call(miSel, query, getSelector);
                    }

                    var orders = nestedOrdersDictionary.TryGetC((CollectionNestedToken)r.Key);

                    if (orders != null)
                    {
                        query = ApplyOrders(query, orders, r.Value.SubQueryContext!);
                    }

                    return Expression.Call(miToList.MakeGenericMethod(type), query);
                }
                else
                {
                    return r.Value.RawExpression;
                }
            })), context.Parameter);
    }

    internal static Expression ApplyOrders(Expression query, List<Order> orders, Tokens.BuildExpressionContext ctx)
    {
        var type = query.Type.ElementType()!;
        bool isQuery = typeof(IQueryable).IsAssignableFrom(query.Type);
        bool isFirst = true;
        foreach (var o in orders)
        {
            var mi = isQuery ?
                (isFirst ?
             (o.OrderType == OrderType.Ascending ? Untyped.miOrderByQ : Untyped.miOrderByDescendingQ) :
             (o.OrderType == OrderType.Ascending ? Untyped.miThenByQ : Untyped.miThenByDescendingQ)
                ) :
                (isFirst ?
             (o.OrderType == OrderType.Ascending ? Untyped.miOrderByE : Untyped.miOrderByDescendingE) :
             (o.OrderType == OrderType.Ascending ? Untyped.miThenByE : Untyped.miThenByDescendingE));

            if (isFirst)
                isFirst = false;

            var lambda = QueryUtils.CreateOrderLambda(o.Token, ctx);

            query = Expression.Call(mi.MakeGenericMethod(type, lambda.Body.Type), query, lambda);
        }

        return query;
    }

    public static DEnumerableCount<T> OrderBy<T>(this DEnumerableCount<T> collection, List<Order> orders)
    {
        var result = OrderBy<T>(new DEnumerable<T>(collection.Collection, collection.Context), orders);

        return new DEnumerableCount<T>(result.Collection, result.Context, collection.TotalElements);
    }

    #endregion

    #region Unique

    [return: MaybeNull]
    public static T Unique<T>(this IEnumerable<T> collection, UniqueType uniqueType)
    {
        return uniqueType switch
        {
            UniqueType.First => collection.First(),
            UniqueType.FirstOrDefault => collection.FirstOrDefault(),
            UniqueType.Single => collection.SingleEx(),
            UniqueType.SingleOrDefault => collection.SingleOrDefaultEx(),
            UniqueType.Only => collection.Only(),
            _ => throw new InvalidOperationException(),
        };
    }

    //[return: MaybeNull]
    public static Task<T> UniqueAsync<T>(this IQueryable<T> collection, UniqueType uniqueType, CancellationToken token)
    {
        return uniqueType switch
        {
            UniqueType.First => collection.FirstAsync(token),
            UniqueType.FirstOrDefault => collection.FirstOrDefaultAsync(token)!,
            UniqueType.Single => collection.SingleAsync(token),
            UniqueType.SingleOrDefault => collection.SingleOrDefaultAsync(token)!,
            UniqueType.Only => collection.Take(2).ToListAsync(token).ContinueWith(l => l.Result.Only()!),
            _ => throw new InvalidOperationException(),
        };
    }

    #endregion

    #region TryTake
    public static DQueryable<T> TryTake<T>(this DQueryable<T> query, int? num)
    {
        if (num.HasValue)
            return new DQueryable<T>(Untyped.Take(query.Query, num.Value, query.Context.ElementType), query.Context);
        return query;
    }

    public static DEnumerable<T> TryTake<T>(this DEnumerable<T> collection, int? num)
    {
        if (num.HasValue)
            return new DEnumerable<T>(Untyped.Take(collection.Collection, num.Value, collection.Context.ElementType), collection.Context);
        return collection;
    }
    #endregion

    #region TimeSerieSelectMany

    static MethodInfo miOverrideInExpression = ReflectionTools.GetMethodInfo(() => SystemTimeExtensions.OverrideSystemTime<int>(null!, null!)).GetGenericMethodDefinition();
    static ConstructorInfo ciAsOf= ReflectionTools.GetConstuctorInfo(() => new SystemTime.AsOf(DateTime.Now));

    public static DQueryable<T> SelectManyTimeSeries<T>(this DQueryable<T> query, SystemTimeRequest systemTime, HashSet<QueryToken> columns, List<Order> orders, List<Filter> timeSeriesFilter, Pagination? pagination)
    {
        var tokens = columns.Concat(orders.Select(a => a.Token))
            .Concat(timeSeriesFilter.SelectMany(a => a.GetTokens()))
            .ToHashSet();

        return query
            .OrderBy(orders.Where(a => a.Token is not TimeSeriesToken).ToList(), pagination)
            .Select(tokens.Where(t => t is not TimeSeriesToken).ToHashSet())
            .SelectManyTimeSeries(systemTime, tokens)
            .Where(timeSeriesFilter)
            .OrderBy(orders.ToList(), pagination)
            .Select(columns);

    }


    public static DQueryable<T> SelectManyTimeSeries<T>(this DQueryable<T> query, SystemTimeRequest systemTime, IEnumerable<QueryToken> tokens)
    {
        var st =
            QueryTimeSeriesLogic.GetDatesInRange(
                systemTime.startDate!.Value.ToUniversalTime(),
                systemTime.endDate!.Value.ToUniversalTime(),
                systemTime.timeSeriesUnit!.ToString()!,
                systemTime.timeSeriesStep!.Value);

        var pDate = Expression.Parameter(typeof(DateValue), "d");

        var collectionSelectorType = typeof(Func<,>).MakeGenericType(typeof(DateValue), typeof(IEnumerable<>).MakeGenericType(query.Query.ElementType));

        if (systemTime.timeSeriesMaxRowsPerStep == null)
            throw new InvalidOperationException("TimeSeriesMaxRowsPerStep is not set");

        var queryTake = Untyped.Take(query.Query, systemTime.timeSeriesMaxRowsPerStep.Value, query.Query.ElementType).Expression;

        var collectionSelector = Expression.Lambda(collectionSelectorType,
            Expression.Call(miOverrideInExpression.MakeGenericMethod(query.Query.ElementType),
            queryTake,
            Expression.New(ciAsOf, Expression.Field(pDate, nameof(DateValue.Date)))),
            pDate);

        string str = tokens.Select(t => QueryUtils.CanColumn(t)).NotNull().ToString("\n");
        if (str.HasText())
            throw new ApplicationException(str);

        List<Expression> expressions = tokens.Select(t => t is TimeSeriesToken ? Expression.Field(pDate, nameof(DateValue.Date)) : t.BuildExpression(query.Context, searchToArray: true)).ToList();
        Expression ctor = TupleReflection.TupleChainConstructor(expressions);

        var pe = Expression.Parameter(ctor.Type);

        var newContext = new BuildExpressionContext(
                ctor.Type, pe,
                tokens.Select((t, i) => new
                {
                    Token = t,
                    Expr = TupleReflection.TupleChainProperty(pe, i)
                }).ToDictionary(t => t.Token!, t => new ExpressionBox(t.Expr)),
                query.Context.Filters,
                query.Context.Orders,
                query.Context.Pagination);

        var resultSelector = Expression.Lambda(ctor, pDate, query.Context.Parameter);

        var newQuery = Untyped.SelectMany(st, collectionSelector, resultSelector);

        return new DQueryable<T>(newQuery, newContext);
    }
    #endregion


    #region TryPaginate

    public static async Task<DEnumerableCount<T>> TryPaginateAsync<T>(this DQueryable<T> query, Pagination pagination, SystemTimeRequest? systemTime, CancellationToken token)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var elemType = query.Context.ElementType;

        if (pagination is Pagination.All)
        {
            var allList = await Untyped.ToListAsync(query.Query, token, elemType);

            return new DEnumerableCount<T>(allList, query.Context, allList.Count);
        }
        else if (pagination is Pagination.Firsts top)
        {
            var topList = await Untyped.ToListAsync(Untyped.Take(query.Query, top.TopElements, elemType), token, elemType);

            return new DEnumerableCount<T>(topList, query.Context, null);
        }
        else if (pagination is Pagination.Paginate pag)
        {
            if (systemTime != null && systemTime.mode is SystemTimeMode.Between or SystemTimeMode.ContainedIn)  //Results multipy due to Joins, not easy to change LINQ provider because joins are delayed
            {
                var q = Untyped.OrderAlsoByKeys(query.Query, elemType);

                var list = await Untyped.ToListAsync(query.Query /*q maybe?*/, token, elemType);

                var elements = list;
                if (pag.CurrentPage != 1)
                    elements = Untyped.ToList(Untyped.Skip(elements, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType), elemType);

                elements = Untyped.ToList(Untyped.Take(elements, pag.ElementsPerPage, elemType), elemType);

                return new DEnumerableCount<T>(elements, query.Context, list.Count);
            }
            else
            {
                var q = Untyped.OrderAlsoByKeys(query.Query, elemType);

                if (pag.CurrentPage != 1)
                    q = Untyped.Skip(q, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType);

                q = Untyped.Take(q, pag.ElementsPerPage, elemType);

                var listTask = await Untyped.ToListAsync(q, token, elemType);
                var countTask = systemTime != null && systemTime.mode is SystemTimeMode.Between or SystemTimeMode.ContainedIn ?
                    (await Untyped.ToListAsync(query.Query, token, elemType)).Count : //Results multipy due to Joins, not easy to change LINQ provider because joins are delayed
                    await Untyped.CountAsync(query.Query, token, elemType);

                return new DEnumerableCount<T>(listTask, query.Context, countTask);
            }
        }

        throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name));
    }

    public static DEnumerableCount<T> TryPaginate<T>(this DQueryable<T> query, Pagination pagination, SystemTimeRequest? systemTime)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var elemType = query.Context.ElementType;

        if (pagination is Pagination.All)
        {
            var allList = Untyped.ToList(query.Query, elemType);

            return new DEnumerableCount<T>(allList, query.Context, allList.Count);
        }
        else if (pagination is Pagination.Firsts top)
        {
            var topList = Untyped.ToList(Untyped.Take(query.Query, top.TopElements, elemType), elemType);

            return new DEnumerableCount<T>(topList, query.Context, null);
        }
        else if (pagination is Pagination.Paginate pag)
        {
            if (systemTime != null && systemTime.mode is SystemTimeMode.Between or SystemTimeMode.ContainedIn)  //Results multipy due to Joins, not easy to change LINQ provider because joins are delayed
            {
                var q = Untyped.OrderAlsoByKeys(query.Query, elemType);

                var list = Untyped.ToList(query.Query /*q?*/, elemType);

                var elements = list;
                if (pag.CurrentPage != 1)
                    elements = Untyped.ToList(Untyped.Skip(elements, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType), elemType);

                elements = Untyped.ToList(Untyped.Take(elements, pag.ElementsPerPage, elemType), elemType);

                return new DEnumerableCount<T>(elements, query.Context, list.Count);
            }
            else
            {
                var q = Untyped.OrderAlsoByKeys(query.Query, elemType);

                if (pag.CurrentPage != 1)
                    q = Untyped.Skip(q, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType);

                q = Untyped.Take(q, pag.ElementsPerPage, elemType);

                var list = Untyped.ToList(q, elemType);
                var count = list.Count < pag.ElementsPerPage ? pag.ElementsPerPage :
                    Untyped.Count(query.Query, elemType);

                return new DEnumerableCount<T>(list, query.Context, count);
            }



        }

        throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name));
    }

    public static DEnumerableCount<T> TryPaginate<T>(this DEnumerable<T> collection, Pagination pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));


        var elemType = collection.Context.ElementType;

        if (pagination is Pagination.All)
        {
            var allList = Untyped.ToList(collection.Collection, elemType);

            return new DEnumerableCount<T>(allList, collection.Context, allList.Count);
        }
        else if (pagination is Pagination.Firsts top)
        {
            var topList = Untyped.ToList(Untyped.Take(collection.Collection, top.TopElements, elemType), elemType);

            return new DEnumerableCount<T>(topList, collection.Context, null);
        }
        else if (pagination is Pagination.Paginate pag)
        {
            int? totalElements = null;

            var q = collection.Collection;
            if (pag.CurrentPage != 1)
                q = Untyped.Skip(q, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType);

            q = Untyped.Take(q, pag.ElementsPerPage, elemType);

            var list = Untyped.ToList(q, elemType);

            if (list.Count < pag.ElementsPerPage && pag.CurrentPage == 1)
                totalElements = list.Count;

            return new DEnumerableCount<T>(list, collection.Context, totalElements ?? Untyped.Count(collection.Collection, elemType));
        }

        throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name));
    }

    public static DEnumerableCount<T> TryPaginate<T>(this DEnumerableCount<T> collection, Pagination pagination)
    {
        if (pagination == null)
            throw new ArgumentNullException(nameof(pagination));

        var elemType = collection.Context.ElementType;

        if (pagination is Pagination.All)
        {
            return new DEnumerableCount<T>(collection.Collection, collection.Context, collection.TotalElements);
        }
        else if (pagination is Pagination.Firsts top)
        {
            var topList = Untyped.ToList(Untyped.Take(collection.Collection, top.TopElements, elemType), elemType);

            return new DEnumerableCount<T>(topList, collection.Context, null);
        }
        else if (pagination is Pagination.Paginate pag)
        {
            var c = collection.Collection;
            if (pag.CurrentPage != 1)
                c = Untyped.Skip(c, (pag.CurrentPage - 1) * pag.ElementsPerPage, elemType);

            c = Untyped.Take(c, pag.ElementsPerPage, elemType);

            return new DEnumerableCount<T>(c, collection.Context, collection.TotalElements);
        }

        throw new InvalidOperationException("pagination type {0} not expexted".FormatWith(pagination.GetType().Name));
    }

    #endregion

    #region GroupBy

    static readonly GenericInvoker<Func<IEnumerable, Delegate, Delegate, IEnumerable>> giGroupByE =
        new((col, ks, rs) => (IEnumerable<object>)Enumerable.GroupBy<string, int, double>((IEnumerable<string>)col, (Func<string, int>)ks, (Func<int, IEnumerable<string>, double>)rs));
    public static DEnumerable<T> GroupBy<T>(this DEnumerable<T> collection, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens)
    {
        var rootKeyTokens = GetRootKeyTokens(keyTokens);

        var redundantKeyTokens = keyTokens.Except(rootKeyTokens).ToHashSet();

        var keySelector = KeySelector(collection.Context, rootKeyTokens);

        LambdaExpression resultSelector = ResultSelectSelectorAndContext(collection.Context, rootKeyTokens, redundantKeyTokens, aggregateTokens, keySelector.Body.Type, isQueryable: false, out BuildExpressionContext newContext);

        var resultCollection = giGroupByE.GetInvoker(collection.Context.ElementType, keySelector.Body.Type, resultSelector.Body.Type)(collection.Collection, keySelector.Compile(), resultSelector.Compile());

        return new DEnumerable<T>(resultCollection, newContext);
    }

    static MethodInfo miGroupByQ = ReflectionTools.GetMethodInfo(() => Queryable.GroupBy<string, int, double>((IQueryable<string>)null!, (Expression<Func<string, int>>)null!, (Expression<Func<int, IEnumerable<string>, double>>)null!)).GetGenericMethodDefinition();
    public static DQueryable<T> GroupBy<T>(this DQueryable<T> query, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens)
    {
        var rootKeyTokens = GetRootKeyTokens(keyTokens);

        var redundantKeyTokens = keyTokens.Except(rootKeyTokens).ToHashSet();

        var keySelector = KeySelector(query.Context, rootKeyTokens);

        LambdaExpression resultSelector = ResultSelectSelectorAndContext(query.Context, rootKeyTokens, redundantKeyTokens, aggregateTokens, keySelector.Body.Type, isQueryable: true, out BuildExpressionContext newContext);

        var resultQuery = query.Query.Provider.CreateQuery(Expression.Call(null, miGroupByQ.MakeGenericMethod(query.Context.ElementType, keySelector.Body.Type, resultSelector.Body.Type),
            new Expression[] { query.Query.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector) }));

        return new DQueryable<T>(resultQuery, newContext);
    }

    private static HashSet<QueryToken> GetRootKeyTokens(HashSet<QueryToken> keyTokens)
    {
        return keyTokens.Where(t => !keyTokens.Any(t2 => t2.Dominates(t))).ToHashSet();
    }


    static MethodInfo miFirstE = ReflectionTools.GetMethodInfo(() => Enumerable.First((IEnumerable<string>)null!)).GetGenericMethodDefinition();

    static LambdaExpression ResultSelectSelectorAndContext(BuildExpressionContext context, HashSet<QueryToken> rootKeyTokens, HashSet<QueryToken> redundantKeyTokens, HashSet<AggregateToken> aggregateTokens, Type keyTupleType, bool isQueryable, out BuildExpressionContext newContext)
    {
        Dictionary<QueryToken, Expression> resultExpressions = new Dictionary<QueryToken, Expression>();
        ParameterExpression pk = Expression.Parameter(keyTupleType, "key");
        ParameterExpression pe = Expression.Parameter(typeof(IEnumerable<>).MakeGenericType(context.ElementType), "e");

        resultExpressions.AddRange(rootKeyTokens.Select((kqt, i) => KeyValuePair.Create(kqt, TupleReflection.TupleChainProperty(pk, i))));

        if (redundantKeyTokens.Any())
        {
            if (isQueryable)
            {
                var tempContext = new BuildExpressionContext(keyTupleType, pk,
                    replacements: rootKeyTokens.Select((kqt, i) => KeyValuePair.Create(kqt, new ExpressionBox(TupleReflection.TupleChainProperty(pk, i), alreadyHidden: true))).ToDictionary(),
                    context.Filters,
                    context.Orders,
                    context.Pagination);

                resultExpressions.AddRange(redundantKeyTokens.Select(t => KeyValuePair.Create(t, t.BuildExpression(tempContext, searchToArray: true))));
            }
            else
            {
                var first = Expression.Call(miFirstE.MakeGenericMethod(typeof(object)), pe);

                resultExpressions.AddRange(redundantKeyTokens.Select(t =>
                {
                    var exp = t.BuildExpression(context, searchToArray: true);
                    var replaced = ExpressionReplacer.Replace(exp,
                    new Dictionary<ParameterExpression, Expression>
                    {
                        { context.Parameter, first }
                    });

                    return KeyValuePair.Create(t, replaced);
                }));
            }
        }

        resultExpressions.AddRange(aggregateTokens.Select(at => KeyValuePair.Create((QueryToken)at, BuildAggregateExpressionEnumerable(pe, at, context))));

        var resultConstructor = TupleReflection.TupleChainConstructor(resultExpressions.Values);

        ParameterExpression pg = Expression.Parameter(resultConstructor.Type, "gr");
        newContext = new BuildExpressionContext(resultConstructor.Type, pg,
            replacements: resultExpressions.Keys.Select((t, i) => KeyValuePair.Create(t, new ExpressionBox(TupleReflection.TupleChainProperty(pg, i)))).ToDictionary(),
            context.Filters, 
            context.Orders,
            context.Pagination);

        return Expression.Lambda(resultConstructor, pk, pe);
    }

    static LambdaExpression KeySelector(BuildExpressionContext context, HashSet<QueryToken> keyTokens)
    {
        var keySelector = Expression.Lambda(
          TupleReflection.TupleChainConstructor(keyTokens.Select(t => t.BuildExpression(context)).ToList()),
          context.Parameter);
        return keySelector;
    }

    static Expression BuildAggregateExpressionEnumerable(Expression collection, AggregateToken at, BuildExpressionContext context)
    {
        Type elementType = collection.Type.ElementType()!;

        if (at.AggregateFunction == AggregateFunction.Count && at.Parent == null)
            return Expression.Call(typeof(Enumerable), "Count", new[] { elementType }, new[] { collection });

        var body = at.Parent!.BuildExpression(context);

        if (at.AggregateFunction == AggregateFunction.Count)
        {
            if (at.FilterOperation.HasValue)
            {
                var condition = QueryUtils.GetCompareExpression(at.FilterOperation.Value, body.Nullify(), Expression.Constant(at.Value, body.Type.Nullify()));

                var lambda = Expression.Lambda(condition, context.Parameter);

                return Expression.Call(typeof(Enumerable), AggregateFunction.Count.ToString(), new[] { elementType }, new[] { collection, lambda });
            }
            else if (at.Distinct)
            {
                var lambda = Expression.Lambda(body, context.Parameter);

                var select = Expression.Call(typeof(Enumerable), "Select", new[] { elementType, body.Type }, new[] { collection, lambda });
                var distinct = Expression.Call(typeof(Enumerable), "Distinct", new[] { body.Type }, new[] { select });
                var param = Expression.Parameter(lambda.Body.Type);
                LambdaExpression notNull = Expression.Lambda(Expression.NotEqual(param, Expression.Constant(null, param.Type.Nullify())), param);
                var count = Expression.Call(typeof(Enumerable), "Count", new[] { body.Type }, new Expression[] { distinct, notNull });

                return count;
            }
            else
                throw new InvalidOperationException();
        }
        else
        {
            if (body.Type != at.Type)
                body = body.TryConvert(at.Type);

            var lambda = Expression.Lambda(body, context.Parameter);

            if (at.AggregateFunction == AggregateFunction.Min || at.AggregateFunction == AggregateFunction.Max)
                return Expression.Call(typeof(Enumerable), at.AggregateFunction.ToString(), new[] { elementType, lambda.Body.Type }, new[] { collection, lambda });

            return Expression.Call(typeof(Enumerable), at.AggregateFunction.ToString(), new[] { elementType }, new[] { collection, lambda });
        }
    }

    static Expression BuildAggregateExpressionQueryable(Expression collection, AggregateToken at, BuildExpressionContext context)
    {
        Type elementType = collection.Type.ElementType()!;

        if (at.AggregateFunction == AggregateFunction.Count)
            return Expression.Call(typeof(Queryable), "Count", new[] { elementType }, new[] { collection });

        var body = at.Parent!.BuildExpression(context);

        var type = at.Type;

        if (body.Type != type)
            body = body.TryConvert(type);

        var lambda = Expression.Lambda(body, context.Parameter);
        var quotedLambda = Expression.Quote(lambda);

        if (at.AggregateFunction == AggregateFunction.Min || at.AggregateFunction == AggregateFunction.Max)
            return Expression.Call(typeof(Queryable), at.AggregateFunction.ToString(), new[] { elementType, lambda.Body.Type }, new[] { collection, quotedLambda });

        return Expression.Call(typeof(Queryable), at.AggregateFunction.ToString(), new[] { elementType }, new[] { collection, quotedLambda });
    }

    static Expression BuildAggregateExpressionQueryableAsync(Expression collection, AggregateToken at, BuildExpressionContext context, CancellationToken token)
    {
        var tokenConstant = Expression.Constant(token);

        Type elementType = collection.Type.ElementType()!;

        if (at.AggregateFunction == AggregateFunction.Count)
            return Expression.Call(typeof(QueryableAsyncExtensions), "CountAsync", new[] { elementType }, new[] { collection, tokenConstant });

        var body = at.Parent!.BuildExpression(context);

        var type = at.AggregateFunction == AggregateFunction.Sum ? at.Type.UnNullify() : at.Type;

        if (body.Type != type)
            body = body.TryConvert(type);

        var lambda = Expression.Lambda(body, context.Parameter);
        var quotedLambda = Expression.Quote(lambda);

        if (at.AggregateFunction == AggregateFunction.Min || at.AggregateFunction == AggregateFunction.Max)
            return Expression.Call(typeof(QueryableAsyncExtensions), at.AggregateFunction.ToString() + "Async", new[] { elementType, lambda.Body.Type }, new[] { collection, quotedLambda, tokenConstant });

        return Expression.Call(typeof(QueryableAsyncExtensions), at.AggregateFunction.ToString() + "Async", new[] { elementType }, new[] { collection, quotedLambda, tokenConstant });
    }


    #endregion

    #region SimpleAggregate

    public static object? SimpleAggregate<T>(this DEnumerable<T> collection, AggregateToken simpleAggregate)
    {
        var expr = BuildAggregateExpressionEnumerable(Expression.Constant(collection.Collection), simpleAggregate, collection.Context);

        return Expression.Lambda<Func<object?>>(Expression.Convert(expr, typeof(object))).Compile()();
    }

    public static object? SimpleAggregate<T>(this DQueryable<T> query, AggregateToken simpleAggregate)
    {
        var expr = BuildAggregateExpressionQueryable(query.Query.Expression, simpleAggregate, query.Context);

        return Expression.Lambda<Func<object?>>(Expression.Convert(expr, typeof(object))).Compile()();
    }

    public static Task<object?> SimpleAggregateAsync<T>(this DQueryable<T> query, AggregateToken simpleAggregate, CancellationToken token)
    {
        var expr = BuildAggregateExpressionQueryableAsync(query.Query.Expression, simpleAggregate, query.Context, token);

        var func = (Func<Task>)Expression.Lambda(expr).Compile();

        var task = func();

        return CastTask<object?>(task);
    }
    public static async Task<T> CastTask<T>(this Task task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        await task.ConfigureAwait(false);

        object? result = task.GetType().GetProperty(nameof(Task<object>.Result))!.GetValue(task);
        return (T)result!;
    }

    #endregion

    public struct ExpandColumn<T> : IExpandColumn
    {
        public QueryToken Token { get; private set; }

        public readonly Func<Lite<Entity>, T> GetValue;
        public ExpandColumn(QueryToken token, Func<Lite<Entity>, T> getValue)
        {
            Token = token;
            GetValue = getValue;
        }

        Expression IExpandColumn.GetExpression(Expression entitySelector)
        {
            return Expression.Invoke(Expression.Constant(GetValue), entitySelector);
        }
    }

    public interface IExpandColumn
    {
        public QueryToken Token { get; }
        Expression GetExpression(Expression entitySelector);
    }

    public static DEnumerable<T> ReplaceColumns<T>(this DEnumerable<T> query, params IExpandColumn[] newColumns)
    {
        var entity = query.Context.Replacements.Single(a => a.Key.FullKey() == "Entity").Value.GetExpression();
        var newColumnsDic = newColumns.ToDictionary(a => a.Token, a => a.GetExpression(entity));

        List<QueryToken> tokens = query.Context.Replacements.Keys.Union(newColumns.Select(a => a.Token)).ToList();
        List<Expression> expressions = tokens.Select(t => newColumnsDic.TryGetC(t) ?? query.Context.Replacements.GetOrThrow(t).GetExpression()).ToList();
        Expression ctor = TupleReflection.TupleChainConstructor(expressions);

        var pe = Expression.Parameter(ctor.Type);

        var newContext = new BuildExpressionContext(
                ctor.Type, pe,
                tokens
                .Select((t, i) => new { Token = t, Expr = TupleReflection.TupleChainProperty(pe, i) })
                .ToDictionary(t => t.Token!, t => new ExpressionBox(t.Expr)), 
                query.Context.Filters, 
                query.Context.Orders, 
                query.Context.Pagination);

        var selector = Expression.Lambda(ctor, query.Context.Parameter);

        return new DEnumerable<T>(Untyped.Select(query.Collection, selector.Compile()), newContext);
    }

    public static ResultTable ToResultTable<T>(this DEnumerableCount<T> collection, QueryRequest req)
    {
        var isMultiKeyGrupping = req.GroupResults && req.Columns.Count(col => col.Token is not AggregateToken) >= 2;

        var resultColumns = collection.Context.Replacements.Where(a => a.Value.SubQueryContext == null).Select(kvp =>
        {
            var token = kvp.Key;

            var expression = Expression.Lambda(kvp.Value.GetExpression(), collection.Context.Parameter);

            var lambda = expression.Compile();

            var array = Untyped.ToArray(Untyped.Select(collection.Collection, lambda), expression.Body.Type);

            var rc = new ResultColumn(token, array);

            if (req.SystemTime == null && (token.Type.IsLite() || isMultiKeyGrupping && token is not AggregateToken) && token.HasToArray() == null)
                rc.CompressUniqueValues = true;

            return rc;
        }).ToList();


        var nestedResultTablesColumns = GetSubQueryColumns(collection.Collection, collection.Context, req.Pagination);

        return new ResultTable(resultColumns.Concat(nestedResultTablesColumns).ToArray(), collection.TotalElements, req.Pagination, req.GroupResults);
    }

    private static List<ResultColumn> GetSubQueryColumns(IEnumerable collection, BuildExpressionContext context, Pagination pagination)
    {
        return context.Replacements.Where(a => a.Value.SubQueryContext != null).Select(kvp =>
        {
            var body = Expression.Call(miToResultTableSubQuery,
                kvp.Value.GetExpression(),
                Expression.Constant((CollectionNestedToken)kvp.Key),
                Expression.Constant(kvp.Value.SubQueryContext!),
                Expression.Constant(pagination)
            );

            var expression = Expression.Lambda(body, context.Parameter);

            var lambda = expression.Compile();

            var array = Untyped.ToArray(Untyped.Select(collection, lambda), expression.Body.Type);

            var rc = new ResultColumn(kvp.Key, array);

            return rc;
        }).ToList();
    }

    public static MethodInfo miToResultTableSubQuery = ReflectionTools.GetMethodInfo(() => ToResultTableSubQuery(new int[0], null!, null!, null!));
    public static ResultTable ToResultTableSubQuery(IEnumerable collection, CollectionNestedToken nested,  BuildExpressionContext ctx, Pagination pagination)
    {

        var resultColumns = ctx.Replacements.Where(a => a.Value.SubQueryContext == null).Select(kvp =>
        {
            var expression = Expression.Lambda(kvp.Value.GetExpression(), ctx.Parameter);

            var lambda = expression.Compile();

            var array = Untyped.ToArray(Untyped.Select(collection, lambda), expression.Body.Type);

            var rc = new ResultColumn(kvp.Key, array);

            return rc;
        }).ToList();

        var subQueryColumns = GetSubQueryColumns(collection, ctx, pagination);

        return new ResultTable(resultColumns.Concat(subQueryColumns).ToArray(), null, pagination, isGroupResults: false);
    }
}
