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
        public object QueryName { get; set; }

        public List<Filter> Filters { get; set; }

        public override string ToString()
        {
            return "{0} {1}".Formato(GetType().Name, QueryName);
        }
    }

    [Serializable]
    public class QueryRequest : BaseQueryRequest
    {
        public List<Column> Columns { get; set; }

        public List<Order> Orders { get; set; }

        public int ElementsPerPage { get; set; }

        public const int AllElements = -1;

        public int CurrentPage { get; set; }

        public int? MaxElementIndex
        {
            get
            {
                if (ElementsPerPage  == AllElements)
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
        public List<Column> Columns { get; set; }

        public List<Order> Orders { get; set; }

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
        public List<Order> Orders { get; set; }

        public UniqueType UniqueType { get; set; }

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
