<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

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
<% using(Html.BeginForm("DoPostBack","Signum","POST")){ %>
     <h2><%= ViewData[ViewDataKeys.PageTitle] ?? ""%></h2>
        <%Html.RenderPartial(ViewData[ViewDataKeys.PartialViewName].ToString()); %>
        <%= Html.ValidationSummaryAjax() %>
        <div id="divASustituir"></div>
        <div class="clear"></div>   
 <%}%>
</asp:Content>
