using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Mvc.Html;
using Signum.Utilities;
using System.Reflection;
using System.IO;
using System.Web;
using System.Web.Routing;
using Signum.Utilities.Reflection;
using Signum.Entities.Reflection;
using System.Web.Script.Serialization;
using System.Web.WebPages;
using Signum.Engine;
using Signum.Web.Controllers;

namespace Signum.Web
{
    public static class HtmlHelperExtenders
    {
        public static MvcHtmlString ValidationSummaryAjax(this HtmlHelper html)
        {
            return new HtmlTag("div")
                    .Id("sfGlobalValidationSummary")
                    .ToHtml();
        }

        public static MvcHtmlString ValidationSummaryAjax(this HtmlHelper html, Context context)
        {
            return new HtmlTag("div")
                .Id(context.Compose("sfGlobalValidationSummary"))
                .ToHtml();
        }

        public static MvcHtmlString HiddenAnonymous(this HtmlHelper html, object value)
        {
            return HiddenAnonymous(html, value, null);
        }

        public static MvcHtmlString HiddenAnonymous(this HtmlHelper html, object value, object htmlAttributes)
        {
            return new HtmlTag("input").Attrs(new
            {
                type = "hidden",
                value = value.ToString()
            }).Attrs(htmlAttributes).ToHtmlSelf();
        }

        public static MvcHtmlString FieldString(this HtmlHelper html, string label, string value)
        {
            var span = new HtmlTag("span").InnerHtml(MvcHtmlString.Create(value)).Class("sf-value-line").ToHtml();
            return Field(html, label, span);
        }

        public static MvcHtmlString Field(this HtmlHelper html, string label, MvcHtmlString value)
        {
            HtmlTag field = new HtmlTag("div")
                                    .Class("sf-field");

            HtmlTag labelLine = new HtmlTag("div")
                                    .Class("sf-label-line")
                                    .SetInnerText(label);

            HtmlTag valueLine = new HtmlTag("div")
                        .Class("sf-value-container")
                        .InnerHtml(value);

            field.InnerHtml(labelLine.ToHtml().Concat(valueLine.ToHtml()));

            return field.ToHtml();
        }

        public static IDisposable FieldInline(this HtmlHelper html)
        {
            return FieldInline(html, null);
        }

        public static IDisposable FieldInline(this HtmlHelper html, string fieldTitle)
        {
            TextWriter writer = html.ViewContext.Writer;

            HtmlTag div = new HtmlTag("div").Class("sf-field");

            writer.Write(div.ToHtml(TagRenderMode.StartTag));
            if (fieldTitle != null)
                writer.Write(new HtmlTag("label").Class("sf-label-line").SetInnerText(fieldTitle).ToHtml(TagRenderMode.Normal));

            HtmlTag div2 = new HtmlTag("div").Class("sf-value-container sf-value-inline");
            writer.Write(div2.ToHtml(TagRenderMode.StartTag));

            return new Disposable(() =>
            {
                writer.Write(div2.ToHtml(TagRenderMode.EndTag));
                writer.Write(div.ToHtml(TagRenderMode.EndTag));
            }); 
        }

        public static MvcHtmlString CheckBox(this HtmlHelper html, string name, bool value, bool enabled)
        {
            return CheckBox(html, name, value, enabled, null);
        }

        public static MvcHtmlString CheckBox(this HtmlHelper html, string name, bool value, bool enabled, IDictionary<string, object> htmlAttributes)
        {
            if (htmlAttributes == null)
                htmlAttributes = new Dictionary<string, object>();

            if (enabled)
                return html.CheckBox(name, value, htmlAttributes);
            else
            {
                HtmlTag checkbox = new HtmlTag("input")
                                        .Id(name)
                                        .Attrs(new
                                        {
                                            type = "checkbox",
                                            name = name,
                                            value = (value ? "true" : "false"),
                                            disabled = "disabled"
                                        })
                                        .Attrs(htmlAttributes);

                if (value)
                    checkbox.Attr("checked", "checked");

                HtmlTag hidden = new HtmlTag("input")
                        .Id(name)
                        .Attrs(new
                        {
                            type = "hidden",
                            name = name,
                            value = (value ? "true" : "false"),
                            disabled = "disabled"
                        });

                return checkbox.ToHtmlSelf().Concat(hidden.ToHtmlSelf());
            }
        }

        public static MvcHtmlString InputType(string inputType, string id, string value, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("input")
                            .Id(id)
                            .Attrs(new { type = inputType, name = id, value = value })
                            .Attrs(htmlAttributes)
                            .ToHtmlSelf();
        }

        /// <summary>
        /// Returns a "label" label that is used to show the name of a field in a form
        /// </summary>
        /// <param name="html"></param>
        /// <param name="id">The id of the label</param>
        /// <param name="innerText">The text of the label, which will be shown</param>
        /// <param name="idField">The id of the field that the label is describing</param>
        /// <param name="cssClass">The class that will be appended to the label</param>
        /// <returns>An HTML string representing a "label" label</returns>
        public static MvcHtmlString Label(this HtmlHelper html, string id, string innerText, string idField, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("label", id)
                                    .Attr("for", idField)
                                    .Attrs(htmlAttributes)
                                    .Class(cssClass)
                                    .SetInnerText(innerText)
                                    .ToHtml();
        }

