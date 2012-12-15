#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Signum.Engine.Operations;
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
        public static string ViewPrefix = "~/Mailing/Views/{0}.cshtml";


        public static void Start(bool smtpConfig, bool newsletter)
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(MailingClient));
                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<EmailMessageDN>(){ PartialViewName = e => ViewPrefix.Formato("EmailMessage")},
                    new EntitySettings<EmailPackageDN>(){ PartialViewName = e => ViewPrefix.Formato("EmailPackage")},
                    new EntitySettings<EmailTemplateDN>(),
                });

                if (smtpConfig)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<SMTPConfigurationDN>() { PartialViewName = e => ViewPrefix.Formato("SMTPConfiguration") },
                });

                if (newsletter)
                    Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<NewsletterDN>() { PartialViewName = e => ViewPrefix.Formato("Newsletter") },
                    new EntitySettings<NewsletterDeliveryDN>() { PartialViewName = e => ViewPrefix.Formato("NewsletterDelivery") },
                });
            }
        }
    }
}
