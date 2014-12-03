using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities.Dashboard;
using Signum.Entities.Authorization;
using System.Reflection;
using System.Web.Routing;
using Signum.Web.UserQueries;
using Signum.Entities;
using Signum.Web.Controllers;
using Signum.Engine.Dashboard;
using Signum.Engine.Authorization;
using Signum.Web.UserAssets;
using System.Web.Mvc.Html;

namespace Signum.Web.Dashboard
{
    public class DashboardClient
    {
        public static string AdminViewPrefix = "~/Dashboard/Views/Admin/{0}.cshtml";
        public static string ViewPrefix = "~/Dashboard/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Dashboard/Scripts/Dashboard");
        public static JsModule GridRepeater = new JsModule("Extensions/Signum.Web.Extensions/Dashboard/Scripts/GridRepeater");

        public class PartViews
        {
            public PartViews(string frontEnd, string admin)
            {
                FrontEndView = frontEnd;
                AdminView = admin;
                HasFullScreenLink = false;
            }

            public string FrontEndView;
            public string AdminView;
            public Func<IPartEntity, string> TitleLink;
            public bool HasFullScreenLink;
        }

        public static Dictionary<Type, PartViews> PanelPartViews = new Dictionary<Type, PartViews>()
        {
            { typeof(UserChartPartEntity), new PartViews(ViewPrefix.FormatWith("UserChartPart"), AdminViewPrefix.FormatWith("UserChartPart")) { HasFullScreenLink = true, TitleLink = p=> NavigateRoute(((UserChartPartEntity)p).UserChart) }},
            { typeof(UserQueryPartEntity), new PartViews(ViewPrefix.FormatWith("SearchControlPart"), AdminViewPrefix.FormatWith("SearchControlPart")) { HasFullScreenLink = true, TitleLink = p=> NavigateRoute(((UserQueryPartEntity)p).UserQuery) }},
            { typeof(CountSearchControlPartEntity), new PartViews(ViewPrefix.FormatWith("CountSearchControlPart"), AdminViewPrefix.FormatWith("CountSearchControlPart")) },
            { typeof(LinkListPartEntity), new PartViews(ViewPrefix.FormatWith("LinkListPart"), AdminViewPrefix.FormatWith("LinkListPart")) },
        };

        static string NavigateRoute(Entity entity)
        {
            if (!Navigator.IsNavigable(entity, null))
                return null;

            return Navigator.NavigateRoute(entity);
        }

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(DashboardClient));

                UserAssetsClient.Start();
                UserAssetsClient.RegisterExportAssertLink<DashboardEntity>();

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<DashboardEntity> { PartialViewName = e => AdminViewPrefix.FormatWith("DashboardAdmin") },
                    new EmbeddedEntitySettings<PanelPartEntity>(),
                    
                    new EntitySettings<UserChartPartEntity>(),

                    new EntitySettings<UserQueryPartEntity>(),

                    new EntitySettings<CountSearchControlPartEntity>(),
                    new EmbeddedEntitySettings<CountUserQueryElementEntity> { PartialViewName = e => AdminViewPrefix.FormatWith("CountUserQueryElement") },
                    
                    new EntitySettings<LinkListPartEntity>(),
                    new EmbeddedEntitySettings<LinkElementEntity> { PartialViewName = e => AdminViewPrefix.FormatWith("LinkElement") },
                });

                Constructor.Register(ctx => new DashboardEntity { Owner = UserEntity.Current.ToLite() });

                LinksClient.RegisterEntityLinks<DashboardEntity>((cp, ctx) => new[]
                {
                    !DashboardPermission.ViewDashboard.IsAuthorized() ? null:
                     new QuickLinkAction(DashboardMessage.Preview, RouteHelper.New().Action<DashboardController>(cpc => cpc.View(cp, null)))
                });

                LinksClient.RegisterEntityLinks<Entity>((entity, ctrl) =>
                {
                    if (!DashboardPermission.ViewDashboard.IsAuthorized())
                        return null;

                    return DashboardLogic.GetDashboardsEntity(entity.EntityType)
                        .Select(cp => new DashboardQuickLink(cp, entity)).ToArray();
                });

                WidgetsHelper.GetEmbeddedWidget += ctx =>
                {
                    if (!DashboardPermission.ViewDashboard.IsAuthorized() || !(ctx.Entity is Entity) || ((Entity)ctx.Entity).IsNew)
                        return null;

                    var dashboard = DashboardLogic.GetEmbeddedDashboard(ctx.Entity.GetType());
                    if (dashboard == null)
                        return null;

                    return new DashboardEmbeddedWidget { Dashboard = dashboard, Entity = (Entity)ctx.Entity };
                };
            }
        }

        class DashboardEmbeddedWidget : IEmbeddedWidget
        {
            public DashboardEntity Dashboard { get; set; }

            public Entity Entity { get; set; }

            public MvcHtmlString ToHtml(HtmlHelper helper)
            {
                return helper.Partial(DashboardClient.ViewPrefix.FormatWith("DashboardView"), Dashboard,
                    new ViewDataDictionary { { "currentEntity", Entity } });
            }

            public EmbeddedWidgetPostion Position
            {
                get
                {
                    return Dashboard.EmbeddedInEntity.Value == DashboardEmbedededInEntity.Top ? EmbeddedWidgetPostion.Top :
                        Dashboard.EmbeddedInEntity.Value == DashboardEmbedededInEntity.Bottom ? EmbeddedWidgetPostion.Bottom :
                        new InvalidOperationException("Unexpected {0}".FormatWith(Dashboard.EmbeddedInEntity.Value)).Throw<EmbeddedWidgetPostion>();
                }
            }
        }

        class DashboardQuickLink : QuickLink
        {
            Lite<DashboardEntity> dashboard;
            Lite<Entity> entity;

            public DashboardQuickLink(Lite<DashboardEntity> dashboard, Lite<Entity> entity)
            {
                this.Text = dashboard.ToString();
                this.dashboard = dashboard;
                this.entity = entity;
                this.IsVisible = true;
                this.Glyphicon = "glyphicon-th-large";
                this.GlyphiconColor = "darkslateblue";
            }

            public override MvcHtmlString Execute()
            {
                return new HtmlTag("a").Attr("href", RouteHelper.New().Action((DashboardController c) => c.View(dashboard, entity))).InnerHtml(TextAndIcon());
            }
        }
    }
}
