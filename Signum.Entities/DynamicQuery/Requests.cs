using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class QueryRequest
    {
        public object QueryName { get; set; }

        public List<Column> Columns { get; set; }

        public List<Filter> Filters { get; set; }

        public List<Order> Orders { get; set; }

        public int? MaxItems { get; set; }

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
    public class QueryCountRequest
    {
        public object QueryName { get; set; }

        public List<Filter> Filters { get; set; }

        public List<CollectionElementToken> Multiplications
        {
            get { return CollectionElementToken.GetElements(Filters.Select(a => a.Token).ToHashSet()); }
        }
    }

    [Serializable]
    public class UniqueEntityRequest
    {
        public object QueryName { get; set; }

        public List<Filter> Filters { get; set; }

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
