using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signum.API.Json;
public class FilterJsonConverter : JsonConverter<FilterTS>
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(FilterTS).IsAssignableFrom(objectType);
    }

    public override void Write(Utf8JsonWriter writer, FilterTS value, JsonSerializerOptions options)
    {
        if (value is FilterConditionTS fc)
            JsonSerializer.Serialize(writer, fc, options);
        else if (value is FilterGroupTS fg)
            JsonSerializer.Serialize(writer, fg, options);
        else
            throw new UnexpectedValueException(value);
    }

    public override FilterTS? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using (var doc = JsonDocument.ParseValue(ref reader))
        {
            var elem = doc.RootElement;

            if (elem.TryGetProperty("operation", out var oper))
            {
                return new FilterConditionTS
                {
                    token = elem.GetProperty("token").GetString()!,
                    operation = oper.GetString()!.ToEnum<FilterOperation>(),
                    value = elem.TryGetProperty("value", out var val) ? val.ToObject<object>(options) : null,
                };
            }

            if (elem.TryGetProperty("groupOperation", out var groupOper))
                return new FilterGroupTS
                {
                    groupOperation = groupOper.GetString()!.ToEnum<FilterGroupOperation>(),
                    token = elem.TryGetProperty("token", out var token) ? token.GetString() : null,
                    filters = elem.GetProperty("filters").EnumerateArray().Select(a => a.ToObject<FilterTS>()!).ToList()
                };

            throw new InvalidOperationException("Impossible to determine type of filter");
        }
    }
}

[JsonConverter(typeof(FilterJsonConverter))]
public abstract class FilterTS
{
    public abstract Filter ToFilter(QueryDescription qd, bool canAggregate, JsonSerializerOptions jsonSerializerOptions, bool canTimeSeries);

    public static FilterTS FromFilter(Filter filter)
    {
        if (filter is FilterCondition fc)
            return new FilterConditionTS
            {
                token = fc.Token.FullKey(),
                operation = fc.Operation,
                value = fc.Value
            };

        if (filter is FilterGroup fg)
            return new FilterGroupTS
            {
                token = fg.Token?.FullKey(),
                groupOperation = fg.GroupOperation,
                filters = fg.Filters.Select(f => FromFilter(f)).ToList(),
            };

        throw new UnexpectedValueException(filter);
    }
}

public class FilterConditionTS : FilterTS
{
    public required string token;
    public FilterOperation operation;
    public object? value;

    public override Filter ToFilter(QueryDescription qd, bool canAggregate, JsonSerializerOptions jsonSerializerOptions, bool canTimeSeries)
    {
        var options = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | (canAggregate ? SubTokensOptions.CanAggregate : 0) | (canTimeSeries ? SubTokensOptions.CanTimeSeries : 0);
        var parsedToken = QueryUtils.Parse(token, qd, options);
        var expectedValueType = FilterCondition.GetValueType(parsedToken, operation);

        object? val;
        try
        {
            val = value is JsonElement jtok ?
                 jtok.ToObject(expectedValueType, jsonSerializerOptions) :
                 value;
        }
        catch(JsonException)
        {
            throw new InvalidOperationException("Invalid value when filtering by " + token.ToString());
        }

        if (val is DateTime dt)
        {
            var kind = parsedToken.DateTimeKind;
            val = dt.ToKind(kind);
        }
        else if (val is ObservableCollection<DateTime?> col)
        {
            var kind = parsedToken.DateTimeKind;
            val = col.Select(dt => dt?.ToKind(kind)).ToObservableCollection();
        }

        return new FilterCondition(parsedToken, operation, val);
    }

    public override string ToString() => $"{token} {operation} {value}";
}

public class FilterGroupTS : FilterTS
{
    public FilterGroupOperation groupOperation;
    public string? token;
    public required List<FilterTS> filters;

    public override Filter ToFilter(QueryDescription qd, bool canAggregate, JsonSerializerOptions jsonSerializerOptions, bool canTimeSeries)
    {
        var options = SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | 
            (canAggregate ? SubTokensOptions.CanAggregate : 0) | (canTimeSeries ? SubTokensOptions.CanTimeSeries : 0);

        var parsedToken = token == null ? null : QueryUtils.Parse(token, qd, options);

        var parsedFilters = filters.Select(f => f.ToFilter(qd, canAggregate, jsonSerializerOptions, canTimeSeries)).ToList();

        return new FilterGroup(groupOperation, parsedToken, parsedFilters);
    }
}

public class ColumnTS
{
    public required string token;
    public string? displayName;

    public Column ToColumn(QueryDescription qd, bool canAggregate, bool canTimeSeries)
    {
        var queryToken = QueryUtils.Parse(token, qd, SubTokensOptions.CanElement | SubTokensOptions.CanToArray | SubTokensOptions.CanSnippet |
            (canAggregate ? SubTokensOptions.CanAggregate : SubTokensOptions.CanOperation | SubTokensOptions.CanManual) |
            (canTimeSeries ? SubTokensOptions.CanTimeSeries : 0));

        return new Column(queryToken, displayName ?? queryToken.NiceName());
    }

    public override string ToString() => $"{token} '{displayName}'";

}

public class PaginationTS
{
    public PaginationMode mode;
    public int? elementsPerPage;
    public int? currentPage;

    public PaginationTS() { }

