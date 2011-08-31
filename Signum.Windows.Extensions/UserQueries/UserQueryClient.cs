using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Windows.Reports;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;

namespace Signum.Windows.UserQueries
{
    public class UserQueryClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryClient.Start();
                Navigator.AddSetting(new EntitySettings<UserQueryDN>(EntityType.Default));
                SearchControl.GetCustomMenuItems += (qn, type) => new UserQueryMenuItem();
            }
        }

        internal static UserQueryDN FromSearchControl(SearchControl searchControl)
        {
            QueryDescription description = Navigator.Manager.GetQueryDescription(searchControl.QueryName);
            var query = QueryClient.GetQuery(searchControl.QueryName);

            var req = searchControl.GetQueryRequest();

            return req.ToUserQuery(description, query);
        }

        internal static void ToSearchControl(UserQueryDN uq, SearchControl searchControl)
        {
            var filters = searchControl.FilterOptions.Where(f=>f.Frozen).Concat(uq.Filters.Select(qf => new FilterOption
            {
                Path = qf.Token.FullKey(),
                Operation = qf.Operation,
                Value = qf.Value
            })).ToList();

            var columns = uq.Columns.Select(qc => new ColumnOption
            {
                Path = qc.Token.FullKey(),
                DisplayName = qc.DisplayName
            }).ToList();

            var orders = uq.Orders.Select(of => new OrderOption
            {
                Path = of.Token.FullKey(),
                OrderType = of.OrderType,
            }).ToList();

            Navigator.Manager.SetFilterTokens(searchControl.QueryName, filters);
            Navigator.Manager.SetOrderTokens(searchControl.QueryName, orders); 
                     
            searchControl.Reinitialize(filters, columns, uq.ColumnsMode, orders);

            searchControl.MaxItemsCount = uq.MaxItems;
        }
    }
}
