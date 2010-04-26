<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

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
<% using(Html.BeginForm("DoPostBack","Signum","POST")){ %>
     <h2><%= ViewData[ViewDataKeys.PageTitle] ?? ""%></h2>
        <%Html.RenderPartial(ViewData[ViewDataKeys.PartialViewName].ToString()); %>
        <%= Html.ValidationSummaryAjax() %>
        <div id="divASustituir"></div>
        <div class="clear"></div>   
 <%}%>
</asp:Content>
