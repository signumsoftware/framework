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
using Signum.Windows.Properties;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;

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
                QueryClient.Start();

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<UserChartDN> { View = e => new UserChart() },
                    new EntitySettings<ChartScriptDN> { View = e => new ChartScript() },
                    new EmbeddedEntitySettings<ChartScriptParameterDN> { View = (e,p) => new ChartScriptParameter(p) }
                });

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
    }
}
