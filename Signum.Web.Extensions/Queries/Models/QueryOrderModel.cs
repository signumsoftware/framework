using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Reports;
using Signum.Entities.DynamicQuery;

namespace Signum.Web.Queries.Models
{
    [Serializable()]
    public class QueryOrderModel : Entity
    {
        public QueryOrderModel() { }

        public QueryOrderModel(QueryOrderDN order, string queryUrlName)
        {
            OrderType = order.OrderType;
            Index = order.Index;
            QueryToken = new QueryTokenModel(order, queryUrlName);
        }

        OrderType orderType;
        public OrderType OrderType
        {
            get { return orderType; }
            set { Set(ref orderType, value, () => OrderType); }
        }

        int index;
        public int Index
        {
            get { return index; }
            set { Set(ref index, value, () => Index); }
        }

        QueryTokenModel queryToken;
        [NotNullValidator]
        public QueryTokenModel QueryToken
        {
            get { return queryToken; }
            set { Set(ref queryToken, value, () => QueryToken); }
        }

        public QueryOrderDN ToQueryOrderDN()
        {
            return new QueryOrderDN
            {
                Token = this.QueryToken.QueryToken.Token,
                OrderType = this.OrderType,
                Index = this.Index
            };
        }
    }
}