    public PaginationTS(Pagination pagination)
    {
        this.mode = pagination.GetMode();
        this.elementsPerPage = pagination.GetElementsPerPage();
        this.currentPage = (pagination as Pagination.Paginate)?.CurrentPage;
    }

    public override string ToString() => $"{mode} {elementsPerPage} {currentPage}";


    public Pagination ToPagination()
    {
        return mode switch
        {
            PaginationMode.All => new Pagination.All(),
            PaginationMode.Firsts => new Pagination.Firsts(this.elementsPerPage!.Value),
            PaginationMode.Paginate => new Pagination.Paginate(this.elementsPerPage!.Value, this.currentPage!.Value),
            _ => throw new InvalidOperationException($"Unexpected {mode}"),
        };
    }
}



public class QueryValueRequestTS
{
    public required string queryKey;
    public List<FilterTS>? filters;
    public string? valueToken;
    public bool? multipleValues;
    public SystemTimeRequest? systemTime;

    public QueryValueRequest ToQueryValueRequest(string queryKey, JsonSerializerOptions jsonSerializerOptions)
    {
        if (queryKey != this.queryKey)
            throw new ArgumentException(nameof(queryKey));

        var qn = QueryLogic.ToQueryName(this.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);

        var value = valueToken.HasText() ? QueryUtils.Parse(valueToken, qd, SubTokensOptions.CanAggregate | SubTokensOptions.CanElement) : null;

        return new QueryValueRequest
        {
            QueryName = qn,
            MultipleValues = multipleValues ?? false,
            Filters = this.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: false, jsonSerializerOptions, this.systemTime?.mode == SystemTimeMode.TimeSeries)).ToList(),
            ValueToken = value,
            SystemTime = this.systemTime?.ToSystemTime(),
        };
    }

    public override string ToString() => queryKey;
}

public class QueryRequestTS
{
    public required string queryKey;
    public required bool groupResults;
    public required List<FilterTS> filters;
    public required List<OrderTS> orders;
    public required List<ColumnTS> columns;
    public required PaginationTS pagination;
    public SystemTimeRequest? systemTime;

    public static QueryRequestTS FromQueryRequest(QueryRequest qr)
    {
        return new QueryRequestTS
        {
            queryKey = QueryUtils.GetKey(qr.QueryName),
            groupResults = qr.GroupResults,
            columns = qr.Columns.Select(c => new ColumnTS { token = c.Token.FullKey(), displayName = c.DisplayName }).ToList(),
            filters = qr.Filters.Select(f => FilterTS.FromFilter(f)).ToList(),
            orders = qr.Orders.Select(o => new OrderTS { orderType = o.OrderType, token = o.Token.FullKey() }).ToList(),
            pagination = new PaginationTS(qr.Pagination),
            systemTime = qr.SystemTime?.Clone(),
        };
    }

    public QueryRequest ToQueryRequest(string queryKey, JsonSerializerOptions jsonSerializerOptions, string? referrerUrl)
    {
        if (queryKey != this.queryKey)
            throw new ArgumentException(nameof(queryKey));

        var qn = QueryLogic.ToQueryName(this.queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);
        var timeSeries = this.systemTime?.mode == SystemTimeMode.TimeSeries;

        return new QueryRequest
        {
            QueryUrl = referrerUrl,
            QueryName = qn,
            GroupResults = groupResults,
            Filters = this.filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: groupResults, jsonSerializerOptions, timeSeries)).ToList(),
            Orders = this.orders.EmptyIfNull().Select(f => f.ToOrder(qd, canAggregate: groupResults, canTimeSeries: timeSeries)).ToList(),
            Columns = this.columns.EmptyIfNull().Select(f => f.ToColumn(qd, canAggregate: groupResults, canTimeSeries: timeSeries)).ToList(),
            Pagination = this.pagination.ToPagination(),
            SystemTime = this.systemTime,
        };
    }


    public override string ToString() => queryKey;
}

public class QueryEntitiesRequestTS
{
    public required string queryKey;
    public required List<FilterTS> filters;
    public required List<OrderTS> orders;
    public int? count;

    public override string ToString() => queryKey;

    public QueryEntitiesRequest ToQueryEntitiesRequest(string queryKey, JsonSerializerOptions jsonSerializerOptions)
    {
        if (queryKey != this.queryKey)
            throw new ArgumentException(queryKey);

        var qn = QueryLogic.ToQueryName(queryKey);
        var qd = QueryLogic.Queries.QueryDescription(qn);
        return new QueryEntitiesRequest
        {
            QueryName = qn,
            Count = count,
            Filters = filters.EmptyIfNull().Select(f => f.ToFilter(qd, canAggregate: false, jsonSerializerOptions, canTimeSeries: false)).ToList(),
            Orders = orders.EmptyIfNull().Select(f => f.ToOrder(qd, canAggregate: false, canTimeSeries: false)).ToList(),
        };
    }
}

public class OrderTS
{
    public required string token;
    public required OrderType orderType;

    public Order ToOrder(QueryDescription qd, bool canAggregate, bool canTimeSeries)
    {
        return new Order(QueryUtils.Parse(this.token, qd, SubTokensOptions.CanElement | SubTokensOptions.CanSnippet | 
            (canAggregate ? SubTokensOptions.CanAggregate : 0) | 
            (canTimeSeries ? SubTokensOptions.CanTimeSeries : 0)
            ), orderType);
    }

    public override string ToString() => $"{token} {orderType}";
}

