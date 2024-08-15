using Signum.Utilities.Reflection;
using Signum.DynamicQuery.Tokens;

namespace Signum.DynamicQuery;

public class AutoDynamicQueryCore<T> : DynamicQueryCore<T>
{
    public IQueryable<T> Query { get; private set; }

    readonly Dictionary<string, Meta?> metas;



    public AutoDynamicQueryCore(IQueryable<T> query)
    {
        this.Query = query;

        metas = DynamicQueryCore.QueryMetadata(Query) ?? throw new InvalidOperationException("Query should be an anoynmous type");

        StaticColumns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
          .Select((e, i) => new ColumnDescriptionFactory(i, e.MemberInfo, metas[e.MemberInfo.Name])).ToArray();
    }

    public override ResultTable ExecuteQuery(QueryRequest request)
    {
        using (SystemTime.Override(request.SystemTime?.ToSystemTime()))
        {
            DQueryable<T> query = GetDQueryable(request, out var inMemoryOrders);

            var result = query.TryPaginate(request.Pagination, request.SystemTime);

            result = result.SelectManySubQueries();

            if (inMemoryOrders != null)
            {
                result = result.OrderBy(inMemoryOrders);
            }

            return result.ToResultTable(request);
        }
    }

    public override async Task<ResultTable> ExecuteQueryAsync(QueryRequest request, CancellationToken token)
    {
        using (SystemTime.Override(request.SystemTime?.ToSystemTime()))
        {
            DQueryable<T> query = GetDQueryable(request, out var inMemoryOrders);

            var result = await query.TryPaginateAsync(request.Pagination, request.SystemTime, token);

            result = result.SelectManySubQueries();

            if (inMemoryOrders != null)
            {
                result = result.OrderBy(inMemoryOrders);
            }

            return result.ToResultTable(request);
        }
    }

    public override ResultTable ExecuteQueryGroup(QueryRequest request)
    {
        using (SystemTime.Override(request.SystemTime?.ToSystemTime()))
        {
            DQueryable<T> query = GetDQueryableGroup(request, out var inMemoryOrders);

            var result = query.TryPaginate(request.Pagination, request.SystemTime);

            if (inMemoryOrders != null)
            {
                result = result.OrderBy(inMemoryOrders);
            }

            return result.ToResultTable(request);
        }
    }

    public override async Task<ResultTable> ExecuteQueryGroupAsync(QueryRequest request, CancellationToken token)
    {
        using (SystemTime.Override(request.SystemTime?.ToSystemTime()))
        {
            DQueryable<T> query = GetDQueryableGroup(request, out var inMemoryOrders);

            var result = await query.TryPaginateAsync(request.Pagination, request.SystemTime, token);

            if (inMemoryOrders != null)
            {
                result = result.OrderBy(inMemoryOrders);
            }

            return result.ToResultTable(request);
        }
    }

    private DQueryable<T> GetDQueryable(QueryRequest request, out List<Order>? inMemoryOrders)
    {
        if (!request.Columns.Where(c => c is _EntityColumn).Any())
            request.Columns.Insert(0, new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName));

