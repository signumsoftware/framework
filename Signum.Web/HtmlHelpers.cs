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
            return "<div id='sfGlobalValidationSummary'>" + 
                   html.ValidationSummary()
                   + "&nbsp;</div>";
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
        public static String Label(this HtmlHelper html, string id, string value, string idField, string cssClass, Dictionary<string, object> htmlAttributes)
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

        public static String Label(this HtmlHelper html, string id, string value, string idField, string cssClass)
        {
            return html.Label(id, value, idField, cssClass, null);
        }

        public static string Span(this HtmlHelper html, string name, string value, string cssClass)
        { 
            return "<span " + 
                ((!string.IsNullOrEmpty(name)) ? "id=\"" + name + "\" name=\"" + name + "\" " : "") +
                "class=\"" + cssClass + "\" >" + value.Replace('_',' ') + 
                "</span>\n";
        }

        public static string Span(this HtmlHelper html, string name, string value, string cssClass, Dictionary<string, object> htmlAttributes)
        {
            return "<span " +
                ((!string.IsNullOrEmpty(name)) ? "id=\"" + name + "\" name=\"" + name + "\" " : "") +
                "class=\"" + cssClass + "\" " +
                htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ") + ">" + value +
                "</span>\n";
        }

        public static string Span(this HtmlHelper html, string name, object value, string cssClass, Type type)
        {
            string format = String.Empty;
            string strValue= String.Empty;

            if (type == typeof(Nullable<Int32>) || type == typeof(Int32)) strValue=(value !=null) ? ((int)value).ToString("N0") : String.Empty;
            if (type == typeof(Nullable<Double>) || type == typeof(Double)) strValue=String.Format("{0:N}",value);
            if (strValue == String.Empty)
            {
                strValue = (value != null) ? value.ToString() : "";
            }
            return Span(html, name, strValue, cssClass);
        }

        public static string Href(this HtmlHelper html, string name, string text, string href, string title, string cssClass, Dictionary<string, object> htmlAttributes)
        { 
            return "<a " +
                ((!string.IsNullOrEmpty(name)) ? "id=\"" + name + "\" name=\"" + name + "\" " : "") +
                "href=\"" + href + "\" " +
                "class=\"" + cssClass + "\" " +
                (htmlAttributes != null ? (htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ")) : "") + 
                ">" + text +
                "</a>\n";
        }

        public static string Div(this HtmlHelper html, string name, string innerHTML, string cssClass, Dictionary<string, object> htmlAttributes)
        {
            return "<div " +
                ((!string.IsNullOrEmpty(name)) ? "id=\"" + name + "\" name=\"" + name + "\" " : "") +
                "class=\"" + cssClass + "\" " + htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ") + ">" + innerHTML +
                "</div>\n";
        }

        public static string Button(this HtmlHelper html, string name, string value, string onclick, string cssClass, Dictionary<string, object> htmlAttributes)
        {
            return "<input type=\"button\" " +
                   "id=\"" + name + "\" " +
                   "value=\"" + value + "\" " +
                   "class=\"" + cssClass + "\" " +
                   htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ") +
                   "onclick=\"" + onclick + "\" " +
                   "/>\n";
        }

        public static string ImageButton(this HtmlHelper html, string name, string imgSrc, string altText, string onclick, Dictionary<string, object> htmlAttributes)
        {
            return "<img id='{0}' src='{1}' alt='{2}' title='{2}' onclick=\"{3}\" {4} />".Formato
                (
                    name, imgSrc, altText, onclick,
                    (htmlAttributes != null) ? htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ") : ""
                );
        }

        #region Message
        public static void Message(this HtmlHelper html, string title, string content, MessageType type){
            Message(html, title, content, type,null);
        }

        public static void Message(this HtmlHelper html, string name, string title, string content, MessageType type) {
            Message(html,title,content,type,new {id = name});
        }

        public static void Message(this HtmlHelper html, string title, string content, MessageType type, object attributeList) {
            string cadena=String.Format("<div class='message{0}' {3}><span class='title'>{1}</span><span class='content'>{2}</span></div>",
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
        public static string AutoCompleteExtender(this HtmlHelper html, string ddlName, string extendedControlName, 
                                                  string entityTypeName, string implementations, string entityIdFieldName,
                                                  string controllerUrl, int numCharacters, int numResults, int delayMiliseconds)
        {                   
            StringBuilder sb = new StringBuilder();
            sb.Append(html.Div(
                        ddlName,
                        "",
                        "AutoCompleteMainDiv",
                        new Dictionary<string, object>() 
                        { 
                            { "onclick", "AutocompleteOnClick('" + ddlName + "','" + 
                                                              extendedControlName + "','" + 
                                                              entityIdFieldName + 
                                                              "', event);" }, 
                        }));
            sb.Append("<script type=\"text/javascript\">CreateAutocomplete('" + ddlName + 
                                                              "','" + extendedControlName + 
                                                              "','" + entityTypeName + 
                                                              "','" + implementations +
                                                              "','" + entityIdFieldName + 
                                                              "','" + controllerUrl +
                                                              "'," + numCharacters +
                                                              "," + numResults +
                                                              "," + delayMiliseconds +
                                                              ");</script>\n");
            return sb.ToString();
        }

        public static string CssDynamic(this HtmlHelper html, string url) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine("var link=document.createElement('link')");
            sb.AppendLine("link.setAttribute('rel', 'stylesheet');");
            sb.AppendLine("link.setAttribute('type', 'text/css');");
            sb.AppendFormat("link.setAttribute('href', '{0}');", url);
            sb.AppendLine();
            sb.AppendLine("var head = document.getElementsByTagName('head')[0];");
            sb.AppendLine("head.appendChild(link);");
            sb.AppendLine("</script>");
            return sb.ToString();
        }
   }
}

