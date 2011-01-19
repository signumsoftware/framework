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
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserQueriesClient.Start();

                Navigator.RegisterArea(typeof(ControlPanelClient)); 

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ControlPanelDN>(EntityType.Default) { PartialViewName = e => RouteHelper.AreaView("ControlPanelAdmin", "ControlPanel") },
                    new EmbeddedEntitySettings<PanelPart>() { PartialViewName = e => RouteHelper.AreaView("PanelPart", "ControlPanel") },
                    
                    //new EntitySettings<SearchControlPartDN>(EntityType.NotSaving) { PartialViewName = e => RouteHelper.AreaView("SearchControlPart" },
                    
                    new EntitySettings<CountSearchControlPartDN>(EntityType.Default) { PartialViewName = e => RouteHelper.AreaView("CountSearchControlPart", "ControlPanel") },
                    new EmbeddedEntitySettings<CountUserQueryElement>() { PartialViewName = e => RouteHelper.AreaView("CountUserQueryElement", "ControlPanel") },
                    
                    new EntitySettings<LinkListPartDN>(EntityType.Default) { PartialViewName = e => RouteHelper.AreaView("LinkListPart", "ControlPanel") },
                    new EmbeddedEntitySettings<LinkElement>() { PartialViewName = e => RouteHelper.AreaView("LinkElement", "ControlPanel") },
                });

                Constructor.ConstructorManager.Constructors.Add(
                    typeof(ControlPanelDN), () => new ControlPanelDN { Related = UserDN.Current.ToLite<IdentifiableEntity>() });
            }
        }
    }
}
