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

namespace Signum.Web.Dashboard
{
    public class DashboardClient
    {
        public static string AdminViewPrefix = "~/Dashboard/Views/Admin/{0}.cshtml";
        public static string ViewPrefix = "~/Dashboard/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/Dashboard/Scripts/Dashboard");
        public static JsModule GridRepeater = new JsModule("Extensions/Signum.Web.Extensions/Dashboard/Scripts/GridRepeater");

        public struct PartViews
        {
            public PartViews(string frontEnd, string admin)
            {
                FrontEnd = frontEnd;
                Admin = admin;
                FullScreenLink = false;
            }

            public string FrontEnd;
            public string Admin;
            public bool FullScreenLink;
        }

        public static Dictionary<Type, PartViews> PanelPartViews = new Dictionary<Type, PartViews>()
        {
            { typeof(UserChartPartDN), new PartViews(ViewPrefix.Formato("UserChartPart"), AdminViewPrefix.Formato("UserChartPart")) { FullScreenLink = true } },
            { typeof(UserQueryPartDN), new PartViews(ViewPrefix.Formato("SearchControlPart"), AdminViewPrefix.Formato("SearchControlPart")) { FullScreenLink = true } },
            { typeof(CountSearchControlPartDN), new PartViews(ViewPrefix.Formato("CountSearchControlPart"), AdminViewPrefix.Formato("CountSearchControlPart")) },
            { typeof(LinkListPartDN), new PartViews(ViewPrefix.Formato("LinkListPart"), AdminViewPrefix.Formato("LinkListPart")) },
        };

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

                    return DashboardLogic.GetDashboardEntity(entity.EntityType)
                        .Select(cp => new DashboardQuickLink(cp, entity)).ToArray(); 
                });
            }
        }

        class DashboardQuickLink : QuickLink
        {
            Lite<DashboardDN> controlPanel;
            Lite<IdentifiableEntity> entity;

            public DashboardQuickLink(Lite<DashboardDN> controlPanel, Lite<IdentifiableEntity> entity)
            {
                this.Text = controlPanel.ToString();
                this.controlPanel = controlPanel;
                this.entity = entity;
                this.IsVisible = true;
            }

            public override MvcHtmlString Execute()
            {
                return new HtmlTag("a").Attr("href", RouteHelper.New().Action((DashboardController c) => c.View(controlPanel, entity))).SetInnerText(Text);
            }
        }
    }
}
