using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class QueryRequest
    {
        public object QueryName { get; set; }

        public List<Column> Columns { get; set; }

        public List<Filter> Filters { get; set; }

        public List<Order> Orders { get; set; }

        public int? Limit { get; set; }
    }

    [Serializable]
    public class QueryCountRequest
    {
        public object QueryName { get; set; }

        public List<Filter> Filters { get; set; }
    }

    [Serializable]
    public class UniqueEntityRequest
    {
        public object QueryName { get; set; }

        public List<Filter> Filters { get; set; }

        public List<Order> Orders { get; set; }

        public UniqueType UniqueType { get; set; }
    }
}
