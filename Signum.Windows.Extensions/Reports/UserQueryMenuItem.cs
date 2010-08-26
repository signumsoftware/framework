using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Extensions;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Signum.Entities.Reports;
using Prop = Signum.Windows.Extensions.Properties;
using Signum.Services;
using System.Windows.Documents;
using Signum.Utilities;

namespace Signum.Windows.Reports
{
    public class UserQueryMenuItem : SearchControlMenuItem
    {
        public static readonly DependencyProperty CurrentUserQueryProperty =
            DependencyProperty.Register("CurrentUserQuery", typeof(UserQueryDN), typeof(UserQueryMenuItem), new UIPropertyMetadata((d, e) => ((UserQueryMenuItem)d).UpdateCurrent((UserQueryDN)e.NewValue)));
        public UserQueryDN CurrentUserQuery
        {
            get { return (UserQueryDN)GetValue(CurrentUserQueryProperty); }
            set { SetValue(CurrentUserQueryProperty, value); }
        }

        private void UpdateCurrent(UserQueryDN current)
        {
            Header = new TextBlock
            {
                Inlines = 
                { 
                    new Run(
                    current == null ? Prop.Resources.MyQueries : current.DisplayName), 
                    UserQueries == null || UserQueries.Count==0 ? (Inline)new Run():  new Bold(new Run(" (" + UserQueries.Count + ")")) 
                }
            };

            foreach (var item in this.Items.OfType<MenuItem>().Where(mi => mi.IsCheckable))
            {
                item.IsChecked = ((Lite<UserQueryDN>)item.Tag).RefersTo(current);
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Icon = ExtensionsImageLoader.GetImageSortName("favorite.png").ToSmallImage();
        }

        List<Lite<UserQueryDN>> UserQueries; 

        public override void Initialize()
        {
            Items.Clear();

            this.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Clicked));

            UserQueries = Server.Return((IUserQueryServer s) => s.GetUserQueries(SearchControl.QueryName));
            
            UpdateCurrent(CurrentUserQuery);

            if (UserQueries.Count > 0)
            {
                foreach (Lite<UserQueryDN> report in UserQueries)
                {
                    MenuItem mi = new MenuItem()
                    {
                        IsCheckable = true,
                        Header = report.ToStr,
                        Tag = report,
                    };
                    Items.Add(mi);
                }
            }

            Items.Add(new Separator());

            Items.Add(new MenuItem()
            {
                Header = Signum.Windows.Extensions.Properties.Resources.Create, 
                Icon = ExtensionsImageLoader.GetImageSortName("add.png").ToSmallImage()
            }.Handle(MenuItem.ClickEvent, New_Clicked));

            Items.Add(new MenuItem()
            {
                Header = Signum.Windows.Extensions.Properties.Resources.Edit, 
                Icon = ExtensionsImageLoader.GetImageSortName("edit.png").ToSmallImage()
            }.Handle(MenuItem.ClickEvent, Edit_Clicked)
            .Bind(MenuItem.IsEnabledProperty, this, "CurrentUserQuery", Converters.IsNotNull));

            Items.Add(new MenuItem()
            {
                Header = Signum.Windows.Extensions.Properties.Resources.Remove, 
                Icon = ExtensionsImageLoader.GetImageSortName("remove.png").ToSmallImage()
            }.Handle(MenuItem.ClickEvent, Remove_Clicked)
            .Bind(MenuItem.IsEnabledProperty, this, "CurrentUserQuery", Converters.IsNotNull));
        }

        private void MenuItem_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (e.OriginalSource is MenuItem)
            {
                MenuItem b = (MenuItem)e.OriginalSource;
                Lite<UserQueryDN> userQuery = (Lite<UserQueryDN>)b.Tag;

                var uq = userQuery.Retrieve();

                var filters = uq.Filters.Select(qf => new FilterOption
                {
                    Path = qf.Token.FullKey(),
                    Operation = qf.Operation,
                    Value = qf.Value
                });

                var columns = uq.Columns.Select(qc => new UserColumnOption
                {
                    Path = qc.Token.FullKey(),
                    DisplayName = qc.DisplayName
                });

                var orders = uq.Orders.Select(of => new OrderOption
                {
                    Path = of.Token.FullKey(),
                    OrderType = of.OrderType,
                });

                if (!SearchControl.NotSet(SearchControl.MaxItemsCountProperty))
                    uq.MaxItems = SearchControl.MaxItemsCount;

                SearchControl.Reinitialize(filters, columns, orders);

                CurrentUserQuery = uq;

                SearchControl.Search();
            }
        }

        private void New_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            UserQueryDN userQuery = UserQuery.FromSearchControl(this.SearchControl);

            userQuery = Navigator.View(userQuery, new ViewOptions
            {
                AllowErrors = AllowErrors.No,
                View = new UserQuery { QueryDescription = SearchControl.Description }
            });

            if (userQuery != null)
            {
                userQuery.Save();

                Initialize();

                CurrentUserQuery = userQuery;
            }
        }

        private void Edit_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            Navigator.Navigate(CurrentUserQuery, new NavigateOptions
            {
                View = new UserQuery { QueryDescription = SearchControl.Description },
                Closed = (s, args) => Initialize()
            });
        }

        private void Remove_Clicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (MessageBox.Show(Prop.Resources.AreYouSureToRemove0.Formato(CurrentUserQuery), Prop.Resources.RemoveUserQuery,
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                Server.Execute((IUserQueryServer s) => s.RemoveUserQuery(CurrentUserQuery.ToLite()));

                CurrentUserQuery = null;

                Initialize();
            }
        }
    }
}
