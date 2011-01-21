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
        public static string ViewPrefix = "~/ControlPanel/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserQueriesClient.Start();

                Navigator.RegisterArea(typeof(ControlPanelClient));


                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ControlPanelDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("ControlPanelAdmin") },
                    new EmbeddedEntitySettings<PanelPart>() { PartialViewName = e => ViewPrefix.Formato("PanelPart") },
                    
                    //new EntitySettings<SearchControlPartDN>(EntityType.NotSaving) { PartialViewName = e => RouteHelper.AreaView("SearchControlPart" },
                    
                    new EntitySettings<CountSearchControlPartDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("CountSearchControlPart") },
                    new EmbeddedEntitySettings<CountUserQueryElement>() { PartialViewName = e => ViewPrefix.Formato("CountUserQueryElement") },
                    
                    new EntitySettings<LinkListPartDN>(EntityType.Default) { PartialViewName = e => ViewPrefix.Formato("LinkListPart") },
                    new EmbeddedEntitySettings<LinkElement>() { PartialViewName = e => ViewPrefix.Formato("LinkElement") },
                });

                Constructor.ConstructorManager.Constructors.Add(
                    typeof(ControlPanelDN), () => new ControlPanelDN { Related = UserDN.Current.ToLite<IdentifiableEntity>() });
            }
        }
    }
}
