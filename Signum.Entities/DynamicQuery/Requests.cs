using System.Collections.ObjectModel;
using System.ComponentModel;

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
namespace Signum.Entities.DynamicQuery;

public abstract class BaseQueryRequest
{
    public required object QueryName { get; set; }

    public required List<Filter> Filters { get; set; }

    public string? QueryUrl { get; set; }

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

    public bool MultiplicationsInSubQueries()
    {
        return GroupResults == false && Pagination is Pagination.All &&
            Orders.Select(a => a.Token).Concat(Columns.Select(a => a.Token)).Any(a => a.HasElement()) &&
            !Filters.SelectMany(a => a.GetFilterConditions()).Select(a => a.Token).Any(t => t.HasElement());
    }

    public List<CollectionElementToken> Multiplications()
    {
        HashSet<QueryToken> allTokens = new HashSet<QueryToken>(this.AllTokens());

        return CollectionElementToken.GetElements(allTokens);
    }

    public List<QueryToken> AllTokens()
    {
        var allTokens = Columns.Select(a => a.Token).ToList();

        if (Filters != null)
            allTokens.AddRange(Filters.SelectMany(a => a.GetFilterConditions()).Select(a => a.Token));

        if (Orders != null)
            allTokens.AddRange(Orders.Select(a => a.Token));

        return allTokens;
    }

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

        public int TopElements { get;  }

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
    public QueryToken? ValueToken { get; set; }

    public bool MultipleValues { get; set; }

    public SystemTime? SystemTime { get; set; }

    public List<CollectionElementToken> Multiplications
    {
        get
        {
            return CollectionElementToken.GetElements(Filters
              .SelectMany(a => a.GetFilterConditions())
              .Select(fc => fc.Token)
              .PreAnd(ValueToken)
              .NotNull()
              .ToHashSet());
        }
    }

}

public class UniqueEntityRequest : BaseQueryRequest
{
    List<Order> orders;
    public List<Order> Orders
    {
        get { return orders; }
        set { orders = value; }
    }

    UniqueType uniqueType;
    public UniqueType UniqueType
    {
        get { return uniqueType; }
        set { uniqueType = value; }
    }

    public List<CollectionElementToken> Multiplications
    {
        get
        {
            var allTokens = Filters
                .SelectMany(a => a.GetFilterConditions())
                .Select(a => a.Token)
                .Concat(Orders.Select(a => a.Token))
                .ToHashSet();

            return CollectionElementToken.GetElements(allTokens);
        }
    }
}

public class QueryEntitiesRequest: BaseQueryRequest
{
    List<Order> orders = new List<Order>();
    public List<Order> Orders
    {
        get { return orders; }
        set { orders = value; }
    }

    public List<CollectionElementToken> Multiplications
    {
        get
        {
            var allTokens = Filters.SelectMany(a=>a.GetFilterConditions()).Select(a => a.Token)
                .Concat(Orders.Select(a => a.Token)).ToHashSet();

            return CollectionElementToken.GetElements(allTokens);
        }
    }

    public int? Count { get; set; }

    public override string ToString() => QueryName.ToString()!;
}
