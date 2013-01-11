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
using Prop = Signum.Windows.Extensions.Properties;
using Signum.Services;
using System.Windows.Documents;
using Signum.Utilities;
using Signum.Entities.UserQueries;
using Signum.Entities.Authorization;
using System.Windows.Data;
using Signum.Windows.Authorization;

namespace Signum.Windows.UserQueries
{
    public static class UserQueryMenuItemConsturctor
    {
        static IValueConverter notNullAndEditable = ConverterFactory.New((UserQueryDN uq) => uq != null && uq.IsAllowedFor(TypeAllowedBasic.Modify));

        public static MenuItem Construct(SearchControl sc)
        {
            List<Lite<UserQueryDN>> userQueries = null;
            UserQueryDN current = null;

            MenuItem miResult = new MenuItem
            {
                Icon = ExtensionsImageLoader.GetImageSortName("favorite.png").ToSmallImage()
            };

            MenuItem edit = null;
            MenuItem remove = null;

            Action updatecurrent = () =>
            {   
                miResult.Header = new TextBlock
                {
                    Inlines = 
                    { 
                        new Run(
                        current == null ? Prop.Resources.MyQueries : current.DisplayName), 
                        userQueries.IsNullOrEmpty() ? (Inline)new Run():  new Bold(new Run(" (" + userQueries.Count + ")")) 
                    }
                };

                foreach (var item in miResult.Items.OfType<MenuItem>().Where(mi => mi.IsCheckable))
                {
                    item.IsChecked = ((Lite<UserQueryDN>)item.Tag).RefersTo(current);
                }

                edit.IsEnabled = current != null;
                remove.IsEnabled = current != null;
            };

            Action initialize = null;

            RoutedEventHandler new_Clicked = (object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                UserQueryDN userQuery = UserQueryClient.FromSearchControl(sc);

                Navigator.Navigate(userQuery, new NavigateOptions
                {
                    View = new UserQuery { QueryDescription = sc.Description },
                    Closed = (s, args) =>
                    {
                        initialize();

                        if (userQuery.IdOrNull != null)
                        {
                            current = userQuery;
                        }

                        updatecurrent();
                    }
                });
            };

            RoutedEventHandler edit_Clicked = (object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                Navigator.Navigate(current, new NavigateOptions()
                {
                    View = new UserQuery { QueryDescription = sc.Description },
                    Closed = (s, args) =>
                    {
                        initialize();
                        updatecurrent();
                    }
                });
            };

            RoutedEventHandler remove_Clicked = (object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                if (MessageBox.Show(Prop.Resources.AreYouSureToRemove0.Formato(current), Prop.Resources.RemoveUserQuery,
                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    Server.Execute((IUserQueryServer s) => s.RemoveUserQuery(current.ToLite()));

                    initialize();

                    current = null;

                    updatecurrent();
                }
            };

            RoutedEventHandler menuItem_Clicked = (object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                if (e.OriginalSource is MenuItem)
                {
                    MenuItem b = (MenuItem)e.OriginalSource;
                    Lite<UserQueryDN> userQuery = (Lite<UserQueryDN>)b.Tag;

                    var uq = userQuery.Retrieve();

                    UserQueryClient.ToSearchControl(uq, sc);

                    current = uq;

                    updatecurrent();

                    sc.Search();
                }
            };

            initialize = () =>
            {
                miResult.Items.Clear();

                userQueries = Server.Return((IUserQueryServer s) => s.GetUserQueries(sc.QueryName));


                if (userQueries.Count > 0)
                {
                    foreach (Lite<UserQueryDN> report in userQueries)
                    {
                        MenuItem mi = new MenuItem()
                        {
                            IsCheckable = true,
                            Header = report.ToString(),
                            Tag = report,
                        };
                        mi.Click += menuItem_Clicked;
                        miResult.Items.Add(mi);
                    }
                }

                miResult.Items.Add(new Separator());

                if (Navigator.IsCreable(typeof(UserQueryDN), true))
                {
                    miResult.Items.Add(new MenuItem()
                    {
                        Header = Signum.Windows.Extensions.Properties.Resources.Create,
                        Icon = ExtensionsImageLoader.GetImageSortName("add.png").ToSmallImage()
                    }.Handle(MenuItem.ClickEvent, new_Clicked));
                }

                miResult.Items.Add(edit = new MenuItem()
                {
                    Header = Signum.Windows.Extensions.Properties.Resources.Edit,
                    Icon = ExtensionsImageLoader.GetImageSortName("edit.png").ToSmallImage()
                }.Handle(MenuItem.ClickEvent, edit_Clicked));

                miResult.Items.Add(remove = new MenuItem()
                {
                    Header = Signum.Windows.Extensions.Properties.Resources.Remove,
                    Icon = ExtensionsImageLoader.GetImageSortName("remove.png").ToSmallImage()
                }.Handle(MenuItem.ClickEvent, remove_Clicked));
            };

            initialize();

            var autoSet = UserQueryClient.GetUserQuery(sc);

            if (autoSet != null)
            {
                UserQueryClient.ToSearchControl(autoSet, sc);
                current = autoSet;

                updatecurrent();

                sc.Search();
            }
            else
            {
                updatecurrent();
            }

            return miResult;
        }
    }
}
