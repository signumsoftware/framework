<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" ValidateRequest="false" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Signum.Web.Properties" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.ControlPanel" %>
<%@ Import Namespace="Signum.Web.ControlPanel" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <%
        Html.IncludeAreaJs("signum/Scripts/SF_Globals.js",
            "signum/Scripts/SF_Popup.js",
            "signum/Scripts/SF_Lines.js",
            "signum/Scripts/SF_ViewNavigator.js",
            "signum/Scripts/SF_FindNavigator.js",
            "signum/Scripts/SF_Validator.js",
            "signum/Scripts/SF_Operations.js",
            "signum/Scripts/SF_DragAndDrop.js",
            "signum/Scripts/SF_Autocomplete.js");        
         %>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

<% 
    using(Html.BeginForm("DoPostBack","Signum","POST")){
    
    ControlPanelDN cp = (ControlPanelDN)Model;
    %> <h1><a href="<%= Navigator.ViewRoute(typeof(ControlPanelDN), cp.Id) %>"><%= cp.DisplayName%></a></h1>
    <%
    if (cp.Parts != null)
    {
        int rowNumber = cp.Parts.Max(p => p.Row);
    %>
    <table>
        <% for (int i = 0; i < rowNumber; i++)
           { 
                %>
                <tr>
                <%
    for (int j = 0; j < cp.NumberOfColumns; j++)
    {
        PanelPart pp = cp.Parts.SingleOrDefault(p => p.Row == i + 1 && (p.Column == j + 1 || p.Fill));
        %>
        <td style="vertical-align:top" <%= (pp != null && pp.Fill) ? ("colspan=\""+ cp.NumberOfColumns + "\"") : "" %>>
        <%
        if (pp != null)
        {
            %>
            <fieldset><legend><%= pp.Title %></legend>
            <%
            PanelPartRenderer.Render(Html, pp);
            %>
            </fieldset>
            <%
        }
        %>
        </td>
        <%
        if (pp != null && pp.Fill)
            j = cp.NumberOfColumns;
    }
                %>
                </tr>
                <%           
    }
        %>
    </table>
    <%
    }
     %>
     
    <div id="divASustituir"></div>
    <div class="clear"></div>   
 <%}%>
</asp:Content>