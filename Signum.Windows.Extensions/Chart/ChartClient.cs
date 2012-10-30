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

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<UserChartDN>(EntityType.Default) { View = e => new UserChart() },
                    new EntitySettings<ChartScriptDN>(EntityType.Default) { View = e => new ChartScript() },
                    new EmbeddedEntitySettings<ChartScriptParameterDN> { View = (e,p) => new ChartScriptParameter(p) }
                });

                SearchControl.GetCustomMenuItems += new MenuItemForQueryName(SearchControl_GetCustomMenuItems);

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
            Icon = ExtensionsImageLoader.GetImageSortName("chartIcon.png").ToSmallImage();
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
