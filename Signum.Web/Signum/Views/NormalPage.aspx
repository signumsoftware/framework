<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" ValidateRequest="false" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Signum.Web.Properties" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
   <%: Html.ScriptsJs(
            "~/signum/Scripts/SF_Globals.js",
            "~/signum/Scripts/SF_Popup.js",   
            "~/signum/Scripts/SF_Lines.js",
            "~/signum/Scripts/SF_ViewNavigator.js",
            "~/signum/Scripts/SF_FindNavigator.js",
            "~/signum/Scripts/SF_Validator.js",   
            "~/signum/Scripts/SF_Operations.js")
    %> 
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">

<% TypeContext modelTC = (TypeContext)Model;
    using(Html.BeginForm("DoPostBack","Signum","POST")){ %>
    
    <div id="divNormalControl">
        <% Html.RenderPartial(Navigator.Manager.NormalControlUrl, ViewData); %>
    </div>
    
    <div id="divASustituir"></div>
    <div class="clear"></div>   
 <%}%>
</asp:Content>
