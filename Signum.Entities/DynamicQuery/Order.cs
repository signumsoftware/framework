using System;
using Signum.Utilities;

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
            return "{0} {1}".FormatWith(token.FullKey(), orderType);
        }
    }

    [InTypeScript(true), DescriptionOptions(DescriptionOptions.Members | DescriptionOptions.Description)]
    public enum OrderType
    {
        Ascending,
        Descending
    }
}
