using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class Order
    {
        QueryToken token;
        public QueryToken Token { get { return token; } }

        OrderType orderType;
        public OrderType OrderType { get { return orderType; } }

        public Order(QueryToken token, OrderType orderType)
        {
            this.token = token;
            this.orderType = orderType;
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(token.FullKey(), orderType);
        }
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
