using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Services;
using Signum.Utilities;
using Signum.Windows.DynamicQuery;
using Signum.Windows.Operations;

namespace Signum.Windows.Help
{
    public static class HelpClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.Manager.TaskNormalWindow += Manager_TaskNormalWindow;
                SearchControl.GetMenuItems += SearchControl_GetMenuItems;
            }
        }

        public static readonly DependencyProperty HelpInfoProperty =
            DependencyProperty.RegisterAttached("HelpInfo", typeof(HelpToolTipInfo), typeof(HelpClient), new PropertyMetadata(null, HelpInfoChange));
        public static HelpToolTipInfo GetHelpInfo(DependencyObject obj)
        {
            return (HelpToolTipInfo)obj.GetValue(HelpInfoProperty);
        }
        public static void SetHelpInfo(DependencyObject obj, HelpToolTipInfo value)
        {
            obj.SetValue(HelpInfoProperty, value);
        }

        static void HelpInfoChange(DependencyObject o, DependencyPropertyChangedEventArgs args)
        {
            UIElement element = (UIElement)o;

            var layer = AdornerLayer.GetAdornerLayer(element);

            HelpToolTipInfo info = (HelpToolTipInfo)args.NewValue;
            
            if (info == null)
            {
                foreach (var ad in layer.GetAdorners(element).EmptyIfNull().OfType<HelpAdorner>().ToList())
                {
                    layer.Remove(ad);
                }
            }
            else
            {
                layer.Add(new HelpAdorner(element)
                {
                    ToolTip = new ToolTip
                    {
                        Content = new HelpToolTip
                        {
                            DataContext = info
                        }
                    }
                });
            }
        }

        static async void Manager_TaskNormalWindow(NormalWindow normal, ModifiableEntity entity)
        {
            ButtonBar bar = normal.Child<ButtonBar>();

            var wrapPanel = (WrapPanel)bar.Content;
            bar.Content = null;

            HelpButton helpButton = new HelpButton
            {
                MainControl = normal,
                Margin = new Thickness(4),
                IsEnabled = false,
            }.Set(DockPanel.DockProperty, Dock.Right);

            bar.Content = new DockPanel
            {
                Children = 
                { 
                    helpButton,
                    wrapPanel
                }
            };

            helpButton.IsActive = await Server.ReturnAsync((IHelpServer s) => s.HasEntityHelpService(entity.GetType()));

            helpButton.Checked += async (sender, args) =>
            {
                var entityHelp = await Server.ReturnAsync((IHelpServer s) => s.GetEntityHelpService(entity.GetType()));

                SetHelpInfo(normal.Child<EntityTitle>(), entityHelp.Info);

                var properties = (from control in normal.Children<Control>(p => p.IsSet(Common.PropertyRouteProperty), HelpButton.WhereFlags)
                                  let propertyRoute = Common.GetPropertyRoute(control).SimplifyToPropertyOrRoot()
                                  where propertyRoute.PropertyRouteType != PropertyRouteType.Root
                                  select new { control, propertyRoute }).ToList();

                var external = properties.Extract(t => t.propertyRoute.RootType != entity.GetType());

                foreach (var t in properties)
                {
                    SetHelpInfo(t.control, entityHelp.Properties.GetOrThrow(t.propertyRoute));
                }

                var buttonBar = normal.Child<ButtonBar>();

                var operations = (from control in buttonBar.Children<ToolBarButton>(p => p.Tag is OperationInfo, HelpButton.WhereFlags)
                                  select new { control, operation = ((OperationInfo)control.Tag).OperationSymbol }).ToList();

                foreach (var t in operations)
                {
                    SetHelpInfo(t.control, entityHelp.Operations.GetOrThrow(t.operation));
                }

                var menuInfos = (from c in buttonBar.Children<ToolBarButton>(p => p.ContextMenu != null, HelpButton.WhereFlags)
                                 from mi in c.ContextMenu.Children<MenuItem>(p => p.Tag is OperationInfo, HelpButton.WhereFlags)
                                 select new { mi, operation = ((OperationInfo)mi.Tag).OperationSymbol }).ToList();

                foreach (var t in menuInfos)
                {
                    SetHelpInfo(t.mi, entityHelp.Operations.GetOrThrow(t.operation)); //ConstructFrom OperationInfo
                }

                if (external.Any())
                {
                    var externalRoutes = external.Select(a => a.propertyRoute).Distinct().ToList();

                    var dictionary = await Server.ReturnAsync((IHelpServer s) => s.GetPropertyRoutesService(externalRoutes));

                    foreach (var t in external)
                    {
                        SetHelpInfo(t.control, dictionary.GetOrThrow(t.propertyRoute));
                    }
                }
            }; 

            helpButton.IsEnabled = true;
        }

        static MenuItem SearchControl_GetMenuItems(SearchControl sc)
        {
            AddHelpButton(sc);

            return null;
        }

        private static async void AddHelpButton(SearchControl sc)
        {
            Menu bar = sc.Child<Menu>(m => m.Name == "menu");

            HelpButton helpButton = new HelpButton
            {
                MainControl = sc,
                Margin = new Thickness(4),
                IsEnabled = false,
            }.Set(DockPanel.DockProperty, Dock.Right);

            bar.After(helpButton);

            var queryName = sc.QueryName;

            helpButton.IsActive = await Server.ReturnAsync((IHelpServer s) => s.HasQueryHelpService(queryName));

            helpButton.Checked += async (sender, args) =>
            {
                var queryHelp = await Server.ReturnAsync((IHelpServer s) => s.GetQueryHelpService(queryName));

                SetHelpInfo(sc.Child<Button>(b=>b.Name == "btSearch"), queryHelp.Info);

                var listView = sc.Child<ListView>(b => b.Name == "lvResult");

                var columns = (from header in listView.Children<SortGridViewColumnHeader>(HelpButton.WhereFlags)
                                  let token = header.RequestColumn.Token
                                  select new { header, token }).ToList();

                var external = columns.Extract(t => !(t.token is ColumnToken));

                foreach (var t in columns)
                {
                    SetHelpInfo(t.header, queryHelp.Columns.GetOrThrow(((ColumnToken)t.token).Column.Name));
                }


                if (external.Any())
                {
                    var externalRoutes = external.Select(a => a.token.GetPropertyRoute()).Distinct().ToList();

                    var dictionary = await Server.ReturnAsync((IHelpServer s) => s.GetPropertyRoutesService(externalRoutes));

                    foreach (var t in external)
                    {
                        SetHelpInfo(t.header, dictionary.TryGetC(t.token.GetPropertyRoute()));
                    }
                }
            };

            sc.ContextMenuOpened += async cm =>
            {
                if (helpButton.IsChecked == true && !sc.Implementations.IsByAll)
                {
                    var pairs = (from mi in cm.Items.OfType<MenuItem>()
                                 where mi.Tag is IContextualOperationContext
                                 select new { mi, coc = (IContextualOperationContext)mi.Tag })
                                 .ToList();

                    var operations = pairs.Select(p=>p.coc.OperationInfo.OperationSymbol).Distinct().ToList();
                    if(operations.Any())
                    {
                        var types = await Task.WhenAll(sc.Implementations.Types.Select(t=> Server.ReturnAsync((IHelpServer s) => s.GetEntityHelpService(t))));
            
                        foreach (var p in pairs)
                        {
                            SetHelpInfo(p.mi, types.Select(t => t.Operations.TryGetC(p.coc.OperationInfo.OperationSymbol)).NotNull().FirstOrDefault());
                        }
                    }
                }
            };

            helpButton.IsEnabled = true;
        }

      
    }
}
