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
using Signum.Windows.Authorization;
using Signum.Entities.Authorization;
using Signum.Windows.Omnibox;
using System.Windows;
using Signum.Utilities;

namespace Signum.Windows.UserQueries
{
    public class UserQueryClient
    {
        public static readonly DependencyProperty UserQueryProperty =
            DependencyProperty.RegisterAttached("UserQuery", typeof(UserQueryDN), typeof(UserQueryClient), new FrameworkPropertyMetadata((s, e)=>OnUserQueryChanged(s, (UserQueryDN)e.NewValue)));
        public static UserQueryDN GetUserQuery(DependencyObject obj)
        {
            return (UserQueryDN)obj.GetValue(UserQueryProperty);
        }

        public static void SetUserQuery(DependencyObject obj, UserQueryDN value)
        {
            obj.SetValue(UserQueryProperty, value);
        }

        private static void OnUserQueryChanged(DependencyObject s, UserQueryDN uc)
        {
            var csc = s as CountSearchControl;
            if (csc != null)
            {
                csc.QueryName = QueryClient.queryNames[uc.Query.Key];
                UserQueryClient.ToCountSearchControl(uc, csc);
                csc.Search();
                return;
            }

            var sc = s as SearchControl;
            if (sc != null && sc.ShowHeader == false)
            {
                sc.QueryName = QueryClient.queryNames[uc.Query.Key];
                UserQueryClient.ToSearchControl(uc, sc);
                sc.Search();
                return;
            }

            return;
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryClient.Start();
                Navigator.AddSetting(new EntitySettings<UserQueryDN>(EntityType.Default) { View = _ => new UserQuery(), IsCreable = a => a });
                Constructor.Register<UserQueryDN>(win =>
                {
                    MessageBox.Show(win,
                        Signum.Windows.Extensions.Properties.Resources._0CanOnlyBeCreatedFromTheSearchWindow.Formato(typeof(UserQueryDN).NicePluralName()),
                        Signum.Windows.Extensions.Properties.Resources.Create,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return null;
                }); 
                SearchControl.GetCustomMenuItems += new MenuItemForQueryName(SearchControl_GetCustomMenuItems);
            }
        }

        static SearchControlMenuItem SearchControl_GetCustomMenuItems(object queryName, Type entityType)
        {
            if (!Navigator.IsViewable(typeof(UserQueryDN), true))
                return null;

            return new UserQueryMenuItem();
        }

        internal static UserQueryDN FromSearchControl(SearchControl searchControl)
        {
            QueryDescription description = Navigator.Manager.GetQueryDescription(searchControl.QueryName);

            return searchControl.GetQueryRequest(true).ToUserQuery(description, QueryClient.GetQuery(searchControl.QueryName), FindOptions.DefaultElementsPerPage);
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
         
            searchControl.Reinitialize(filters, columns, uq.ColumnsMode, orders);

            searchControl.ElementsPerPage = uq.ElementsPerPage ?? FindOptions.DefaultElementsPerPage;
        }

        internal static void ToCountSearchControl(UserQueryDN uq, CountSearchControl countSearchControl)
        {
            var filters = uq.Filters.Select(qf => new FilterOption
            {
                Path = qf.Token.FullKey(),
                Operation = qf.Operation,
                Value = qf.Value
            }).ToList();

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

            Navigator.Manager.SetFilterTokens(countSearchControl.QueryName, filters);
            Navigator.Manager.SetOrderTokens(countSearchControl.QueryName, orders);

            countSearchControl.Reinitialize(filters, columns, uq.ColumnsMode, orders);
        }
        
    }
}
