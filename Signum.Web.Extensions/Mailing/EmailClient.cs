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

namespace Signum.Web.Mailing
{
    public static class EmailClient
    {
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                //EmailLogic.BodyRenderer += (vn, model, args) => RenderControl(vn, model, args);
                EmailLogic.BodyRenderer += (vn, args) => RenderView(vn, args);
            }
        }

        //public static string RenderControl(string viewName, object model, IDictionary<string, object> args)
        //{
        //    ViewDataDictionary viewData = new ViewDataDictionary();
        //    viewData.Model = model;
        //    if (args != null)
        //        viewData.AddRange(args);

        //    ViewPage vp = new ViewPage { ViewData = viewData };
        //    Control control = vp.LoadControl(viewName);

        //    vp.Controls.Add(control);

        //    StringBuilder sb = new StringBuilder();
        //    using (StringWriter sw = new StringWriter(sb))
        //    {
        //        var fakeResponse = new HttpResponse(sw);
        //        var fakeRequest = new HttpRequest(null, "http://fakeUri.com", null);
        //        var fakeContext = new HttpContext(fakeRequest, fakeResponse);

        //        var oldContext = HttpContext.Current;
        //        HttpContext.Current = fakeContext;

        //        using (HtmlTextWriter tw = new HtmlTextWriter(sw))
        //        {
        //            vp.RenderView(new ViewContext());
        //        }

        //        HttpContext.Current = oldContext;

        //        sw.Flush();
        //    }

        //    return sb.ToString();
        //}

        public static string RenderView(string templateAbsoluteUrl, IDictionary<string, string> args)
        {
            System.Net.WebClient wc = new System.Net.WebClient();
            if (args != null && args.Count > 0)
            {
                if (!templateAbsoluteUrl.Contains('?'))
                    foreach (var kvp in args)
                    {
                        wc.QueryString[kvp.Key] = kvp.Value;
                    }
                else
                    templateAbsoluteUrl = "{0}&{1}".Formato(templateAbsoluteUrl, args.ToString(kvp =>
                        "{0}={1}".Formato(kvp.Key, kvp.Value), "&"));
            }
            byte[] requestedHTML = wc.DownloadData(templateAbsoluteUrl);
            return encoding.GetString(requestedHTML);
        }

        private static UTF8Encoding encoding = new UTF8Encoding();

    }
}
