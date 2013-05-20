using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace Signum.Web.HtmlHelpers
{
    public static class TabHelpers
    {
        public static HelperResult Tab(this HtmlHelper helper, TypeContext tc, string tabId, Func<object, HelperResult> title, Func<object, HelperResult> body)
        {
            var vo = tc.ViewOverrides;

            return new HelperResult(writer =>
            {
                if (vo != null)
                {
                    var pre = vo.OnPreTab(tabId, helper, tc);
                    if (pre != null)
                        writer.WriteLine(pre.ToString());
                }

                writer.WriteLine("<fieldset id=" + tc.Compose(tabId) + ">");

                writer.WriteLine("   <legend> ");
                title(null).WriteTo(writer);
                writer.WriteLine("</legend>");

                body(null).WriteTo(writer);
                writer.WriteLine("</fieldset>");

                if (vo != null)
                {
                    var post = vo.OnPostTab(tabId, helper, tc);
                    if (post != null)
                        writer.WriteLine(post.ToString());
                }
            });
        }

        public static HelperResult Tab(this HtmlHelper helper, TypeContext tc, string tabId, string title, Func<object, HelperResult> html)
        {
            var vo = tc.ViewOverrides;

            return new HelperResult(writer =>
            {
                if (vo != null)
                {
                    var pre = vo.OnPreTab(tabId, helper, tc);
                    if (pre != null)
                        writer.WriteLine(pre.ToString());
                }

                writer.WriteLine("<fieldset id=" + tc.Compose(tabId) + ">");
                writer.WriteLine("   <legend> " + HttpUtility.HtmlEncode(title) + "</legend>");
                html(null).WriteTo(writer);
                writer.WriteLine("</fieldset>");

                if (vo != null)
                {
                    var post = vo.OnPostTab(tabId, helper, tc);
                    if (post != null)
                        writer.WriteLine(post.ToString());
                }
            });
        }
    }
}