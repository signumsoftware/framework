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
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows;

namespace Signum.Windows.Chart
{
    public static class ChartClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryClient.Start();

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<UserChartDN>(EntityType.Main) { View = e => new UserChart() },
                    new EntitySettings<ChartScriptDN>(EntityType.Main) { View = e => new ChartScript() },
                    new EmbeddedEntitySettings<ChartScriptParameterDN> { View = (e,p) => new ChartScriptParameter(p) }
                });

                SearchControl.GetMenuItems += SearchControl_GetCustomMenuItems;

                UserChartDN.SetConverters(query => QueryClient.GetQueryName(query.Key), queryname => QueryClient.GetQuery(queryname));

                string processName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

                var main = Registry.CurrentUser
                    .OpenSubKey("Software")
                    .OpenSubKey("Microsoft")
                    .OpenSubKey("Internet Explorer")
                    .OpenSubKey("Main", true)
                    .CreateSubKey("FeatureControl")
                    .CreateSubKey("FEATURE_BROWSER_EMULATION");

                main.SetValue(processName, 9999, RegistryValueKind.DWord);

                ChartUtils.RemoveNotNullValidators();
            }
        }


        static MenuItem SearchControl_GetCustomMenuItems(SearchControl sc)
        {
            if (!ChartPermissions.ViewCharting.IsAuthorized())
                return null;

            var miResult = new MenuItem
            {

                Header = Signum.Windows.Extensions.Properties.Resources.Chart,
                Icon = ExtensionsImageLoader.GetImageSortName("chartIcon.png").ToSmallImage()
            };

            miResult.Click += delegate
            {
                ChartRequestWindow window = new ChartRequestWindow()
                {
                    FilterOptions = sc.FilterOptions,
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
