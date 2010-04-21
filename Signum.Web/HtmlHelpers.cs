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
            return "<div id='sfGlobalValidationSummary'></div>";
        }

        public static string ValidationSummaryAjax(this HtmlHelper html, Context context)
        {
            return "<div id='{0}'></div>".Formato(context.Compose("sfGlobalValidationSummary"));
        }

        public static void Field(this HtmlHelper html, string label, string value)
        {
            html.Write("<div class=\"field\"><span class=\"labelLine\">{0}</span><span class=\"valueLine\">{1}</span></div><div class=\"clearall\"></div>".Formato(label, value));
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
                return html.CheckBox(name, value, htmlAttributes);
            else 
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<input type='checkbox' id='{0}' name='{0}' value='{1}' disabled='disabled'{2}{3} />".Formato(
                    name, 
                    value ? "true" : "false", 
                    htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " "), 
                    value ? "checked='checked'" : ""));

                sb.AppendLine("<input type='hidden' id='{0}' name='{0}' value='{1}' disabled='disabled' />".Formato(
                    name,
                    value ? "true" : "false"
                    ));
                return sb.ToString();
            }
        }

        /// <summary>
        /// Returns a "label" label that is used to show the name of a field in a form
        /// </summary>
        /// <param name="html"></param>
        /// <param name="id">The id of the label</param>
        /// <param name="value">The text of the label, which will be shown</param>
        /// <param name="idField">The id of the field that the label is describing</param>
        /// <param name="cssClass">The class that will be appended to the label</param>
        /// <returns>An HTML string representing a "label" label</returns>
        public static string Label(this HtmlHelper html, string id, string value, string idField, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            if (htmlAttributes == null)
                htmlAttributes = new Dictionary<string, object>();

            if (htmlAttributes.ContainsKey("class"))
                htmlAttributes["class"] += " " + cssClass;
            else
                htmlAttributes["class"] = cssClass;

            return
            String.IsNullOrEmpty(id) ?
                String.Format("<label for='{0}' {1}>{2}</label>", idField, htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " "), value) :
                String.Format("<label for='{0}' id='{1}' {2}>{3}</label>", idField, id, htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " "), value);
        }

        public static string Label(this HtmlHelper html, string id, string value, string idField, string cssClass)
        {
            return html.Label(id, value, idField, cssClass, null);
        }

        public static string Span(this HtmlHelper html, string id, string value, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            string idname = id.HasText() ? (" id='" + id + "'") : "";
            string attributes = htmlAttributes != null ? (" " + htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ")) : "";
            string css = cssClass.HasText() ? " class='" + cssClass + "'" : "";
            return "<span{0}{1}{2}>{3}</span>".Formato(idname, attributes, css, value);
        }

        public static string Span(this HtmlHelper html, string name, string value, string cssClass)
        {
            return Span(html, name, value, cssClass, null);
        }

        public static string Span(this HtmlHelper html, string name, string value)
        {
            return Span(html, name, value, null, null);
        }

        public static string Href(this HtmlHelper html, string name, string text, string href, string title, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            string idname = name.HasText() ? (" id='" + name + "' name='" + name + "'") : "";
            string attributes = htmlAttributes != null ? (" " + htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ")) : "";
            string css = cssClass.HasText() ? " class='" + cssClass + "'" : "";
            string tooltip = " title='" + (title.HasText() ? title : text) + "' ";
            return "<a{0}{1}{2}{3} href=\"{4}\">{5}</a>".Formato(idname,css,tooltip,attributes,href,text);
        }

        public static string Div(this HtmlHelper html, string name, string innerHTML, string cssClass)
        {
            return html.Div(name, innerHTML, cssClass, null);
        }

        public static string Div(this HtmlHelper html, string name, string innerHTML, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            string idname = name.HasText() ? (" id='" + name + "' name='" + name + "'") : "";
            string attributes = htmlAttributes != null ? (" " + htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ")) : "";
            string css = cssClass.HasText() ? " class='" + cssClass + "'" : "";
            return "<div{0}{1}{2}>{3}</div>".Formato(idname, css, attributes, innerHTML);
        }

        public static string Button(this HtmlHelper html, string name, string value, string onclick, string cssClass, IDictionary<string, object> htmlAttributes)
        {
            string idname = name.HasText() ? (" id='" + name + "' name='" + name + "'") : "";
            string attributes = htmlAttributes != null ? (" " + htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ")) : "";
            string css = cssClass.HasText() ? " class='" + cssClass + "'" : "";
            return "<input type='button'{0}{1} value='{2}'{3} onclick=\"{4}\" />".Formato(idname, css, value, attributes, onclick);
        }

        public static string ImageButton(this HtmlHelper html, string name, string imgSrc, string altText, string onclick, IDictionary<string, object> htmlAttributes)
        {
            string attributes = htmlAttributes != null ? (" " + htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ")) : "";
            return "<img id='{0}' src='{1}' alt='{2}' title='{2}' onclick=\"{3}\"{4} />".Formato(name, imgSrc, altText, onclick, attributes);
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
            string cadena=String.Format("<div class='message{0}' {3}><p class='title'>{1}</p><p class='content'>{2}</p></div>",
                    Enum.GetName(typeof(MessageType),type),
                    title,
                    content,
                    ToAttributeList(attributeList));
            html.ViewContext.HttpContext.Response.Write(cadena);
        }
        #endregion

        private static string ToAttributeList(object values) {
            StringBuilder sb = new StringBuilder(); 
            foreach (System.ComponentModel.PropertyDescriptor descriptor in System.ComponentModel.TypeDescriptor.GetProperties(values))
            {
                object obj2 = descriptor.GetValue(values);
                sb.Append("{0}=\"{1}\" ".Formato(descriptor.Name, obj2.ToString()));
            }
            return sb.ToString();
        }

        public static string AutoCompleteExtender(this HtmlHelper html, string ddlName, string entityTypeName, string implementations, string entityIdFieldName,
                                                  string controllerUrl, string onSuccess)
        {                   
            return  @"<script type='text/javascript'>
                        new Autocompleter('{0}', '{1}', {{
	                        entityIdFieldName: '{2}',
	                        extraParams: {{typeName: '{3}', implementations : '{4}'}}}});
                     </script>"
                    .Formato(ddlName, controllerUrl, entityIdFieldName, entityTypeName, implementations); 
        }

        public static string CssDynamic(this HtmlHelper html, string url) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine("var link=document.createElement('link');");
            sb.AppendLine("link.setAttribute('rel', 'stylesheet');");
            sb.AppendLine("link.setAttribute('type', 'text/css');");
            sb.AppendFormat("link.setAttribute('href', '{0}');", url);
            sb.AppendLine();
            sb.AppendLine("var head = document.getElementsByTagName('head')[0];");
            sb.AppendLine("head.appendChild(link);");
            sb.AppendLine("</script>");
            return sb.ToString();
        }

        public static string GetScriptRegistrationCode(string url, bool includeScriptTags){
            StringBuilder sb = new StringBuilder();
            if (includeScriptTags)
                sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine("var script=document.createElement('script');");
            sb.AppendLine("script.setAttribute('type', 'text/javascript');");
            sb.AppendFormat("script.setAttribute('src', '{0}');", url);
            sb.AppendLine();
            sb.AppendLine("var head = document.getElementsByTagName('head')[0];");
            sb.AppendLine("head.appendChild(script);");
            if (includeScriptTags)
                sb.AppendLine("</script>");
            return sb.ToString();
        }

        public static void RegisterScript(this HtmlHelper html, Type type, string fullName) {
            string url = new ScriptManager().Page.ClientScript.GetWebResourceUrl(type, fullName);
            html.ViewContext.HttpContext.Response.Write(HtmlHelperExtenders.GetScriptRegistrationCode(url, true));
        }
   }
}

