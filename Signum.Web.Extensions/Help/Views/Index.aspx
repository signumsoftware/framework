<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Help.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ContentPlaceHolderID="head" runat="server">
    <%: Html.ScriptCss("~/help/Content/help.css") %>
</asp:Content>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
<div class="grid_16" id="entityContent">
    <h1>Documentación de ayuda</h1>
    <table><tr><td>    
            <%
                foreach (NamespaceModel item in ((NamespaceModel)Model).Namespaces)
                {
            %>
     
                <% Html.RenderPartial(HelpClient.ViewPrefix + HelpClient.NamespaceControlUrl, item); %>
    </td><td>
            <%
                }
            %>
        </td></tr>
    </table>
    <% 
        if (ViewData.TryGetC("appendices") != null)
        {
            List<AppendixHelp> appendices = (List<AppendixHelp>)ViewData["appendices"];
            if (appendices.Count > 0)
            { %>
                    <h2>Apéndices</h2>
        <ul>
            <% foreach(var a in appendices) { %>
                <li>
                <%: Html.ActionLink(a.Title, "ViewAppendix", new { appendix = a.Name }) %>
                </li>
            <% } %>
        </ul>
    <%} 
    }%>
</div>    
</asp:Content>