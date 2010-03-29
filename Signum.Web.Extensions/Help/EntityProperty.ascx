<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<%@ Import Namespace="System.Collections.Generic" %>
 
<% 
       Node<KeyValuePair<string, PropertyHelp>> ep = (Node<KeyValuePair<string, PropertyHelp>>)Model;
       KeyValuePair<string, PropertyHelp> k = ep.Value;
       Response.Write(
        "<span class='shortcut'>[p:" + ViewData["EntityName"] + "." + k.Key + "]</span>"
        
        + "<dt>" + k.Value.PropertyInfo.NiceName() + "</dt>"
           + "<dd>" + k.Value.Info.WikiParse(HelpClient.DefaultWikiSettings)
           + Html.TextArea("p-" + k.Key.Replace("/", "_"), k.Value.UserDescription, new { @class = "editable" })
           + "<span class=\"editor\" id=\"p-" + k.Key.Replace("/", "_") + "-editor\">" + k.Value.UserDescription.WikiParse(HelpClient.DefaultWikiSettings) + "</span>"
           + "</dd>");

       if (ep.Children.Count > 0)
       {
           Response.Write("<dl class=\"embedded\">");
           foreach (var v in ep.Children)
               Html.RenderPartial(HelpClient.ViewPrefix + HelpClient.ViewEntityPropertyUrl, v);
           Response.Write("</dl>");
       }
%>    