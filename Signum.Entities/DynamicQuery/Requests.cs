using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum.Utilities;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public abstract class BaseQueryRequest
    {
        public object QueryName { get; set; }
        
        public List<Filter> Filters { get; set; }
        
        public string QueryUrl { get; set; }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(GetType().Name, QueryName);
        }
    }

    [Serializable]
    public class QueryRequest : BaseQueryRequest
    {
        public bool GroupResults { get; set; }

        public List<Column> Columns { get; set; }
        
        public List<Order> Orders { get; set; }
        
        public Pagination Pagination { get; set; }

        public SystemTime SystemTime { get; set; }

        public List<CollectionElementToken> Multiplications()
        {
            HashSet<QueryToken> allTokens = this.AllTokens().ToHashSet();

            return CollectionElementToken.GetElements(allTokens);
        }

        public List<QueryToken> AllTokens()
        {
            var allTokens = Columns.Select(a => a.Token).ToList();

            if (Filters != null)
                allTokens.AddRange(Filters.Select(a => a.Token));

            if (Orders != null)
                allTokens.AddRange(Orders.Select(a => a.Token));

            return allTokens;
        }
    }

    [DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
    public enum PaginationMode
    {
        All,
        Firsts,
        Paginate
    }

    [DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
    public enum SystemTimeMode
    {
        AsOf,
        Between,
        ContainedIn,
        All
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum SystemTimeProperty
    {
        SystemValidFrom,
        SystemValidTo,
    }

    [Serializable]
    public abstract class Pagination 
    {
        public abstract PaginationMode GetMode();
        public abstract int? GetElementsPerPage();
        public abstract int? MaxElementIndex { get; }

        [Serializable]
        public class All : Pagination
        {
            public override int? MaxElementIndex
            {
                get { return null; }
            }

            public override PaginationMode GetMode()
            {
                return PaginationMode.All;
            }

            public override int? GetElementsPerPage()
            {
                return null;
            }
        }

        [Serializable]
        public class Firsts : Pagination
        {
            public static int DefaultTopElements = 20; 

            public Firsts(int topElements)
            {
                this.TopElements = topElements;
            }
            
            public int TopElements { get; private set; }

            public override int? MaxElementIndex
            {
                get { return TopElements; }
            }

            public override PaginationMode GetMode()
            {
                return PaginationMode.Firsts;
            }

            public override int? GetElementsPerPage()
            {
                return TopElements;
            }
        }

        [Serializable]
        public class Paginate : Pagination
        {
            public static int DefaultElementsPerPage = 20;

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

            public int StartElementIndex()
            {
                return (ElementsPerPage * (CurrentPage - 1)) + 1;
            }

            public int EndElementIndex(int rows)
            {
                return StartElementIndex() + rows - 1;
            }

            public int TotalPages(int totalElements)
            {
                return (totalElements + ElementsPerPage - 1) / ElementsPerPage; //Round up
            }

            public override int? MaxElementIndex
            {
                get { return (ElementsPerPage * (CurrentPage + 1)) - 1; }
            }

            public override PaginationMode GetMode()
            {
                return PaginationMode.Paginate;
            }

            public override int? GetElementsPerPage()
            {
                return ElementsPerPage;
            }

            public Paginate WithCurrentPage(int newPage)
            {
                return new Paginate(this.ElementsPerPage, newPage);
            }
        }
    }

    [Serializable]
    public class QueryValueRequest : BaseQueryRequest
    {
        public QueryToken ValueToken { get; set; }

        public List<CollectionElementToken> Multiplications
        {
            get { return CollectionElementToken.GetElements(Filters.Select(a => a.Token).PreAnd(ValueToken).NotNull().ToHashSet()); }
        }
    }

    [Serializable]
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
                HashSet<QueryToken> allTokens = Filters.Select(a => a.Token)
                    .Concat(Orders.Select(a => a.Token)).ToHashSet();

                return CollectionElementToken.GetElements(allTokens);
            }
        }
    }

    [Serializable]
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
                HashSet<QueryToken> allTokens = Filters.Select(a => a.Token)
                    .Concat(Orders.Select(a => a.Token)).ToHashSet();

                return CollectionElementToken.GetElements(allTokens);
            }
        }

        public int? Count { get; set; }

        public override string ToString() => QueryName.ToString();
    }
}
