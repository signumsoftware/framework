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
using Signum.Web.Extensions.UserQueries;
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
            public Func<IPartDN, string> TitleLink;
            public bool HasFullScreenLink;
        }

        public static Dictionary<Type, PartViews> PanelPartViews = new Dictionary<Type, PartViews>()
        {
            { typeof(UserChartPartDN), new PartViews(ViewPrefix.Formato("UserChartPart"), AdminViewPrefix.Formato("UserChartPart")) { HasFullScreenLink = true, TitleLink = p=> NavigateRoute(((UserChartPartDN)p).UserChart) }},
            { typeof(UserQueryPartDN), new PartViews(ViewPrefix.Formato("SearchControlPart"), AdminViewPrefix.Formato("SearchControlPart")) { HasFullScreenLink = true, TitleLink = p=> NavigateRoute(((UserQueryPartDN)p).UserQuery) }},
            { typeof(CountSearchControlPartDN), new PartViews(ViewPrefix.Formato("CountSearchControlPart"), AdminViewPrefix.Formato("CountSearchControlPart")) },
            { typeof(LinkListPartDN), new PartViews(ViewPrefix.Formato("LinkListPart"), AdminViewPrefix.Formato("LinkListPart")) },
        };

        static string NavigateRoute(IdentifiableEntity entity)
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
                UserAssetsClient.RegisterExportAssertLink<DashboardDN>();

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<DashboardDN> { PartialViewName = e => AdminViewPrefix.Formato("DashboardAdmin") },
                    new EmbeddedEntitySettings<PanelPartDN>(),
                    
                    new EntitySettings<UserChartPartDN>(),

                    new EntitySettings<UserQueryPartDN>(),

                    new EntitySettings<CountSearchControlPartDN>(),
                    new EmbeddedEntitySettings<CountUserQueryElementDN> { PartialViewName = e => AdminViewPrefix.Formato("CountUserQueryElement") },
                    
                    new EntitySettings<LinkListPartDN>(),
                    new EmbeddedEntitySettings<LinkElementDN> { PartialViewName = e => AdminViewPrefix.Formato("LinkElement") },
                });

                Constructor.Register(ctx => new DashboardDN { Owner = UserDN.Current.ToLite() });

                LinksClient.RegisterEntityLinks<DashboardDN>((cp, ctx) => new[]
                {
                    !DashboardPermission.ViewDashboard.IsAuthorized() ? null:
                     new QuickLinkAction(DashboardMessage.Preview, RouteHelper.New().Action<DashboardController>(cpc => cpc.View(cp, null)))
                });

                LinksClient.RegisterEntityLinks<IdentifiableEntity>((entity, ctrl) =>
                {
                    if (!DashboardPermission.ViewDashboard.IsAuthorized())
                        return null;

                    return DashboardLogic.GetDashboardsEntity(entity.EntityType)
                        .Select(cp => new DashboardQuickLink(cp, entity)).ToArray();
                });

                WidgetsHelper.GetEmbeddedWidget += ctx =>
                {
                    if (!DashboardPermission.ViewDashboard.IsAuthorized() || !(ctx.Entity is IdentifiableEntity))
                        return null;

                    var dashboard = DashboardLogic.GetEmbeddedDashboard(ctx.Entity.GetType());
                    if (dashboard == null)
                        return null;

                    return new DashboardEmbeddedWidget { Dashboard = dashboard, Entity = (IdentifiableEntity)ctx.Entity };
                };
            }
        }

        class DashboardEmbeddedWidget : IEmbeddedWidget
        {
            public DashboardDN Dashboard { get; set; }

            public IdentifiableEntity Entity { get; set; }

            public MvcHtmlString ToHtml(HtmlHelper helper)
            {
                return helper.Partial(DashboardClient.ViewPrefix.Formato("DashboardView"), Dashboard,
                    new ViewDataDictionary { { "currentEntity", Entity } });
            }

            public EmbeddedWidgetPostion Position
            {
                get
                {
                    return Dashboard.EmbeddedInEntity.Value == DashboardEmbedededInEntity.Top ? EmbeddedWidgetPostion.Top :
                        Dashboard.EmbeddedInEntity.Value == DashboardEmbedededInEntity.Bottom ? EmbeddedWidgetPostion.Bottom :
                        new InvalidOperationException("Unexpected {0}".Formato(Dashboard.EmbeddedInEntity.Value)).Throw<EmbeddedWidgetPostion>();
                }
            }
        }

        class DashboardQuickLink : QuickLink
        {
            Lite<DashboardDN> dashboard;
            Lite<IdentifiableEntity> entity;

            public DashboardQuickLink(Lite<DashboardDN> dashboard, Lite<IdentifiableEntity> entity)
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
