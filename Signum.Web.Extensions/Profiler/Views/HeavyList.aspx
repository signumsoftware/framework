<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" AutoEventWireup="true"
    Inherits="System.Web.Mvc.ViewPage" %>

<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Utilities.ExpressionTrees" %>
<%@ Import Namespace="Signum.Web.Profiler" %>
<asp:Content ID="registerContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2><%=ViewData[ViewDataKeys.PageTitle] %></h2>
    <div>
        <%  
            if (HeavyProfiler.Enabled)
            {
        %><%=Html.ActionLink("Disable", "Disable")%><%
             }
        else
        {
        %><%=Html.ActionLink("Enable", "Enable")%>
        <% } %>
    <%=Html.ActionLink("Clean", "Clean")%>

    <%=Html.ActionLink("Slowest", "HeavySlowest")%>
    </div>
    <%Html.RenderPartial(ProfileClient.ViewPath + "ProfilerTable", Model); %>
</asp:Content>
