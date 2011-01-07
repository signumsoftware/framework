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

namespace Signum.Web
{
    public enum MessageType
    {
        Ok,
        Info,
        Warning,
        Error
    }

 

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

        public static void FieldString(this HtmlHelper html, string label, string value)
        {
            var span = new HtmlTag("span").InnerHtml(MvcHtmlString.Create(value)).Class("valueLine").ToHtml();
            Field(html, label, span);
        }

        public static void Field(this HtmlHelper html, string label, MvcHtmlString value)
        {
            HtmlTag field = new HtmlTag("div")
                                    .Class("field");

            HtmlTag labelLine = new HtmlTag("div")
                                    .Class("labelLine")
                                    .SetInnerText(label);

            HtmlTag valueLine = new HtmlTag("div")
                        .Class("value-container")
                        .InnerHtml(value);

            MvcHtmlString clear = HtmlHelperExtenders.GetClearDiv();

            field.InnerHtml(labelLine.ToHtml().Concat(valueLine.ToHtml()));

            html.Write(field.ToHtml());

            html.Write(clear);
        }

        public static MvcHtmlString GetClearDiv()
        {
            return new HtmlTag("div")
                    .Class("clearall")
                    .ToHtml();
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

            if (url != "#")
                href.Attr("href", url);

            if (!string.IsNullOrEmpty(title))
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

        public static void Write(this HtmlHelper html, MvcHtmlString htmlText)
        {
            html.ViewContext.HttpContext.Response.Write(htmlText.ToHtmlString());
        }

        #region Message
        public static void Message(this HtmlHelper html, string title, string content, MessageType type){
            Message(html, title, content, type,null);
        }

        public static void Message(this HtmlHelper html, string name, string title, string content, MessageType type) {
            Message(html,title,content,type,new {id = name});
        }

        public static void Message(this HtmlHelper html, string title, string content, MessageType type, object attributeList) {
            HtmlTag div = new HtmlTag("div")
                        .Class("message" + Enum.GetName(typeof(MessageType), type))
                        .Attrs(attributeList);

            HtmlTag tbTitle = new HtmlTag("p")
                        .Class(title);

            HtmlTag tbContent = new HtmlTag("p")
                        .Class(content);

            div.InnerHtml(tbTitle.ToHtml().Concat(tbContent.ToHtml()));

            html.Write(div.ToHtml());
        }
        #endregion    

        public static IHtmlString AutoCompleteExtender(this HtmlHelper html, string ddlName, Type[] types, string entityIdFieldName,
                                                  string controllerUrl, string onSuccess)
        {
            return html.DynamicJs("~/signum/Scripts/SF_autocomplete.js").Callback(@"function () {{
                            new SF.Autocompleter(""{0}"", ""{1}"", {{
	                            entityIdFieldName: ""{2}"",
	                            extraParams: {{types: ""{3}""}}}});
                        }}"
                    .Formato(ddlName, controllerUrl, entityIdFieldName, types.ToString(t => Navigator.ResolveWebTypeName(Reflector.ExtractLite(t) ?? t), ","))); 
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
   }
}

