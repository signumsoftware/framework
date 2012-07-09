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

namespace Signum.Windows.Chart
{
    public static class ChartClient
    {
        static Func<ChartRendererBase> RendererConstructor; 

        public static void Start(Func<ChartRendererBase> rendererConstructor)
        {
            if(Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (rendererConstructor == null)
                    throw new ArgumentNullException("rendererConstructor"); 

                RendererConstructor = rendererConstructor; 

                QueryClient.Start();
                
                Navigator.AddSetting(new EntitySettings<UserChartDN>(EntityType.Default) { View = e => new UserChart(), IsCreable = a => a });
                Constructor.Register<UserChartDN>(win =>
                {
                    MessageBox.Show(win, 
                        Signum.Windows.Extensions.Properties.Resources._0CanOnlyBeCreatedFromTheChartWindow.Formato(typeof(UserChartDN).NicePluralName()),
                        Signum.Windows.Extensions.Properties.Resources.Create,
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return null;
                }); 
                SearchControl.GetCustomMenuItems += new MenuItemForQueryName(SearchControl_GetCustomMenuItems);

                UserChartDN.SetConverters(query => QueryClient.GetQueryName(query.Key), queryname => QueryClient.GetQuery(queryname));
            }
        }

        static SearchControlMenuItem SearchControl_GetCustomMenuItems(object queryName, Type entityType)
        {
            if (ChartPermissions.ViewCharting.IsAuthorized())
                return new ChartMenuItem();

            return null; 
        }

        internal static ChartRendererBase GetChartRenderer()
        {
            return RendererConstructor(); 
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
            ChartWindow window = new ChartWindow()
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
