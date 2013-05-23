using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using Signum.Windows.Chart;
using Signum.Entities.Chart;
using System.Reflection;
using Signum.Entities.Reports;
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

namespace Signum.Windows.Chart
{
    public static class ChartClient
    {
        public static readonly DependencyProperty UserChartProperty =
            DependencyProperty.RegisterAttached("UserChart", typeof(UserChartDN), typeof(ChartClient), new UIPropertyMetadata(null));
        public static UserChartDN GetUserChart(DependencyObject obj)
        {
            return (UserChartDN)obj.GetValue(UserChartProperty);
        }
        public static void SetUserChart(DependencyObject obj, UserChartDN value)
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
                    new EntitySettings<UserChartDN> { View = e => new UserChart(), Icon = ExtensionsImageLoader.GetImageSortName("chartIcon.png") },
                    new EntitySettings<ChartScriptDN> { View = e => new ChartScript(), Icon = ExtensionsImageLoader.GetImageSortName("chartScript.png") },
                    new EmbeddedEntitySettings<ChartScriptParameterDN> { View = (e,p) => new ChartScriptParameter(p) }
                });

                UserAssetsClient.Start();
                UserAssetsClient.RegisterExportAssertLink<UserChartDN>();

                SearchControl.GetMenuItems += SearchControl_GetCustomMenuItems;

                UserChartDN.SetConverters(query => QueryClient.GetQueryName(query.Key), queryname => QueryClient.GetQuery(queryname));

                string processName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

                var main = Registry.CurrentUser
                    .CreateSubKey("Software")
                    .CreateSubKey("Microsoft")
                    .CreateSubKey("Internet Explorer")
                    .CreateSubKey("Main")
                    .CreateSubKey("FeatureControl")
                    .CreateSubKey("FEATURE_BROWSER_EMULATION");

                main.SetValue(processName, 9999, RegistryValueKind.DWord);

                Constructor.Register<UserChartDN>(elem =>
                {
                    MessageBox.Show(Window.GetWindow(elem),
                        ChartMessage._0CanOnlyBeCreatedFromTheChartWindow.NiceToString().Formato(typeof(UserChartDN).NicePluralName()),
                        ChartMessage.UserChart_CreateNew.NiceToString(),
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return null;
                }); 

                ChartUtils.RemoveNotNullValidators();

                LinksClient.RegisterEntityLinks<IdentifiableEntity>((entity, ctrl) =>
                    Server.Return((IChartServer us) => us.GetUserChartsEntity(entity.EntityType))
                    .Select(cp => new UserChartQuickLink (cp, entity)).ToArray());
            }
        }

        class UserChartQuickLink : QuickLink
        {
            Lite<UserChartDN> userChart;
            Lite<IdentifiableEntity> entity;

            public UserChartQuickLink(Lite<UserChartDN> userChart, Lite<IdentifiableEntity> entity)
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
                ChartRequestWindow window = new ChartRequestWindow()
                {
                    DataContext = new ChartRequest(sc.QueryName)
                    {
                        Filters = sc.FilterOptions.Select(fo => fo.ToFilter()).ToList(),
                    }
                };

                window.Show();
            };

            return miResult;
        }

        internal static void View(UserChartDN uc, IdentifiableEntity currentEntity)
        {
            var query = QueryClient.GetQueryName(uc.Query.Key);

            if (uc.EntityType != null)
            {
                if (currentEntity == null)
                {
                    var entity = Navigator.Find(new FindOptions(Server.GetType(uc.EntityType.ToString())));

                    if (entity == null)
                        return;

                    currentEntity = entity.Retrieve();
                }

                CurrentEntityConverter.SetFilterValues(uc.Filters, currentEntity);
            }

            ChartRequestWindow cw = new ChartRequestWindow()
            {
                DataContext = new ChartRequest(query)
            };

            ChartClient.SetUserChart(cw, uc);

            cw.Show();
        }
    }
}
