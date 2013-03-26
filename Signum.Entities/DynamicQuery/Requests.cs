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

        public override string ToString()
        {
            return "{0} {1}".Formato(GetType().Name, QueryName);
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

        int elementsPerPage;
        public int ElementsPerPage
        {
            get { return elementsPerPage; }
            set { elementsPerPage = value; }
        }

        public const int AllElements = -1;

        int currentPage;
        public int CurrentPage
        {
            get { return currentPage; }
            set { currentPage = value; }
        }

        public int? MaxElementIndex
        {
            get
            {
                if (ElementsPerPage == AllElements)
                    return null;

                return (ElementsPerPage * (CurrentPage + 1)) - 1;
            }
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
