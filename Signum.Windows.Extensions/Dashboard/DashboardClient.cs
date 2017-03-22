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
                UserAssetsClient.RegisterExportAssertLink<DashboardEntity>();

                Navigator.AddSettings(new List<EntitySettings>()
                {
                    new EntitySettings<DashboardEntity>() { View = e => new DashboardEdit(), Icon = ExtensionsImageLoader.GetImageSortName("dashboard.png") },

                    new EntitySettings<ValueUserQueryListPartEntity>() { View = e => new ValueUserQueryListPartEntityEdit() },
                    new EntitySettings<LinkListPartEntity>() { View = e => new LinkListPartEdit() },
                    new EntitySettings<UserQueryPartEntity>() { View = e => new UserQueryPartEdit() },                
                    new EntitySettings<UserChartPartEntity>() { View = e => new UserChartPartEdit() }
                });

                PartViews.Add(typeof(UserQueryPartEntity), new PartView
                {
                    ViewControl = () => new UserQueryPartView(),
                    IsTitleEnabled = () => Navigator.IsNavigable(typeof(UserQueryEntity), true),
                    OnTitleClick = part =>
                    {
                        Navigator.Navigate(((UserQueryPartEntity)part).UserQuery);
                    },
                    FullScreen = (elem, part) =>
                    {
                        UserQueryClient.Explore(((UserQueryPartEntity)part).UserQuery, UserAssetsClient.GetCurrentEntity(elem)); 
                    }
                });

                PartViews.Add(typeof(UserChartPartEntity), new PartView
                {
                    ViewControl = () => new UserChartPartView(),
                    IsTitleEnabled = ()=> Navigator.IsNavigable(typeof(UserChartEntity), true),
                    OnTitleClick = part =>
                    {
                        Navigator.Navigate(((UserChartPartEntity)part).UserChart);
                    },
                    FullScreen = (elem, part) =>
                    {
                        ChartClient.View(((UserChartPartEntity)part).UserChart, UserAssetsClient.GetCurrentEntity(elem));
                    }
                });

                PartViews.Add(typeof(ValueUserQueryListPartEntity), new PartView
                {
                    ViewControl = () => new CountSearchControlPartView()
                });

                PartViews.Add(typeof(LinkListPartEntity), new PartView
                {
                    ViewControl = () => new LinkListPartView()
                });

                LinksClient.RegisterEntityLinks<DashboardEntity>((cp, ctrl) => new[]
                {  
                    new QuickLinkAction(DashboardMessage.Preview, () => Navigate(cp, null)) 
                    {
                        IsVisible = DashboardPermission.ViewDashboard.IsAuthorized() 
                    }
                });

                LinksClient.RegisterEntityLinks<Entity>((entity, ctrl) =>
                {       
                    if(!DashboardPermission.ViewDashboard.IsAuthorized())
                        return null;

                    return Server.Return((IDashboardServer us) => us.GetDashboardsEntity(entity.EntityType))
                        .Select(cp => new DashboardQuickLink(cp, entity)).ToArray();
                });

                Navigator.Manager.OnGetEmbeddedWigets += (e, ctx) =>
                {
                    if (!DashboardPermission.ViewDashboard.IsAuthorized() || !(e is Entity))
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
                                    throw new InvalidOperationException("Unexpected")
                    }; 

                };
            }
        }

        class DashboardQuickLink : QuickLink
        {
            Lite<DashboardEntity> dashboard;
            Lite<Entity> entity;

            public DashboardQuickLink(Lite<DashboardEntity> dashboard, Lite<Entity> entity)
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

        public static void Navigate(Lite<DashboardEntity> dashboard, Entity currentEntity)
        {
            Navigator.OpenIndependentWindow(() => new DashboardWindow
            {
                tbDashboard = { Text = NormalWindowMessage.Loading0.NiceToString().FormatWith(dashboard.EntityType.NiceName()) }
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
        public Action<IPartEntity> OnTitleClick;
        public Func<bool> IsTitleEnabled;
        public Action<FrameworkElement, IPartEntity> FullScreen; 
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
