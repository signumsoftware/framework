using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Windows.Chart;
using Signum.Entities.Chart;
using System.Reflection;
using Signum.Entities.Authorization;
using Signum.Windows.Authorization;
using System.Windows;
using Signum.Utilities;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using Signum.Entities;
using Signum.Windows.Basics;
using Signum.Entities.UserQueries;
using Signum.Services;
using Signum.Windows.UserQueries;
using Signum.Windows.UserAssets;

namespace Signum.Windows.Chart
{
    public static class ChartClient
    {
        public static readonly DependencyProperty UserChartProperty =
            DependencyProperty.RegisterAttached("UserChart", typeof(UserChartEntity), typeof(ChartClient), new UIPropertyMetadata(null));
        public static UserChartEntity GetUserChart(DependencyObject obj)
        {
            return (UserChartEntity)obj.GetValue(UserChartProperty);
        }
        public static void SetUserChart(DependencyObject obj, UserChartEntity value)
        {
            obj.SetValue(UserChartProperty, value);
        }


        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                TypeClient.Start();
                QueryClient.Start();

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<UserChartEntity> { View = e => new UserChart(), Icon = ExtensionsImageLoader.GetImageSortName("chartIcon.png") },
                    new EntitySettings<ChartScriptEntity> { View = e => new ChartScript(), Icon = ExtensionsImageLoader.GetImageSortName("chartScript.png") },
                    new EmbeddedEntitySettings<ChartScriptParameterEntity> { View = e => new ChartScriptParameter() }
                });

                UserAssetsClient.Start();
                UserAssetsClient.RegisterExportAssertLink<UserChartEntity>();

                SearchControl.GetMenuItems += SearchControl_GetCustomMenuItems;

                UserChartEntity.SetConverters(query => QueryClient.GetQueryName(query.Key), queryname => QueryClient.GetQuery(queryname));

                string processName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

                var main = Registry.CurrentUser
                    .CreateSubKey("Software")
                    .CreateSubKey("Microsoft")
                    .CreateSubKey("Internet Explorer")
                    .CreateSubKey("Main")
                    .CreateSubKey("FeatureControl")
                    .CreateSubKey("FEATURE_BROWSER_EMULATION");

                main.SetValue(processName, 9999, RegistryValueKind.DWord);

                Constructor.Register<UserChartEntity>(ctx =>
                {
                    MessageBox.Show(Window.GetWindow(ctx.Element),
                        ChartMessage._0CanOnlyBeCreatedFromTheChartWindow.NiceToString().FormatWith(typeof(UserChartEntity).NicePluralName()),
                        ChartMessage.CreateNew.NiceToString(),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return null;
                }); 

                LinksClient.RegisterEntityLinks<Entity>((entity, ctrl) =>
                    Server.Return((IChartServer us) => us.GetUserChartsEntity(entity.EntityType))
                    .Select(cp => new UserChartQuickLink (cp, entity)).ToArray());
            }
        }

        class UserChartQuickLink : QuickLink
        {
            Lite<UserChartEntity> userChart;
            Lite<Entity> entity;

            public UserChartQuickLink(Lite<UserChartEntity> userChart, Lite<Entity> entity)
            {
                this.ToolTip = userChart.ToString();
                this.Label = userChart.ToString();
                this.userChart = userChart;
                this.entity = entity;
                this.IsVisible = true;
                this.Icon = ExtensionsImageLoader.GetImageSortName("chartIcon.png");
            }

            public override void Execute()
            {
                ChartClient.View(userChart.Retrieve(), entity.Retrieve());
            }

            public override string Name
            {
                get { return userChart.Key(); }
            }
        }

        static MenuItem SearchControl_GetCustomMenuItems(SearchControl sc)
        {
            if (!ChartPermission.ViewCharting.IsAuthorized())
                return null;

            var miResult = new MenuItem
            {
                Header = ChartMessage.Chart.NiceToString(),
                Icon = ExtensionsImageLoader.GetImageSortName("chartIcon.png").ToSmallImage()
            };

            miResult.Click += delegate
            {
                var cr = new ChartRequest(sc.QueryName)
                {
                    Filters = sc.FilterOptions.Select(fo => fo.ToFilter()).ToList(),
                };

                ChartClient.OpenChartRequest(cr, null, null);
            };

            return miResult;
        }

        internal static void View(UserChartEntity uc, Entity currentEntity)
        {
            if (uc.EntityType != null)
            {
                if (currentEntity == null)
                {
                    var entity = Finder.Find(new FindOptions(Server.GetType(uc.EntityType.ToString())));

                    if (entity == null)
                        return;

                    currentEntity = entity.Retrieve();
                }
            }

            var query = QueryClient.GetQueryName(uc.Query.Key);
            
            OpenChartRequest(new ChartRequest(query), uc, currentEntity);
        }

        internal static void OpenChartRequest(ChartRequest chartRequest, UserChartEntity uc, Entity currentEntity)
        {
            Navigator.OpenIndependentWindow(() => 
            {
                var crw = new ChartRequestWindow()
                {
                    DataContext = chartRequest,
                    Title = ChartMessage.ChartOf0.NiceToString().FormatWith(QueryUtils.GetNiceName(chartRequest.QueryName)),
                    Icon = Finder.Manager.GetFindIcon(chartRequest.QueryName, false) ?? ExtensionsImageLoader.GetImageSortName("chartIcon.png")
                };

                if (uc != null)
                    SetUserChart(crw, uc);

                if (currentEntity != null)
                    UserAssetsClient.SetCurrentEntity(crw, currentEntity);

                return crw; 
            });
        }
    }
}
