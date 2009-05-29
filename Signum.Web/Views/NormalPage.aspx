<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <title><%=ViewData[ViewDataKeys.PageTitle] ?? ""%></title>
    <link href="<%= System.Web.VirtualPathUtility.ToAbsolute("~/Content/LineStyles.css") %>"  rel="stylesheet" type="text/css" />
    <link href="<%= System.Web.VirtualPathUtility.ToAbsolute("~/Content/Site.css") %>"  rel="stylesheet" type="text/css" />

    <script src="../../Scripts/jquery-1.3.2.min-vsdoc.js" type="text/javascript"></script>
    <script src="../../Scripts/jquery-1.3.2-vsdoc.js" type="text/javascript"></script>
    <script src="../../Scripts/jquery-1.3.2.min.js" type="text/javascript"></script>
    <script src="../../Scripts/jquery-1.3.2.js" type="text/javascript"></script>
    <script src="../../Scripts/SF_Globals.js" type="text/javascript"></script>
    <script src="../../Scripts/SF_AjaxValidation.js" type="text/javascript"></script>
    <script src="../../Scripts/SF_PopupPanel.js" type="text/javascript"></script>
    <script src="../../Scripts/SF_DragAndDrop.js" type="text/javascript"></script>
    <script src="../../Scripts/SF_Autocomplete.js" type="text/javascript"></script>
    <script src="../../Scripts/SF_SearchEngine.js" type="text/javascript"></script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<% using(Html.BeginForm("DoPostBack","Signum","POST")){ %>
     <h2><%= ViewData[ViewDataKeys.PageTitle] ?? ""%></h2>
        <input type="button" onclick="TrySave('/Signum/TrySave')" value="Guardar" />     
        <input type="submit" value="Submit" />
        <br />
        <%= Html.ValidationSummaryAjax() %>
        <br />
        <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
        <div id="divASustituir"></div>
 <%}%>
</asp:Content>
