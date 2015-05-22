using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public abstract class BaseQueryRequest
    {
        object queryName;
        public object QueryName
        {
            get { return queryName; }
            set { queryName = value; }
        }

        List<Filter> filters;
        public List<Filter> Filters
        {
            get { return filters; }
            set { filters = value; }
        }

        string queryUrl;
        public string QueryUrl
        {
            get { return queryUrl; }
            set { queryUrl = value; }
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(GetType().Name, QueryName);
        }
    }

    [Serializable]
    public class QueryRequest : BaseQueryRequest
    {
        List<Column> columns;
        public List<Column> Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        List<Order> orders;
        public List<Order> Orders
        {
            get { return orders; }
            set { orders = value; }
        }

        Pagination pagination;
        public Pagination Pagination
        {
            get { return pagination; }
            set { pagination = value; }
        }

        public List<CollectionElementToken> Multiplications
        {
            get
            {
                HashSet<QueryToken> allTokens =
                    Columns.Select(a => a.Token)
                    .Concat(Filters.Select(a => a.Token))
                    .Concat(Orders.Select(a => a.Token)).ToHashSet();

                return CollectionElementToken.GetElements(allTokens);
            }
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum PaginationMode
    {
        All,
        Firsts,
        Paginate
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
                this.topElements = topElements;
            }

            readonly int topElements;
            public int TopElements
            {
                get { return topElements; }
            }

            public override int? MaxElementIndex
            {
                get { return topElements; }
            }

            public override PaginationMode GetMode()
            {
                return PaginationMode.Firsts;
            }

            public override int? GetElementsPerPage()
            {
                return topElements;
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

                this.elementsPerPage = elementsPerPage;
                this.currentPage = currentPage;
            }

            readonly int elementsPerPage;
            public int ElementsPerPage          
            {
                get { return elementsPerPage; }
            }

            readonly int currentPage;
            public int CurrentPage
            {
                get { return currentPage; }
            }

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
                return (totalElements + elementsPerPage - 1) / elementsPerPage; //Round up
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
                return elementsPerPage;
            }

            public Paginate WithCurrentPage(int newPage)
            {
                return new Paginate(this.elementsPerPage, newPage);
            }
        }
    }

    [Serializable]
    public class QueryGroupRequest : BaseQueryRequest
    {
        List<Column> columns;
        public List<Column> Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        List<Order> orders;
        public List<Order> Orders
        {
            get { return orders; }
            set { orders = value; }
        }

        public List<CollectionElementToken> Multiplications
        {
            get
            {
                HashSet<QueryToken> allTokens =
                    Columns.Select(a => a.Token)
                    .Concat(Orders.Select(a => a.Token))
                    .Concat(Filters.Select(a => a.Token)).ToHashSet();

                return CollectionElementToken.GetElements(allTokens);
            }
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

    [Serializable]
    public class QueryCountRequest : BaseQueryRequest
    {
        public List<CollectionElementToken> Multiplications
        {
            get { return CollectionElementToken.GetElements(Filters.Select(a => a.Token).ToHashSet()); }
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
}
