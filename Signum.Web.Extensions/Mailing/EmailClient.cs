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

namespace Signum.Web.Mailing
{
    public static class EmailClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                EmailLogic.BodyRenderer += new BodyRenderer(EmailLogic_WebMailRenderer);
            }
        }

        static string EmailLogic_WebMailRenderer(string viewName, IEmailOwnerDN owner, Dictionary<string, object> args)
        {
            ViewDataDictionary viewData = new ViewDataDictionary();
            viewData.Model = owner;
            if (args != null)
                viewData.AddRange(args);

            ViewPage vp = new ViewPage { ViewData = viewData };
            Control control = vp.LoadControl(viewName);

            vp.Controls.Add(control);

            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            {
                using (HtmlTextWriter tw = new HtmlTextWriter(sw))
                {
                    vp.RenderControl(tw);
                }
            }

            return sb.ToString();
        }

    }
}