        public static MvcHtmlString Label(this HtmlHelper html, string id, string innerText, string idField, string cssClass)
        {
            return html.Label(id, innerText, idField, cssClass, null);
        }

        public static MvcHtmlString Span(this HtmlHelper html, string id, string innerText, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("span", id)
                        .Attrs(htmlAttributes)
                        .Class(cssClass)
                        .SetInnerText(innerText)
                        .ToHtml();
        }

        public static MvcHtmlString Span(this HtmlHelper html, string name, string value, string cssClass)
        {
            return Span(html, name, value, cssClass, null);
        }

        public static MvcHtmlString Span(this HtmlHelper html, string name, string value)
        {
            return Span(html, name, value, null, null);
        }

        public static MvcHtmlString Href(this HtmlHelper html, string url, string text)
        {
            return new HtmlTag("a")
                        .Attr("href", url)
                        .SetInnerText(text)
                        .ToHtml();
        }

        public static MvcHtmlString Href(this HtmlHelper html, string id, string innerText, string url, string title, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            HtmlTag href = new HtmlTag("a", id)
                        .Attrs(htmlAttributes)
                        .Class(cssClass)
                        .SetInnerText(innerText);

            if (url.HasText())
                href.Attr("href", url);

            if (title.HasText())
                href.Attr("title", title);

            return href.ToHtml();
        }

        public static MvcHtmlString Div(this HtmlHelper html, string id, MvcHtmlString innerHTML, string cssClass)
        {
            return html.Div(id, innerHTML, cssClass, null);
        }

        public static MvcHtmlString Div(this HtmlHelper html, string id, MvcHtmlString innerHTML, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("div", id)
                .Attrs(htmlAttributes)
                .Class(cssClass)
                .InnerHtml(innerHTML)
                .ToHtml();
        }

        public static MvcHtmlString Button(this HtmlHelper html, string id, string value, string onclick, string cssClass)
        {
            return html.Button(id, value, onclick, cssClass, null);
        }

        public static MvcHtmlString Button(this HtmlHelper html, string id, string value, string onclick, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new HtmlTag("input", id)
                .Attrs(new { type = "button", value = value, onclick = onclick })
                .Attrs(htmlAttributes)
                .Class(cssClass)
                .ToHtmlSelf();
        }

        public static string PropertyNiceName<R>(this HtmlHelper html, Expression<Func<R>> property)
        {
            return ReflectionTools.BasePropertyInfo(property).NiceName();
        }

        public static string PropertyNiceName<T, R>(this HtmlHelper html, Expression<Func<T, R>> property)
        {
            return ReflectionTools.BasePropertyInfo(property).NiceName();
        }

        public static UrlHelper UrlHelper(this HtmlHelper html)
        {
            return new UrlHelper(html.ViewContext.RequestContext);
        }

        public static MvcHtmlString FormatHtml(this HtmlHelper html, string text, params object[] values)
        {
            return text.FormatHtml(values);
        }

        public static MvcHtmlString FormatHtml(this string text, params object[] values)
        {
            var encoded = HttpUtility.HtmlEncode(text);

            if(values == null)
                return new MvcHtmlString(encoded);

            return new MvcHtmlString(string.Format(encoded,
                values.Select(a => a is IHtmlString ? ((IHtmlString)a).ToHtmlString() : HttpUtility.HtmlEncode(a)).ToArray()));
        }

        public static MvcHtmlString Json(this HtmlHelper html, object value)
        {
            var serializer = new JavaScriptSerializer();
            serializer.RegisterConverters(new List<JavaScriptConverter>(new JavaScriptConverter[] { new LiteJavaScriptConverter() }));
            return new MvcHtmlString(serializer.Serialize(value));
        }

        public static MvcHtmlString JQueryNotification(this HtmlHelper helper, string strongText, string normalText, int? marginTop = 10)
        { 
            var pContent = new HtmlTag("span").Class("ui-icon ui-icon-info").Attr("style", "float: left; margin-right: .3em;").ToHtml();
            
            if (strongText.HasText())
                pContent = pContent.Concat(new HtmlTag("strong").SetInnerText(strongText).ToHtml());
            
            pContent = pContent.Concat(new MvcHtmlString(normalText));

            return new HtmlTag("div").Class("ui-state-highlight ui-corner-all")
                .Attr("style", "margin-top: {0}px; padding: 0 .7em; padding: 10px;".Formato(marginTop))
                .InnerHtml(new HtmlTag("p").InnerHtml(pContent).ToHtml())
                .ToHtml();
        }

        public static MvcHtmlString JQueryError(this HtmlHelper helper, string strongText, string normalText)
        {
            var pContent = new HtmlTag("span").Class("ui-icon ui-icon-alert").Attr("style", "float: left; margin-right: .3em;").ToHtml();

            if (strongText.HasText())
                pContent = pContent.Concat(new HtmlTag("strong").SetInnerText(strongText).ToHtml());

            pContent = pContent.Concat(new MvcHtmlString(normalText));

            return new HtmlTag("div").Class("ui-state-error ui-corner-all")
                .Attr("style", "margin-top: 10px; padding: 0 .7em; padding: 10px;")
                .InnerHtml(new HtmlTag("p").InnerHtml(pContent).ToHtml())
                .ToHtml();
        }
    }
}

