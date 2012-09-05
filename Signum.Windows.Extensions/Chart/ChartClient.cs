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

namespace Signum.Windows.Chart
{
    public static class ChartClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryClient.Start();

                Navigator.AddSetting(new EntitySettings<UserChartDN>(EntityType.Default) { View = e => new UserChart() });
                SearchControl.GetCustomMenuItems += new MenuItemForQueryName(SearchControl_GetCustomMenuItems);

                UserChartDN.SetConverters(query => QueryClient.GetQueryName(query.Key), queryname => QueryClient.GetQuery(queryname));

                string processName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

                Registry.CurrentUser
                    .OpenSubKey("Software")
                    .OpenSubKey("Microsoft")
                    .OpenSubKey("Internet Explorer")
                    .OpenSubKey("Main")
                    .OpenSubKey("FeatureControl")
                    .OpenSubKey("FEATURE_BROWSER_EMULATION", true)
                    .SetValue(processName, 9999, RegistryValueKind.DWord);
            }
        }


        static SearchControlMenuItem SearchControl_GetCustomMenuItems(object queryName, Type entityType)
        {
            if (ChartPermissions.ViewCharting.IsAuthorized())
                return new ChartMenuItem();

            return null; 
        }
    }

    internal class ChartMenuItem : SearchControlMenuItem
    {
        public ChartMenuItem()
        {
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Header = Signum.Windows.Extensions.Properties.Resources.Chart;
            Icon = ExtensionsImageLoader.GetImageSortName("charts/chartIcon.png").ToSmallImage();
        }

        protected override void OnClick()
        {
            ChartRequestWindow window = new ChartRequestWindow()
            {
                FilterOptions = this.SearchControl.FilterOptions,
                DataContext = new ChartRequest(this.SearchControl.QueryName)
                {
                    Filters = this.SearchControl.FilterOptions.Select(fo=>fo.ToFilter()).ToList(),
                }
            };

            window.Show(); 
        }
    }
}
