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

namespace Signum.Web.ControlPanel
{
    public class ControlPanelClient
    {
        public static string AdminViewPrefix = "~/ControlPanel/Views/Admin/{0}.cshtml";
        public static string ViewPrefix = "~/ControlPanel/Views/{0}.cshtml";

        public static Dictionary<Type, string> PanelPartViews = new Dictionary<Type, string>()
        {
            {typeof(UserQueryDN), ViewPrefix.Formato("SearchControlPart") },
            {typeof(CountSearchControlPartDN), ViewPrefix.Formato("CountSearchControlPart") },
            {typeof(LinkListPartDN), ViewPrefix.Formato("LinkListPart") },
        };

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserQueriesClient.Start();

                Navigator.RegisterArea(typeof(ControlPanelClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<ControlPanelDN>(EntityType.Default) { PartialViewName = e => AdminViewPrefix.Formato("ControlPanelAdmin") },
                    new EmbeddedEntitySettings<PanelPart>() { PartialViewName = e => AdminViewPrefix.Formato("PanelPart") },
                    
                    new EntitySettings<CountSearchControlPartDN>(EntityType.Default) { PartialViewName = e => AdminViewPrefix.Formato("CountSearchControlPart") },
                    new EmbeddedEntitySettings<CountUserQueryElement>() { PartialViewName = e => AdminViewPrefix.Formato("CountUserQueryElement") },
                    
                    new EntitySettings<LinkListPartDN>(EntityType.Default) { PartialViewName = e => AdminViewPrefix.Formato("LinkListPart") },
                    new EmbeddedEntitySettings<LinkElement>() { PartialViewName = e => AdminViewPrefix.Formato("LinkElement") },
                });

                Constructor.ConstructorManager.Constructors.Add(
                    typeof(ControlPanelDN), () => new ControlPanelDN { Related = UserDN.Current.ToLite<IdentifiableEntity>() });
            }
        }
    }
}
