<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Help.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<%@ Import Namespace="Signum.Engine.Help" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
<script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>
    <% Html.RenderPartial(HelpClient.ViewPrefix + HelpClient.Menu); %>
    <%
        AppendixHelp ah = (AppendixHelp)Model;
     %>
    <form action="Help/Appendix/<%=ah.Name%>/Save" id="form-save">
        <div class="grid_16" id="entityName">    
            <h1><%=ah.Title%></h1> 
            <%= Html.TextArea("description", ah.Description, 5, 80, new { @class = "editable" })
                                            + "<span class=\"editor\" id=\"description-editor\">" + Html.WikiParse(ah.Description).Replace("\n", "<p>") + "</span>"%>
        </div>
        <div class="clear"></div>
    </form>
</asp:Content>