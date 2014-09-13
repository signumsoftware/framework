using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Dashboard;
using System.Reflection;
using Signum.Windows.Dashboard.Admin;
using System.Windows.Controls;
using Signum.Entities;
using System.Windows;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Windows.Dashboard;
using Signum.Windows.Basics;
using Signum.Entities.UserQueries;
using Signum.Entities.Reflection;
using Signum.Services;
using Signum.Windows.Authorization;
using Signum.Entities.Chart;
using Signum.Windows.UserAssets;
using Signum.Windows.UserQueries;
using Signum.Windows.Chart;

namespace Signum.Windows.Dashboard
{
    public static class DashboardClient
    {
        public static Dictionary<Type, PartView> PartViews = new Dictionary<Type, PartView>();

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                TypeClient.Start();

                UserAssetsClient.Start();
                UserAssetsClient.RegisterExportAssertLink<DashboardDN>();

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<DashboardDN>() { View = e => new DashboardEdit(), Icon = ExtensionsImageLoader.GetImageSortName("dashboard.png") },

                    new EntitySettings<CountSearchControlPartDN>() { View = e => new CountSearchControlPartEdit() },
                    new EntitySettings<LinkListPartDN>() { View = e => new LinkListPartEdit() },
                    new EntitySettings<UserQueryPartDN>() { View = e => new UserQueryPartEdit() },                
                    new EntitySettings<UserChartPartDN>() { View = e => new UserChartPartEdit() }
                });

                PartViews.Add(typeof(UserQueryPartDN), new PartView
                {
                    ViewControl = () => new UserQueryPartView(),
                    IsTitleEnabled = () => Navigator.IsNavigable(typeof(UserQueryDN), true),
                    OnTitleClick = part =>
                    {
                        Navigator.Navigate(((UserQueryPartDN)part).UserQuery);
                    },
                    FullScreen = (elem, part) =>
                    {
                        UserQueryClient.Explore(((UserQueryPartDN)part).UserQuery, UserAssetsClient.GetCurrentEntity(elem)); 
                    }
                });

                PartViews.Add(typeof(UserChartPartDN), new PartView
                {
                    ViewControl = () => new UserChartPartView(),
                    IsTitleEnabled = ()=> Navigator.IsNavigable(typeof(UserChartDN), true),
                    OnTitleClick = part =>
                    {
                        Navigator.Navigate(((UserChartPartDN)part).UserChart);
                    },
                    FullScreen = (elem, part) =>
                    {
                        ChartClient.View(((UserChartPartDN)part).UserChart, UserAssetsClient.GetCurrentEntity(elem));
                    }
                });

                PartViews.Add(typeof(CountSearchControlPartDN), new PartView
                {
                    ViewControl = () => new CountSearchControlPartView()
                });

                PartViews.Add(typeof(LinkListPartDN), new PartView
                {
                    ViewControl = () => new LinkListPartView()
                });

                LinksClient.RegisterEntityLinks<DashboardDN>((cp, ctrl) => new[]
                {  
                    new QuickLinkAction(DashboardMessage.Preview, () => Navigate(cp, null)) 
                    {
                        IsVisible = DashboardPermission.ViewDashboard.IsAuthorized() 
                    }
                });

                LinksClient.RegisterEntityLinks<IdentifiableEntity>((entity, ctrl) =>
                {       
                    if(!DashboardPermission.ViewDashboard.IsAuthorized())
                        return null;

                    return Server.Return((IDashboardServer us) => us.GetDashboardsEntity(entity.EntityType))
                        .Select(cp => new DashboardQuickLink(cp, entity)).ToArray();
                });

                Navigator.Manager.OnGetEmbeddedWigets += (e, ctx) =>
                {
                    if (!DashboardPermission.ViewDashboard.IsAuthorized() || !(e is IdentifiableEntity))
                        return null;

                    var dashboard = Server.Return((IDashboardServer s) => s.GetEmbeddedDashboard(e.GetType()));
                    if (dashboard == null)
                        return null;

                    var control = new DashboardView { DataContext = dashboard }.Set(UserAssetsClient.CurrentEntityProperty, e);

                    return new EmbeddedWidget
                    {
                         Control = control, 
                         Position = dashboard.EmbeddedInEntity.Value == DashboardEmbedededInEntity.Top ? EmbeddedWidgetPostion.Top:
                                    dashboard.EmbeddedInEntity.Value == DashboardEmbedededInEntity.Bottom ? EmbeddedWidgetPostion.Bottom:
                                    new InvalidOperationException("Unexpected").Throw<EmbeddedWidgetPostion>()
                    }; 

                };
            }
        }

        class DashboardQuickLink : QuickLink
        {
            Lite<DashboardDN> dashboard;
            Lite<IdentifiableEntity> entity;

            public DashboardQuickLink(Lite<DashboardDN> dashboard, Lite<IdentifiableEntity> entity)
            {
                this.ToolTip = dashboard.ToString(); 
                this.Label = dashboard.ToString();
                this.dashboard = dashboard;
                this.entity = entity;
                this.IsVisible = true;
                this.Icon = ExtensionsImageLoader.GetImageSortName("dashboard.png");
            }

            public override void Execute()
            {
                DashboardClient.Navigate(dashboard, entity.Retrieve());
            }

            public override string Name
            {
                get { return dashboard.Key(); }
            }
        }

        public static void Navigate(Lite<DashboardDN> dashboard, IdentifiableEntity currentEntity)
        {
            Navigator.OpenIndependentWindow(() => new DashboardWindow
            {
                tbDashboard = { Text = NormalWindowMessage.Loading0.NiceToString().Formato(dashboard.EntityType.NiceName()) }
            },
            afterShown: win =>
            {
                var cp = dashboard.Retrieve();

                if (cp.EntityType != null)
                {
                    UserAssetsClient.SetCurrentEntity(win, currentEntity);
                }

                win.DataContext = dashboard.Retrieve();
            });
        }
    }

    public class PartView
    {
        public Expression<Func<FrameworkElement>> ViewControl;
        public Action<IPartDN> OnTitleClick;
        public Func<bool> IsTitleEnabled;
        public Action<FrameworkElement, IPartDN> FullScreen; 
    }

    public class DashboardViewDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            PartView pv = DashboardClient.PartViews.TryGetC(item.GetType());

            if (pv == null)
                return null;

            return Fluent.GetDataTemplate(pv.ViewControl);
        }
    }
}
