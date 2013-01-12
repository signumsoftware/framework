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
        public QueryToken Token { get; private set; }
        public OrderType OrderType { get; private set; }

        public Order(QueryToken token, OrderType orderType)
        {
            this.Token = token;
            this.OrderType = orderType;
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(Token.FullKey(), OrderType);
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
