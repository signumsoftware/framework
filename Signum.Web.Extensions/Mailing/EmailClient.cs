#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
using Signum.Entities.Operations;
using Signum.Utilities;
using Signum.Entities;
using System.Web;
using Signum.Entities.Basics;
using System.Reflection;
using Signum.Entities.Files;
using Signum.Engine.Mailing;
using System.Web.UI;
using System.IO;
using Signum.Entities.Mailing;
using System.Web.Routing;
#endregion

namespace Signum.Web.Mailing
{
    public static class EmailClient
    {
        public static string ViewPrefix = "email/Views/";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(EmailClient), "/email/", "Signum.Web.Extensions.Mailing."));
                
                RouteTable.Routes.InsertRouteAt0("email/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "email" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<EmailMessageDN>(EntityType.Default){ PartialViewName = e => ViewPrefix + "EmailMessage"},
                    new EntitySettings<EmailPackageDN>(EntityType.Default){ PartialViewName = e => ViewPrefix + "EmailPackage"},                    
               });

                Navigator.RegisterTypeName<IEmailOwnerDN>();
            }
        }
    }
}
