<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server"> 
    <link href="Content/LineStyles.css" rel="stylesheet" type="text/css" />
    
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_AjaxValidation.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_DragAndDrop.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Autocomplete.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_SearchEngine.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_PopupPanel.js")%>" type="text/javascript"></script>

</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<% using(Html.BeginForm("DoPostBack","Signum","POST")){ %>
     <h2><%= ViewData[ViewDataKeys.PageTitle] ?? ""%></h2>
        <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString()); %>
        <%= Html.ValidationSummaryAjax() %>
 <%}%>
</asp:Content>
