using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using Signum.API.Json;
using Signum.DynamicQuery.Tokens;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
namespace Signum.DynamicQuery;

public abstract class BaseQueryRequest
{
    public required object QueryName { get; set; }

    public required List<Filter> Filters { get; set; }

    public string? QueryUrl { get; set; }

    public abstract BaseQueryRequest CombineFullTextFilters();

    public override string ToString()
    {
        return "{0} {1}".FormatWith(GetType().Name, QueryName);
    }

    public abstract HashSet<QueryToken> AllTokens();

    public abstract string Dump();

    protected string DumpAnonymous(object obj)
    {
        return obj.GetType().GetProperties().ToString(p =>
        {
            var value = p.GetValue(obj);

            string str = value?.ToString() ?? "null";

            if (str.Contains("\n"))
                return p.Name + ":\n" + str.Indent(4);

            return p.Name + ": " + str;
        }, "\n");
    }
}

public class QueryRequest : BaseQueryRequest
{
    public bool GroupResults { get; set; }

    public required List<Column> Columns { get; set; }

    public required List<Order> Orders { get; set; }

    public required Pagination Pagination { get; set; }

    public SystemTimeRequest? SystemTime { get; set; }

    public override string Dump()
    {
        return DumpAnonymous(new
        {
            QueryName = QueryUtils.GetKey(QueryName),
            GroupResults = GroupResults,
            Filters = Filters.ToString("\n"),
            Columns = Columns.ToString("\n"),
            Orders = Orders.ToString("\n"),
            Pagination = Pagination.ToString(),
            SystemTime = SystemTime?.ToString()
        });
    }

    public void AssertNeasted()
    {
        var columns = Columns.Select(a => a.Token.HasNested()).NotNull().Distinct().ToList();
        var filterProblems = Filters.Select(a => a.GetDeepestNestedToken()).NotNull().Distinct().Where(f => !columns.Contains(f)).ToString(a => a.FullKey(), "\n");
        var orderProblems = Orders.Select(a => a.Token.HasNested()).NotNull().Distinct().Where(f => !columns.Contains(f)).ToString(a => a.FullKey(), "\n");
        if (filterProblems.HasText() || orderProblems.HasText())
        {
            throw new InvalidOperationException("\n\n".Combine(
                filterProblems == null ? null : $"Unable to filter by Nested token not selected in columns:\n{filterProblems.Indent(4)}",
                orderProblems == null ? null : $"Unable to order by Nested token not selected in columns:\n{orderProblems.Indent(4)}"));
        }
    }

    public List<CollectionElementToken> Multiplications() => CollectionElementToken.GetElements(this.AllTokens());
    public List<FilterSqlServerFullText> FullTextTableFilters() => FilterSqlServerFullText.TableFilters(this.Filters);

    public override HashSet<QueryToken> AllTokens() => 
        Filters.SelectMany(a => a.GetAllFilters()).SelectMany(f => f.GetTokens())
        .Concat(Columns.Select(a => a.Token))
        .Concat(Orders.Select(a => a.Token))
        .ToHashSet();

    public QueryRequest Clone() => new QueryRequest
    {
        QueryName = QueryName,
        GroupResults = GroupResults,
        Columns = Columns,
        Filters = Filters,
        Orders = Orders,
        Pagination = Pagination,
        SystemTime = SystemTime,
    };

    public override QueryRequest CombineFullTextFilters()
    {
        var result = new QueryRequest
        {
            QueryName = this.QueryName,
            QueryUrl = this.QueryUrl,
            Columns = this.Columns,
            GroupResults = this.GroupResults,
            Filters = this.Filters.Select(f => f.ToFullText()).NotNull().ToList(),
            Orders = this.Orders.Select(o => o.ToFullText()).ToList(),
            Pagination = this.Pagination,
            SystemTime = this.SystemTime,
        };

        Filter.SetIsTable(result.Filters, result.AllTokens());

        return result;
    }
}

[DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description), InTypeScript(true)]
public enum PaginationMode
{
    All,
    [Description("First")]
    Firsts,
    [Description("Pages")]
    Paginate
}

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum RefreshMode
{
    Auto,
    Manual
}

public class SystemTimeRequest
{
    public SystemTimeMode mode;
    public SystemTimeJoinMode? joinMode;
    public DateTime? startDate;
    public DateTime? endDate;
    public int? timeSeriesStep;
    public TimeSeriesUnit? timeSeriesUnit;
    public int? timeSeriesMaxRowsPerStep; 

    public SystemTimeRequest() { }

