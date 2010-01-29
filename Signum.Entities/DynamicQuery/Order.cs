using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class Order
    {
        public Order(QueryToken token, OrderType orderType)
        {
            this.Token = token;
            this.OrderType = orderType;
        }

        public QueryToken Token { get; set; }

        public OrderType OrderType { get; set; }
    }

    /// <summary>
    /// An OrderBy order type 
    /// </summary>
    public enum OrderType
    {
        Ascending,
        Descending
    }
}
