<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Help.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Utilities" %>

<asp:Content ID="Content2" ContentPlaceHolderID="head" runat="server">
    <link href="help/Content/help.css" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
<script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>
<div class="grid_16" id="entityContent">
<%
    int count = (int)ViewData["EntityCount"];

    List<EntityHelp> list = (List<EntityHelp>)Model;
    %>


<h1> Entidades sin descripción - <%= list.Count %> de <%= count %> (<%= Math.Round(100*list.Count / (double) count, 1) %> % )</h1>
<ul>
        <%
        foreach (EntityHelp eh in list)
        {%>
        <li><a href="<%= HelpLogic.EntityUrl(eh.Type)%>"><%=eh.Type.NiceName()%></a></li>
        <%}    
     %>
</ul>     
</div>
<div class="clear></div>
<div class="grid_16" id="entityContent">
<h1>Enlaces rotos</h1>
<table>
<%
    Dictionary<EntityHelp, HashSet<WikiParserExtensions.WikiLink>> unavailable = (Dictionary<EntityHelp, HashSet<WikiParserExtensions.WikiLink>>)ViewData["UnavailableLinks"];
    if (unavailable != null && unavailable.Count > 0)
    {
        foreach (var v in unavailable){
        %>
    <tr><td><a href="<%= HelpLogic.EntityUrl(v.Key.Type)%>"><%=v.Key.Type.NiceName()%></a></td><td><%
        foreach (var l in v.Value.Distinct(wl1=>wl1.Text)){ %>
            <%= l.ToHtmlString()%><br />
        <% } %></td></tr>
    <%
        }
        }
%>
</table>

</asp:Content>