<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" AutoEventWireup="true"
    Inherits="System.Web.Mvc.ViewPage" %>

<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Utilities.ExpressionTrees" %>
<%@ Import Namespace="Signum.Web.Profiler" %>
<asp:Content ID="registerContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>
        Profiler Results</h2>
    <div>
        <%  
            if (Profiler.Enabled)
            {
        %><%=Html.ActionLink("Disable", "Disable")%><%
                                                        }
        else
        {
        %><%=Html.ActionLink("Enable", "Enable")%>
        <% } %>
    </div>
    <div>
    <%=Html.ActionLink("Clean", "Clean")%>
    </div>
    <%Html.RenderPartial(ProfileClient.ViewPath + "ProfilerTable", Model); %>
</asp:Content>
