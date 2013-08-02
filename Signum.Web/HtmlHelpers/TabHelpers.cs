using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;

namespace Signum.Web
{
    public static class TabHelpers
    {
        public static HelperResult Fieldset(this HtmlHelper helper, TypeContext tc, string id, string title, MvcHtmlString body)
        {
            var result = new HelperResult(writer =>
            {
                writer.WriteLine("<fieldset id=" + tc.Compose(id) + ">");
                writer.WriteLine("   <legend> " + HttpUtility.HtmlEncode(title) + "</legend>");
                writer.WriteLine(body.ToHtmlString());
                writer.WriteLine("</fieldset>");
            });

            var vo = tc.ViewOverrides;

            if (vo != null)
                return vo.OnSurrondFieldset(id, helper, tc, result);

            return result;
        }

        public static HelperResult Fieldset(this HtmlHelper helper, TypeContext tc, string id, string title, Func<object, HelperResult> body)
        {
            var result = new HelperResult(writer =>
            {
                writer.WriteLine("<fieldset id=" + tc.Compose(id) + ">");
                writer.WriteLine("   <legend> " + HttpUtility.HtmlEncode(title) + "</legend>");
                body(null).WriteTo(writer);
                writer.WriteLine("</fieldset>");
            });

            var vo = tc.ViewOverrides;

            if (vo != null)
                return vo.OnSurrondFieldset(id, helper, tc, result);

            return result;
        }

        public static HelperResult Fieldset(this HtmlHelper helper, TypeContext tc, string id, MvcHtmlString title, MvcHtmlString body)
        {
            var result = new HelperResult(writer =>
            {
                writer.WriteLine("<fieldset id=" + tc.Compose(id) + ">");
                writer.WriteLine("   <legend> " + title.ToHtmlString() + "</legend>");
                writer.WriteLine(body.ToHtmlString());
                writer.WriteLine("</fieldset>");
            });

            var vo = tc.ViewOverrides;

            if (vo != null)
                return vo.OnSurrondFieldset(id, helper, tc, result);

            return result;
        }

        public static HelperResult Fieldset(this HtmlHelper helper, TypeContext tc, string id, MvcHtmlString title, Func<object, HelperResult> body)
        {
            var result = new HelperResult(writer =>
            {
                writer.WriteLine("<fieldset id=" + tc.Compose(id) + ">");
                writer.WriteLine("   <legend> " + title.ToHtmlString() + "</legend>");
                body(null).WriteTo(writer);
                writer.WriteLine("</fieldset>");
            });

            var vo = tc.ViewOverrides;

            if (vo != null)
                return vo.OnSurrondFieldset(id, helper, tc, result);

            return result;
        }

        public static HelperResult Fieldset(this HtmlHelper helper, TypeContext tc, string id, Func<object, HelperResult> title, Func<object, HelperResult> body)
        {
            var result = new HelperResult(writer =>
            {
                writer.WriteLine("<fieldset id=" + tc.Compose(id) + ">");

                writer.WriteLine("   <legend> ");
                title(null).WriteTo(writer);
                writer.WriteLine("</legend>");

                body(null).WriteTo(writer);
                writer.WriteLine("</fieldset>");
            });

            var vo = tc.ViewOverrides;

            if (vo != null)
                return vo.OnSurrondFieldset(id, helper, tc, result);

            return result;
        }
    }
}