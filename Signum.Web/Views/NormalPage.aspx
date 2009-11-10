<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <!--<link href="System.Web.VirtualPathUtility.ToAbsolute("~/Content/Site.css")" rel="stylesheet" type="text/css" />-->
    <!--<link href="<%= System.Web.VirtualPathUtility.ToAbsolute("~/Content/LineStyles.css")%>" rel="stylesheet" type="text/css" />-->
    
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_AjaxValidation.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_DragAndDrop.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Autocomplete.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_SearchEngine.js")%>" type="text/javascript"></script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<% using(Html.BeginForm("DoPostBack","Signum","POST")){ %>
     <input type="hidden" id="<%=ViewDataKeys.TabId%>" name="<%=ViewDataKeys.TabId%>" value="<%=(string)ViewData[ViewDataKeys.TabId]%>" />

     <table>
        <tr>
            <td>
                <%--<%= Html.GetWidgetsListForViewName(Model, ViewData[ViewDataKeys.MainControlUrl].ToString(), "") %>--%>
            </td>
            <td>
<h2>
        <span class="typeNiceName"><%= ViewData[ViewDataKeys.EntityTypeNiceName]%></span>
        <span class="title"><%= ViewData[ViewDataKeys.PageTitle] ?? "" %></span>
     </h2>
     <div class="operations">
        <%if (Model != null && Navigator.Manager.ShowOkSave(Model.GetType(), false)){ %>
            <a id="btnSave" class="OperationDiv" href="javascript:TrySave('Signum/TrySave');">Guardar</a>   
        <%} %>
        <%= Html.GetButtonBarElements(Model, ViewData[ViewDataKeys.MainControlUrl].ToString(), "") %>     
     </div>
     <div class="validationSummaryAjax">
        <%= Html.ValidationSummaryAjax() %>     
     </div>    
    <div id="divMainControl" class="divMainControl">
        <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
    </div>
    <div id="divASustituir"></div>
            </td>
        </tr>
     </table>
 <%}%>
</asp:Content>
