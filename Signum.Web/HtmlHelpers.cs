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
            return "<div id=\"sfGlobalValidationSummary\" name=\"sfGlobalValidationSummary\">" + 
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
            //String.IsNullOrEmpty(id) ?
            //    String.Format("<label for='{0}' class='{1}'>{2}</label>", idField, cssClass, value) :
            //    String.Format("<label for='{0}' id='{3}' class='{1}'>{2}</label>", idField, cssClass, value, id);
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
                htmlAttributes.ToString(kv => kv.Key + "=" + kv.Value.ToString().Quote(), " ") + ">" + text +
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


        public static void Message(this HtmlHelper html, string name, string title, string content, MessageType type) {
            
               string message= String.Format("<div class='message{0}' id='{3}'><span class='title'>{1}</span><span class='content'>{2}</span></div>",
                    Enum.GetName(typeof(MessageType),type),
                    title,
                    content,
                    name
                );
            html.ViewContext.HttpContext.Response.Write(message);
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

        //public static string GetEmbeddedResource(this HtmlHelper helper, string resourceName)
        //{
        //    Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);    
        //    StreamReader reader = new StreamReader(stream);
        //    string script = reader.ReadToEnd();
        //    return script;
        //}
        //public static string GetEmbeddedResource(this HtmlHelper helper, string resourceName)
        //{
        //    string script = Assembly.GetExecutingAssembly().GetManifestResourceNames()[9];
        //    return script;
        //}
        //public static string GetEmbeddedResource(this HtmlHelper helper, string resourceName)
        //{
        //    var a = CreateResourceUrl(helper, Assembly.GetExecutingAssembly(), resourceName, "text/javascript");
        //    return a;
        //}
        //public static string GetEmbeddedResource(this HtmlHelper helper, Type caller, string resourceName)
        //{
        //    string scriptLocation = System.Web.UI.Page.ClientScript.GetWebResourceUrl( "MSDWUC_WindowStatus.js");
        //    Page.ClientScript.RegisterClientScriptInclude("MSDWUC_WindowStatus.js", scriptLocation);

        //}


        public static string CreateResourceUrl(HtmlHelper helper, Assembly assembly, string resourceName, string contentType)
        {
            return TranslateAbsolutePath(helper, string.Concat(new object[] { "~/_$res$_.axd/", EncodeToUrl(contentType), "/", EncodeToUrl(assembly.GetName().Name), '/', EncodeToUrl(resourceName) }));
        }
        internal static string EncodeToUrl(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).Replace('+', '_').Replace('/', '-').Replace('=', '$');
        }
        public static string TranslateAbsolutePath(HtmlHelper helper, string path)
        {
            if (path.StartsWith("/") || path.StartsWith("http://"))
            {
                return path;
            }
            if (!path.StartsWith("~/"))
            {
                throw new Exception("Invalid path passed to PathHelper.TranslateAbsolutePath()");
            }
            string applicationPath = helper.ViewContext.HttpContext.Request.ApplicationPath;
            if (!applicationPath.EndsWith("/"))
            {
                applicationPath = applicationPath + "/";
            }
            return (applicationPath + path.Substring(2));
        }

 

 

 

 


 

 

   }

}
