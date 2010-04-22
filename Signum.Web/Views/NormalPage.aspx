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
    <%= Html.Hidden(ViewDataKeys.TabId, ViewData[ViewDataKeys.TabId]) %>

    <h2>
        <span class="typeNiceName"><%= modelTC.UntypedValue.GetType().NiceName() %></span>
        <span class="entityId">
           <% int? idOrNull = ((IIdentifiable)modelTC.UntypedValue).IdOrNull;
           if (idOrNull != null)
           { %>
                <span class="separator">[</span>
                <span>ID: <%= idOrNull.Value%></span> 
                <span class="separator">]</span>
            <% } %>
            </span>
        <span class="title"><%= ViewData[ViewDataKeys.PageTitle] ?? "" %></span>
     </h2>
     <div id="divButtonBar" class="operations">
        <%if (Model != null && Navigator.Manager.ShowOkSave(modelTC.UntypedValue.GetType(), false))
          { %>
            <div id="btnSave" class="ButtonDiv" onclick="javascript:TrySave({});"><%= Resources.Save %></div>  
        <%} %>
        <%= ButtonBarEntityHelper.GetForEntity(this.ViewContext, (ModifiableEntity)modelTC.UntypedValue, ViewData[ViewDataKeys.PartialViewName].ToString(), modelTC.ControlID).ToString(Html)%>     
     </div>
     <div class="clearall"></div>
     <div class="validationSummaryAjax">
        <%= Html.ValidationSummaryAjax() %> 
        <% Html.WritePageHeader(); %>    
     </div>    
    <div id="divMainControl" class="divMainControl">
        <%
            Html.RenderPartial(ViewData[ViewDataKeys.PartialViewName].ToString(), Model); 
        %>
    </div>
    <div id="divASustituir"></div>
    <div class="clear"></div>   
 <%}%>
</asp:Content>
