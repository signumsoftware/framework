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

namespace Signum.Web.Mailing
{
    public static class EmailClient
    {
        public static string ViewPrefix = "email/Views/";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                //EmailLogic.BodyRenderer += (vn, model, args) => RenderControl(vn, model, args);
                EmailLogic.BodyRenderer = (vn, args) => RenderView(vn, args);
                
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(EmailClient), "/email/", "Signum.Web.Extensions.Mailing."));
                
                RouteTable.Routes.InsertRouteAt0("email/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "email" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<EmailMessageDN>(EntityType.Default){ PartialViewName = e => ViewPrefix + "EmailMessage"},
                    new EntitySettings<EmailPackageDN>(EntityType.Default){ PartialViewName = e => ViewPrefix + "EmailPackage"},                    
               });

            }
        }


        public static string RenderView(string templateAbsoluteUrl, IDictionary<string, string> args)
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            wc.Headers["Method"] = "Post";
            wc.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            byte[] postData = null;
            if (args != null && args.Count > 0)
            {
                postData = System.Text.Encoding.UTF8.GetBytes(args.ToString(kvp => "{0}={1}".Formato(kvp.Key, HttpUtility.UrlEncode(kvp.Value)), "&"));

                //if (!templateAbsoluteUrl.Contains('?'))
                //    foreach (var kvp in args)
                //    {
                //        wc.QueryString[kvp.Key] = kvp.Value;
                //    }
                //else
                //    templateAbsoluteUrl = "{0}&{1}".Formato(templateAbsoluteUrl, args.ToString(kvp =>
                //        "{0}={1}".Formato(kvp.Key, kvp.Value), "&"));
            }
            byte[] requestedHTML = wc.UploadData(templateAbsoluteUrl, postData);

            //byte[] requestedHTML = wc.DownloadData(templateAbsoluteUrl);
            return encoding.GetString(requestedHTML);
        }

        private static UTF8Encoding encoding = new UTF8Encoding();

    }
}
