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
        public static string ValidationSummaryAjax(this HtmlHelper html)
        {
            return new FluentTagBuilder("div")
                    .GenerateId("sfGlobalValidationSummary")
                    .ToString(TagRenderMode.Normal);
        }

        public static string ValidationSummaryAjax(this HtmlHelper html, Context context)
        {
            return new FluentTagBuilder("div")
                .GenerateId(context.Compose("sfGlobalValidationSummary"))
                .ToString(TagRenderMode.Normal);
        }

        public static void Field(this HtmlHelper html, string label, string value)
        {
            FluentTagBuilder field = new FluentTagBuilder("div")
                                    .AddCssClass("field");

            FluentTagBuilder labelLine = new FluentTagBuilder("div")
                                    .AddCssClass("labelLine")
                                    .InnerHtml(label);

            FluentTagBuilder valueLine = new FluentTagBuilder("div")
                        .AddCssClass("valueLine")
                        .InnerHtml(value);

            string clear = HtmlHelperExtenders.GetClearDiv();

            html.Write (field
                        .InnerHtml(labelLine.ToString(TagRenderMode.Normal)
                              + valueLine.ToString(TagRenderMode.Normal)
                              + clear)
                        .ToString(TagRenderMode.Normal));
        }

        public static string GetClearDiv()
        {
            return new FluentTagBuilder("div")
                    .AddCssClass("clearll")
                    .ToString(TagRenderMode.SelfClosing);
        }

        public static string CheckBox(this HtmlHelper html, string name, bool value, bool enabled)
        {
            return CheckBox(html, name, value, enabled, null);
        }

        public static string CheckBox(this HtmlHelper html, string name, bool value, bool enabled, IDictionary<string, object> htmlAttributes)
        {
            if (htmlAttributes == null)
                htmlAttributes = new Dictionary<string, object>();

            if (enabled)
                return html.CheckBox(name, value, htmlAttributes).ToHtmlString();
            else 
            {
                FluentTagBuilder checkbox = new FluentTagBuilder("input")
                                        .GenerateId(name)
                                        .MergeAttributes(new
                                        {
                                            type = "checkbox",
                                            name = name,
                                            value = (value ? "true" : "false"),
                                            disabled = "disabled"
                                        })
                                        .MergeAttributes(htmlAttributes);

                if (value)
                    checkbox.MergeAttribute("checked", "checked");

                FluentTagBuilder hidden = new FluentTagBuilder("input")
                        .GenerateId(name)
                        .MergeAttributes(new
                        {
                            type = "hidden",
                            name = name,
                            value = (value ? "true" : "false"),
                            disabled = "disabled"
                        });

                return checkbox.ToString(TagRenderMode.SelfClosing)
                        + hidden.ToString(TagRenderMode.SelfClosing);
            }
        }

        public static string InputType(string inputType, string id, string value, IDictionary<string, object> htmlAttributes)
        {
            return new FluentTagBuilder("input")
                            .GenerateId(id)
                            .MergeAttributes(new { type = inputType, name = id, value = value })
                            .MergeAttributes(htmlAttributes)
                            .ToString(TagRenderMode.SelfClosing);
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
        public static string Label(this HtmlHelper html, string id, string innerText, string idField, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new FluentTagBuilder("label", id)
                                    .MergeAttribute("for", idField)
                                    .MergeAttributes(htmlAttributes)
                                    .AddCssClass(cssClass)
                                    .SetInnerText(innerText)
                                    .ToString(TagRenderMode.Normal);
        }

        public static string Label(this HtmlHelper html, string id, string innerText, string idField, string cssClass)
        {
            return html.Label(id, innerText, idField, cssClass, null);
        }

        public static string Span(this HtmlHelper html, string id, string innerText, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new FluentTagBuilder("span", id)
                        .MergeAttributes(htmlAttributes)
                        .AddCssClass(cssClass)
                        .SetInnerText(innerText)
                        .ToString(TagRenderMode.Normal);
        }

        public static string Span(this HtmlHelper html, string name, string value, string cssClass)
        {
            return Span(html, name, value, cssClass, null);
        }

        public static string Span(this HtmlHelper html, string name, string value)
        {
            return Span(html, name, value, null, null);
        }

        public static string Href(this HtmlHelper html, string url, string text) {
            return new FluentTagBuilder("a")
                        .MergeAttribute("href", url)
                        .SetInnerText(text)
                        .ToString(TagRenderMode.Normal);
        }

        public static string Href(this HtmlHelper html, string id, string innerText, string url, string title, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            FluentTagBuilder href = new FluentTagBuilder("a", id)
                        .MergeAttribute("href", url)
                        .MergeAttributes(htmlAttributes)
                        .AddCssClass(cssClass)
                        .SetInnerText(innerText);

            if (!string.IsNullOrEmpty(title))
                href.MergeAttribute("title", title);

            return href.ToString(TagRenderMode.Normal);
        }

        public static string Div(this HtmlHelper html, string id, string innerHTML, string cssClass)
        {
            return html.Div(id, innerHTML, cssClass, null);
        }

        public static string Div(this HtmlHelper html, string id, string innerHTML, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new FluentTagBuilder("div", id)
                .MergeAttributes(htmlAttributes)
                .AddCssClass(cssClass)
                .InnerHtml(innerHTML)
                .ToString(TagRenderMode.Normal);
        }

        public static string Button(this HtmlHelper html, string id, string value, string onclick, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            return new FluentTagBuilder("input", id)
                .MergeAttributes(new { type = "button", value = value, onclick = onclick })
                .MergeAttributes(htmlAttributes)
                .AddCssClass(cssClass)
                .ToString(TagRenderMode.SelfClosing);
        }

        public static void Write(this HtmlHelper html, string text)
        {
            html.ViewContext.HttpContext.Response.Write(text);
        }

        #region Message
        public static void Message(this HtmlHelper html, string title, string content, MessageType type){
            Message(html, title, content, type,null);
        }

        public static void Message(this HtmlHelper html, string name, string title, string content, MessageType type) {
            Message(html,title,content,type,new {id = name});
        }

        public static void Message(this HtmlHelper html, string title, string content, MessageType type, object attributeList) {
            FluentTagBuilder div = new FluentTagBuilder("div")
                        .AddCssClass("message" + Enum.GetName(typeof(MessageType), type))
                        .MergeAttributes(attributeList);

            FluentTagBuilder tbTitle = new FluentTagBuilder("p")
                        .AddCssClass(title);

            FluentTagBuilder tbContent = new FluentTagBuilder("p")
                        .AddCssClass(content);

            div.InnerHtml(tbTitle.ToString(TagRenderMode.Normal) + tbContent.ToString(TagRenderMode.Normal));
            html.ViewContext.HttpContext.Response.Write(div.ToString(TagRenderMode.Normal));
        }
        #endregion    

        public static string AutoCompleteExtender(this HtmlHelper html, string ddlName, string entityTypeName, string implementations, string entityIdFieldName,
                                                  string controllerUrl, string onSuccess)
        {                   
            return  @"<script type=""text/javascript"">
                        new Autocompleter(""{0}"", ""{1}"", {{
	                        entityIdFieldName: ""{2}"",
	                        extraParams: {{typeName: ""{3}"", implementations : ""{4}""}}}});
                     </script>"
                    .Formato(ddlName, controllerUrl, entityIdFieldName, entityTypeName, implementations); 
        }
   }
}

