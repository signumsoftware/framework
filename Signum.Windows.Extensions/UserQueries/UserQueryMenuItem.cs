using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using Signum.Services;
using System.Windows.Documents;
using Signum.Utilities;
using Signum.Entities.UserQueries;
using Signum.Entities.Authorization;
using System.Windows.Data;
using Signum.Windows.Authorization;
using System.Windows.Input;
using System.Windows.Threading;

namespace Signum.Windows.UserQueries
{
    public static class UserQueryMenuItemConstructor
    {
        static IValueConverter notNullAndEditable = ConverterFactory.New((UserQueryEntity uq) => uq != null && uq.IsAllowedFor(TypeAllowedBasic.Modify));

        public static MenuItem Construct(SearchControl sc)
        {
            List<Lite<UserQueryEntity>> userQueries = null;
            UserQueryEntity current = null;

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
                        current == null ? UserQueryMessage.MyQueries.NiceToString() : current.DisplayName), 
                        userQueries.IsNullOrEmpty() ? (Inline)new Run():  new Bold(new Run(" (" + userQueries.Count + ")")) 
                    }
                };

                foreach (var item in miResult.Items.OfType<MenuItem>().Where(mi => mi.IsCheckable))
                {
                    item.IsChecked = ((Lite<UserQueryEntity>)item.Tag).RefersTo(current);
                }

                bool isEnabled = current != null && !Navigator.IsReadOnly(current);

                if (edit != null)
                    edit.IsEnabled = isEnabled;

                if (remove != null)
                    remove.IsEnabled = isEnabled;
            };

            Action initialize = null;

            RoutedEventHandler new_Clicked = (object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                sc.FocusSearch(); //Commit RealValue bindings

                UserQueryEntity userQuery = UserQueryClient.FromSearchControl(sc);

                var disp = Dispatcher.CurrentDispatcher;
                Navigator.Navigate(userQuery, new NavigateOptions
                {
                    View = () => new UserQuery { QueryDescription = sc.Description },
                    Closed = (s, args) =>
                    {
                        disp.Invoke(() =>
                        {
                            initialize();

                            if (userQuery.IdOrNull != null)
                            {
                                current = userQuery;
                            }

                            updatecurrent();
                        });
                    }
                });
            };

            RoutedEventHandler edit_Clicked = (object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                var d = Dispatcher.CurrentDispatcher;
                Navigator.Navigate(current, new NavigateOptions
                {
                    View = () => new UserQuery { QueryDescription = sc.Description },
                    Closed = (s, args) =>
                    {
                        d.Invoke(() =>
                        {
                            initialize();
                            updatecurrent();
                        });
                    }
                });
            };

            RoutedEventHandler remove_Clicked = (object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                if (MessageBox.Show(UserQueryMessage.AreYouSureToRemove0.NiceToString().FormatWith(current), UserQueryMessage.RemoveUserQuery.NiceToString(),
                    MessageBoxButton.YesNo, MessageBoxImage.Exclamation, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
                    current.ToLite().DeleteLite(UserQueryOperation.Delete);

                    initialize();

                    updatecurrent();
                }
            };

            RoutedEventHandler menuItem_Clicked = (object sender, RoutedEventArgs e) =>
            {
                e.Handled = true;

                if (e.OriginalSource is MenuItem)
                {
                    MenuItem b = (MenuItem)e.OriginalSource;
                    Lite<UserQueryEntity> userQuery = (Lite<UserQueryEntity>)b.Tag;

                    var uq = Server.Return((IUserQueryServer s) => s.RetrieveUserQuery(userQuery));

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

                if (current != null && !userQueries.Contains(current.ToLite()))
                    current = null;

                if (userQueries.Count > 0)
                {
                    foreach (Lite<UserQueryEntity> report in userQueries.OrderBy(a=>a.ToString()))
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

                if (Navigator.IsNavigable(typeof(UserQueryEntity), isSearch: true))
                {
                    miResult.Items.Add(new Separator());

                    if (Navigator.IsCreable(typeof(UserQueryEntity), true))
                    {
                        miResult.Items.Add(new MenuItem()
                        {
                            Header = EntityControlMessage.Create.NiceToString(),
                            Icon = ExtensionsImageLoader.GetImageSortName("add.png").ToSmallImage()
                        }.Handle(MenuItem.ClickEvent, new_Clicked));
                    }

                    miResult.Items.Add(edit = new MenuItem()
                    {
                        Header = UserQueryMessage.Edit.NiceToString(),
                        Icon = ExtensionsImageLoader.GetImageSortName("edit.png").ToSmallImage()
                    }.Handle(MenuItem.ClickEvent, edit_Clicked));

                    miResult.Items.Add(remove = new MenuItem()
                    {
                        Header = EntityControlMessage.Remove.NiceToString(),
                        Icon = ExtensionsImageLoader.GetImageSortName("remove.png").ToSmallImage()
                    }.Handle(MenuItem.ClickEvent, remove_Clicked));
                }
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
