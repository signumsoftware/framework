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
using Signum.Web.Queries;
using Signum.Entities;

namespace Signum.Web.ControlPanel
{
    public class ControlPanelClient
    {
        public static string ViewPrefix = "controlPanel/Views/";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserQueriesClient.Start();

                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(ControlPanelClient), "~/controlPanel/", "Signum.Web.Extensions.ControlPanel."));

                RouteTable.Routes.InsertRouteAt0("controlPanel/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "controlPanel" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<ControlPanelDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "ControlPanelAdmin" },
                    new EntitySettings<PanelPart>(EntityType.NotSaving) { PartialViewName = e => ViewPrefix + "PanelPart" },
                    
                    //new EntitySettings<SearchControlPartDN>(EntityType.NotSaving) { PartialViewName = e => ViewPrefix + "SearchControlPart" },
                    
                    new EntitySettings<CountSearchControlPartDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "CountSearchControlPart" },
                    new EntitySettings<CountUserQueryElement>(EntityType.NotSaving) { PartialViewName = e => ViewPrefix + "CountUserQueryElement" },
                    
                    new EntitySettings<LinkListPartDN>(EntityType.Default) { PartialViewName = e => ViewPrefix + "LinkListPart" },
                    new EntitySettings<LinkElement>(EntityType.NotSaving) { PartialViewName = e => ViewPrefix + "LinkElement" },
                });

                Constructor.ConstructorManager.Constructors.Add(
                    typeof(ControlPanelDN), () => new ControlPanelDN { Related = UserDN.Current.ToLite<IdentifiableEntity>() });
            }
        }
    }
}
