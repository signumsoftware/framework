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
    public static class MailingClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(MailingClient));

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<EmailMessageDN>(EntityType.Default){ PartialViewName = e => RouteHelper.AreaView("EmailMessage", "Mailing")},
                    new EntitySettings<EmailPackageDN>(EntityType.Default){ PartialViewName = e => RouteHelper.AreaView("EmailPackage", "Mailing")},
                    new EntitySettings<EmailTemplateDN>(EntityType.ServerOnly)
                });
            }
        }
    }
}
