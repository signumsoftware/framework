<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Help.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Utilities" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
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
            <%=appendices.ToString(a => "<li><a href=\"Help/Appendix/{0}\">{1}</a></li>".Formato(a.Name, a.Title), "")%>
        </ul>
    <%}
    }%>
</div>    
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="head" runat="server">
    <link href="<%= Request.Url.GetLeftPart( UriPartial.Authority ) + VirtualPathUtility.ToAbsolute( "~/Content/help.css" ) %>"
        rel="stylesheet" type="text/css" />
</asp:Content>