        if (request.CanDoMultiplicationsInSubQueries())
        {
            var columnAndOrderTokens = request.Columns.Select(a => a.Token)
                 .Concat(request.Orders.Select(a => a.Token))
                 .Distinct()
                 .ToHashSet();

            inMemoryOrders = request.Orders;

            var query = Query
              .ToDQueryable(GetQueryDescription())
              .Where(request.Filters)
              .SelectWithSubQueries(columnAndOrderTokens);

            return query;
        }
        else
        {
            var simpleFilters = request.Filters.Where(f => !f.IsAggregate()).ToList();
            var aggregateFilters = request.Filters.Where(f => f.IsAggregate()).ToList();

            var query = Query
                .ToDQueryable(GetQueryDescription())
                .SelectMany(request.Multiplications(), request.FullTextTableFilters())
                .Where(request.Filters);

            if(request.SystemTime != null && request.SystemTime.mode == SystemTimeMode.TimeSeries)
            {
                inMemoryOrders = null;

                return query
                   .SelectManyTimeSeries(request.SystemTime, request.Columns, request.Orders);

            }
            else if (request.Pagination is Pagination.All)
            {
                var allColumns = request.Columns.Select(a => a.Token)
                    .Concat(request.Orders.Select(a => a.Token))
                    .Distinct()
                    .Select(t => new Column(t, null)).ToList();

                inMemoryOrders = request.Orders.ToList();

                return query.Select(allColumns);
            }
            else
            {
                inMemoryOrders = null;

                return query
                    .OrderBy(request.Orders)
                    .Select(request.Columns);
            }
        }
    }

    private DQueryable<T> GetDQueryableGroup(QueryRequest request, out List<Order>? inMemoryOrders)
    {
        var simpleFilters = request.Filters.Where(f => !f.IsAggregate()).ToList();
        var aggregateFilters = request.Filters.Where(f => f.IsAggregate()).ToList();

        var keys = request.Columns.Select(t => t.Token).Where(t => t is not AggregateToken && t is not TimeSeriesToken).ToHashSet();

        var allAggregates = request.AllTokens().OfType<AggregateToken>().ToHashSet();

        DQueryable<T> query = Query
            .ToDQueryable(GetQueryDescription())
            .SelectMany(request.Multiplications(), request.FullTextTableFilters())
            .Where(simpleFilters)
            .GroupBy(keys, allAggregates)
            .Where(aggregateFilters);

        if(request.SystemTime != null && request.SystemTime.mode == SystemTimeMode.TimeSeries)
        {
            inMemoryOrders = null;

            return query
                .SelectManyTimeSeries(request.SystemTime, request.Columns, request.Orders);

        }
        else if (request.Pagination is Pagination.All)
        {
            inMemoryOrders = request.Orders.ToList();
            return query;
        }
        else
        {
            inMemoryOrders = null;
            return query.OrderBy(request.Orders);
        }
    }

    public override object? ExecuteQueryValue(QueryValueRequest request)
    {
        using (SystemTime.Override(request.SystemTime))
        {
            var query = Query.ToDQueryable(GetQueryDescription())
            .SelectMany(request.Multiplications(), request.FullTextTableFilters())
            .Where(request.Filters);

            if (request.ValueToken == null)
                return Untyped.Count(query.Query, query.Context.ElementType);

            if (request.ValueToken is AggregateToken at)
                return query.SimpleAggregate(at);

            return query.SelectOne(request.ValueToken).Unique(UniqueType.SingleOrDefault);
        }
    }

    public override async Task<object?> ExecuteQueryValueAsync(QueryValueRequest request, CancellationToken token)
    {
        using (SystemTime.Override(request.SystemTime))
        {
            var query = Query.ToDQueryable(GetQueryDescription())
            .SelectMany(request.Multiplications(), request.FullTextTableFilters())
            .Where(request.Filters);

            if (request.ValueToken == null)
                return await Untyped.CountAsync(query.Query, token, query.Context.ElementType);

            if (request.ValueToken is AggregateToken at)
                return await query.SimpleAggregateAsync(at, token);

            if (request.MultipleValues)
                return await query.SelectOne(request.ValueToken).ToListAsync(token);

            return await query.SelectOne(request.ValueToken).UniqueAsync(UniqueType.SingleOrDefault, token);
        }
    }

    public override Lite<Entity>? ExecuteUniqueEntity(UniqueEntityRequest request)
    {
        var ex = new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName);

        DQueryable<T> orderQuery = Query
            .ToDQueryable(GetQueryDescription())
            .SelectMany(request.Multiplications(), request.FullTextTableFilters())
            .Where(request.Filters)
            .OrderBy(request.Orders);

        var result = orderQuery
            .SelectOne(ex.Token)
            .Unique(request.UniqueType);

        return (Lite<Entity>?)result;
    }

    public override async Task<Lite<Entity>?> ExecuteUniqueEntityAsync(UniqueEntityRequest request, CancellationToken token)
    {
        var ex = new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName);

        DQueryable<T> orderQuery = Query
            .ToDQueryable(GetQueryDescription())
            .SelectMany(request.Multiplications(), request.FullTextTableFilters())
            .Where(request.Filters)
            .OrderBy(request.Orders);

        var result = await orderQuery
            .SelectOne(ex.Token)
            .UniqueAsync(request.UniqueType, token);

        return (Lite<Entity>?)result;
    }

    public override IQueryable<Lite<Entity>> GetEntitiesLite(QueryEntitiesRequest request)
    {
        var ex = new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName);

        DQueryable<T> query = Query
         .ToDQueryable(GetQueryDescription())
         .SelectMany(request.Multiplications(), request.FullTextTableFilters())
         .OrderBy(request.Orders)
         .Where(request.Filters)
         .Select(new List<Column> { ex });

        var result = (IQueryable<Lite<Entity>>)Untyped.Select(query.Query, query.Context.GetEntitySelector());

        if (request.Multiplications().Any())
            result = result.Distinct();

        return result.TryTake(request.Count);
    }

    public override IQueryable<Entity> GetEntitiesFull(QueryEntitiesRequest request)
    {
        var ex = new _EntityColumn(EntityColumnFactory().BuildColumnDescription(), QueryName);

        DQueryable<T> query = Query
         .ToDQueryable(GetQueryDescription())
         .SelectMany(request.Multiplications(), request.FullTextTableFilters())
         .OrderBy(request.Orders)
         .Where(request.Filters)
         .Select(new List<Column> { ex });

        var result = (IQueryable<Entity>)Untyped.Select(query.Query, query.Context.GetEntityFullSelector());

        if (request.Multiplications().Any())
            result = result.Distinct();

        return result.TryTake(request.Count);
    }

    public override Expression? Expression
    {
        get { return Query.Expression; }
    }
}