    public SystemTimeRequest(SystemTime systemTime)
    {
        if (systemTime is SystemTime.AsOf asOf)
        {
            mode = SystemTimeMode.AsOf;
            startDate = asOf.DateTime;
        }
        else if (systemTime is SystemTime.Between between)
        {
            mode = SystemTimeMode.Between;
            joinMode = between.JoinMode;
            startDate = between.StartDateTime;
            endDate = between.EndtDateTime;
        }
        else if (systemTime is SystemTime.ContainedIn containedIn)
        {
            mode = SystemTimeMode.ContainedIn;
            joinMode = containedIn.JoinMode;
            startDate = containedIn.StartDateTime;
            endDate = containedIn.EndtDateTime;
        }
        else if (systemTime is SystemTime.All all)
        {
            mode = SystemTimeMode.All;
            joinMode = all.JoinMode;
            startDate = null;
            endDate = null;
        }
        else
            throw new UnexpectedValueException();
    }

    public override string ToString() => $"{mode} {startDate} {endDate}";

    public SystemTimeRequest Clone() => new SystemTimeRequest
    { 
        mode = mode,
        joinMode = joinMode,
        startDate = startDate,
        endDate = endDate,
        timeSeriesUnit = timeSeriesUnit,
        timeSeriesStep = timeSeriesStep,
        timeSeriesMaxRowsPerStep = timeSeriesMaxRowsPerStep,
    };

    public SystemTime? ToSystemTime()
    {
        return mode switch
        {
            SystemTimeMode.AsOf => new SystemTime.AsOf(startDate!.Value),
            SystemTimeMode.Between => new SystemTime.Between(startDate!.Value, endDate!.Value, joinMode!.Value),
            SystemTimeMode.ContainedIn => new SystemTime.ContainedIn(startDate!.Value, endDate!.Value, joinMode!.Value),
            SystemTimeMode.All => new SystemTime.All(joinMode!.Value),
            SystemTimeMode.TimeSeries => null,
            _ => throw new InvalidOperationException($"Unexpected {mode}"),
        };
    }
}

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum SystemTimeMode
{
    AsOf,
    Between,
    ContainedIn,
    All,
    TimeSeries,
}

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum TimeSeriesUnit
{
    Year,
    Quarter,
    Month,
    Week,
    Day, 
    Hour,
    Minute,
    Second,
    Millisecond,
}

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum SystemTimeJoinMode
{
    Current,
    FirstCompatible,
    AllCompatible,
}

[DescriptionOptions(DescriptionOptions.Members)]
public enum SystemTimeProperty
{
    SystemValidFrom,
    SystemValidTo,
}

public abstract class Pagination : IEquatable<Pagination>
{
    public abstract PaginationMode GetMode();
    public abstract int? GetElementsPerPage();
    public abstract int? MaxElementIndex { get; }
    public abstract bool Equals(Pagination? other);
    public abstract override string ToString();

    public Pagination ToBigPage()
    {
        if (this is Pagination.Paginate p && p.CurrentPage > 1)
            return new Paginate.Paginate(p.CurrentPage * p.ElementsPerPage, 1);

        return this;
    }

    public class All : Pagination
    {
        public override PaginationMode GetMode() => PaginationMode.All;
        public override int? GetElementsPerPage() => null;
        public override int? MaxElementIndex => null;
        public override bool Equals(Pagination? other) => other is All;
        public override string ToString() => "All";
    }

    public class Firsts : Pagination
    {
        public static int DefaultTopElements = 20;

        public Firsts(int topElements)
        {
            this.TopElements = topElements;
        }

        public int TopElements { get; }

        public override PaginationMode GetMode() => PaginationMode.Firsts;
        public override int? GetElementsPerPage() => TopElements;
        public override int? MaxElementIndex => TopElements;
        public override bool Equals(Pagination? other) => other is Firsts f && f.TopElements == this.TopElements;
        public override string ToString() => "First " + TopElements;

    }

    public class Paginate : Pagination
    {
        public Paginate(int elementsPerPage, int currentPage = 1)
        {
            if (elementsPerPage <= 0)
                throw new InvalidOperationException("elementsPerPage should be greater than zero");

            if (currentPage <= 0)
                throw new InvalidOperationException("currentPage should be greater than zero");

            this.ElementsPerPage = elementsPerPage;
            this.CurrentPage = currentPage;
        }

        public int ElementsPerPage { get; private set; }
        public int CurrentPage { get; private set; }

        public int StartElementIndex() => (ElementsPerPage * (CurrentPage - 1)) + 1;
        public int EndElementIndex(int rows) => StartElementIndex() + rows - 1;
        public int TotalPages(int totalElements) => (totalElements + ElementsPerPage - 1) / ElementsPerPage; //Round up
        public Paginate WithCurrentPage(int newPage) => new Paginate(this.ElementsPerPage, newPage);

