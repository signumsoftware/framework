using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Entities.ControlPanel;
using Signum.Entities.Authorization;
using System.Reflection;
using System.Web.Routing;
using Signum.Web.UserQueries;
using Signum.Entities;
using Signum.Entities.Reports;
using Signum.Web.Controllers;
using Signum.Engine.ControlPanel;
using Signum.Engine.Authorization;
using Signum.Web.Extensions.UserQueries;

namespace Signum.Web.ControlPanel
{
    public class ControlPanelClient
    {
        public static string AdminViewPrefix = "~/ControlPanel/Views/Admin/{0}.cshtml";
        public static string ViewPrefix = "~/ControlPanel/Views/{0}.cshtml";
        public static JsModule Module = new JsModule("Extensions/Signum.Web.Extensions/ControlPanel/Scripts/ControlPanel");
        public static JsModule GridRepeater = new JsModule("Extensions/Signum.Web.Extensions/ControlPanel/Scripts/GridRepeater");

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
                UserQueriesClient.Start();

                Navigator.RegisterArea(typeof(ControlPanelClient));

                UserAssetsClient.Start();
                UserAssetsClient.RegisterExportAssertLink<ControlPanelDN>();

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ControlPanelDN> { PartialViewName = e => AdminViewPrefix.Formato("ControlPanelAdmin") },
                    new EmbeddedEntitySettings<PanelPartDN>(),
                    
                    new EntitySettings<UserChartPartDN>(),

                    new EntitySettings<UserQueryPartDN>(),

                    new EntitySettings<CountSearchControlPartDN>(),
                    new EmbeddedEntitySettings<CountUserQueryElementDN> { PartialViewName = e => AdminViewPrefix.Formato("CountUserQueryElement") },
                    
                    new EntitySettings<LinkListPartDN>(),
                    new EmbeddedEntitySettings<LinkElementDN> { PartialViewName = e => AdminViewPrefix.Formato("LinkElement") },
                });

                Constructor.Manager.Constructors.Add(
                    typeof(ControlPanelDN), () => new ControlPanelDN { Owner = UserDN.Current.ToLite() });

                LinksClient.RegisterEntityLinks<ControlPanelDN>((cp, ctx) => new[]
                {
                    !ControlPanelPermission.ViewControlPanel.IsAuthorized() ? null:
                     new QuickLinkAction(ControlPanelMessage.Preview, RouteHelper.New().Action<ControlPanelController>(cpc => cpc.View(cp, null)))
                });
           
                LinksClient.RegisterEntityLinks<IdentifiableEntity>((entity, ctrl) =>
                {
                    if (!ControlPanelPermission.ViewControlPanel.IsAuthorized())
                        return null;

                    return ControlPanelLogic.GetControlPanelsEntity(entity.EntityType)
                        .Select(cp => new ControlPanelQuickLink(cp, entity)).ToArray(); 
                });
            }
        }

        class ControlPanelQuickLink : QuickLink
        {
            Lite<ControlPanelDN> controlPanel;
            Lite<IdentifiableEntity> entity;

            public ControlPanelQuickLink(Lite<ControlPanelDN> controlPanel, Lite<IdentifiableEntity> entity)
            {
                this.Text = controlPanel.ToString();
                this.controlPanel = controlPanel;
                this.entity = entity;
                this.IsVisible = true;
            }

            public override MvcHtmlString Execute()
            {
                return new HtmlTag("a").Attr("href", RouteHelper.New().Action((ControlPanelController c) => c.View(controlPanel, entity))).SetInnerText(Text);
            }
        }
    }
}
