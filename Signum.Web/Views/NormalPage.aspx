<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Signum.Web.Properties" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Popup.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Lines.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_ViewNavigator.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_FindNavigator.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Validator.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Operations.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_DragAndDrop.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Autocomplete.js")%>" type="text/javascript"></script>
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