        public override PaginationMode GetMode() => PaginationMode.Paginate;
        public override int? GetElementsPerPage() => ElementsPerPage;
        public override int? MaxElementIndex => (ElementsPerPage * (CurrentPage + 1)) - 1;
        public override bool Equals(Pagination? other) => other is Paginate p && p.ElementsPerPage == ElementsPerPage && p.CurrentPage == CurrentPage;
        public override string ToString() => $"Paginate {ElementsPerPage} (Page = {CurrentPage})";

    }
}

public class QueryValueRequest : BaseQueryRequest
{
    public required QueryToken? ValueToken { get; set; }

    public required bool MultipleValues { get; set; }

    public required SystemTime? SystemTime { get; set; }


    public override HashSet<QueryToken> AllTokens() => Filters
              .SelectMany(f => f.GetAllFilters())
              .SelectMany(f => f.GetTokens())
              .PreAnd(ValueToken)
              .NotNull()
              .ToHashSet();

    public List<CollectionElementToken> Multiplications() => CollectionElementToken.GetElements(this.AllTokens());
    public List<FilterSqlServerFullText> FullTextTableFilters() => FilterSqlServerFullText.TableFilters(this.Filters);

    public override QueryValueRequest CombineFullTextFilters()
    {
        var result = new QueryValueRequest
        {
            QueryName = this.QueryName,
            QueryUrl = this.QueryUrl,
            Filters = this.Filters.Select(f => f.ToFullText()).NotNull().ToList(),
            SystemTime = this.SystemTime,
            ValueToken = this.ValueToken,
            MultipleValues = this.MultipleValues
        };

        Filter.SetIsTable(result.Filters, result.AllTokens());

        return result;
    }

    public override string Dump()
    {
        return DumpAnonymous(new
        {
            QueryName = QueryUtils.GetKey(QueryName),
            ValueToken = ValueToken?.ToString(),
            MultipleValues = MultipleValues,
            SystemTime = SystemTime?.ToString()
        });
    }

}

public class UniqueEntityRequest : BaseQueryRequest
{
    public required List<Order> Orders { get; set; }

    public required UniqueType UniqueType { get; set; }

    public List<CollectionElementToken> Multiplications() => CollectionElementToken.GetElements(this.AllTokens());
    public List<FilterSqlServerFullText> FullTextTableFilters() => FilterSqlServerFullText.TableFilters(this.Filters);

    public override HashSet<QueryToken> AllTokens() =>
        Filters.SelectMany(a => a.GetAllFilters()).SelectMany(f => f.GetTokens())
        .Concat(Orders.Select(a => a.Token)).ToHashSet();

    public override UniqueEntityRequest CombineFullTextFilters()
    {
        var result = new UniqueEntityRequest
        {
            QueryName = this.QueryName,
            QueryUrl = this.QueryUrl,
            Filters = this.Filters.Select(f => f.ToFullText()).NotNull().ToList(),
            Orders = this.Orders.Select(f => f.ToFullText()).ToList(),
            UniqueType = this.UniqueType,
        };

        Filter.SetIsTable(result.Filters, result.AllTokens());

        return result;
    }

    public override string Dump()
    {
        return DumpAnonymous(new
        {
            QueryName = QueryUtils.GetKey(QueryName),
            Filters = Filters.ToString("\n"),
            Orders = Orders.ToString("\n"),
            UniqueType = UniqueType,
        })!;
    }
}

public class QueryEntitiesRequest : BaseQueryRequest
{
    public required List<Order> Orders { get; set; }

    public List<CollectionElementToken> Multiplications() => CollectionElementToken.GetElements(AllTokens());
    public List<FilterSqlServerFullText> FullTextTableFilters() => FilterSqlServerFullText.TableFilters(this.Filters);

    public override HashSet<QueryToken> AllTokens() => 
        Filters.SelectMany(a => a.GetAllFilters()).SelectMany(f => f.GetTokens())
        .Concat(Orders.Select(a => a.Token))
        .ToHashSet();

    public required int? Count { get; set; }

    public override string ToString() => QueryName.ToString()!;

    public override QueryEntitiesRequest CombineFullTextFilters()
    {
        var result = new QueryEntitiesRequest
        {
            QueryName = this.QueryName,
            QueryUrl = this.QueryUrl,
            Filters = this.Filters.Select(f => f.ToFullText()).NotNull().ToList(),
            Orders = this.Orders.Select(f => f.ToFullText()).ToList(),
            Count = Count
        };

        Filter.SetIsTable(result.Filters, result.AllTokens());

        return result;
    }


    public override string Dump()
    {
        return DumpAnonymous(new
        {
            QueryName = QueryUtils.GetKey(QueryName),
            Filters = Filters.ToString("\n"),
            Orders = Orders.ToString("\n"),
            Count = Count,
        });
    }
}
