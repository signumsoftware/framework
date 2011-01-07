<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Help.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Engine" %>
<asp:Content ID="Content2" ContentPlaceHolderID="head" runat="server">
    <%: Html.ScriptCss("~/help/Content/help.css") %>
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
<%: Html.ScriptsJs("~/signum/Scripts/SF_Globals.js") %>
    <% Html.RenderPartial(HelpClient.ViewPrefix + HelpClient.Menu); %>
    <%
        AppendixHelp ah = (AppendixHelp)Model;
    
    using(var f = Html.BeginForm("SaveAppendix", "Help", new { appendix = ah.Name }, FormMethod.Post, new { id = "form-save" }))
    %>
        <div class="grid_16" id="entityName">    
            <h1><%: ah.Title%></h1> 
            <%= Html.TextArea("description", ah.Description, 5, 80, new { @class = "editable" })
                                                            + "<span class=\"editor\" id=\"description-editor\">" + ah.Description.WikiParse(HelpClient.DefaultWikiSettings).Replace("\n", "<p>") + "</span>"%>
        </div>
        <div class="clear"></div>
    <% } %>
</asp:Content>