using System.ComponentModel;
using Signum.DynamicQuery.Tokens;
using System.Runtime.CompilerServices;
using static Npgsql.Replication.PgOutput.Messages.RelationMessage;

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
}

public class QueryRequest : BaseQueryRequest
{
    public bool GroupResults { get; set; }

    public required List<Column> Columns { get; set; }

    public required List<Order> Orders { get; set; }

    public required Pagination Pagination { get; set; }

    public SystemTime? SystemTime { get; set; }

    public bool CanDoMultiplicationsInSubQueries()
    {
        return GroupResults == false && Pagination is Pagination.All &&
            Orders.Select(a => a.Token).Concat(Columns.Select(a => a.Token)).Any(a => a.HasElement()) &&
            !Filters.SelectMany(f => f.GetAllFilters()).SelectMany(f => f.GetTokens()).Any(t => t.HasElement());
    }

    public List<CollectionElementToken> Multiplications() => CollectionElementToken.GetElements(this.AllTokens());
    public List<FilterFullText> FullTextTableFilters() => FilterFullText.TableFilters(this.Filters);

    public HashSet<QueryToken> AllTokens() => 
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

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum SystemTimeMode
{
    AsOf,
    Between,
    ContainedIn,
    All
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

    public class All : Pagination
    {
        public override PaginationMode GetMode() => PaginationMode.All;
        public override int? GetElementsPerPage() => null;
        public override int? MaxElementIndex => null;
        public override bool Equals(Pagination? other) => other is All;
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
    }
}

public class QueryValueRequest : BaseQueryRequest
{
    public required QueryToken? ValueToken { get; set; }

    public required bool MultipleValues { get; set; }

    public required SystemTime? SystemTime { get; set; }


    public HashSet<QueryToken> AllTokens() => Filters
              .SelectMany(f => f.GetAllFilters())
              .SelectMany(f => f.GetTokens())
              .PreAnd(ValueToken)
              .NotNull()
              .ToHashSet();

    public List<CollectionElementToken> Multiplications() => CollectionElementToken.GetElements(this.AllTokens());
    public List<FilterFullText> FullTextTableFilters() => FilterFullText.TableFilters(this.Filters);

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

}

public class UniqueEntityRequest : BaseQueryRequest
{
    public required List<Order> Orders { get; set; }

    public required UniqueType UniqueType { get; set; }

    public List<CollectionElementToken> Multiplications() => CollectionElementToken.GetElements(this.AllTokens());
    public List<FilterFullText> FullTextTableFilters() => FilterFullText.TableFilters(this.Filters);

    public HashSet<QueryToken> AllTokens() =>
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
}

public class QueryEntitiesRequest : BaseQueryRequest
{
    public required List<Order> Orders { get; set; }

    public List<CollectionElementToken> Multiplications() => CollectionElementToken.GetElements(AllTokens());
    public List<FilterFullText> FullTextTableFilters() => FilterFullText.TableFilters(this.Filters);

    public HashSet<QueryToken> AllTokens() => 
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
}
