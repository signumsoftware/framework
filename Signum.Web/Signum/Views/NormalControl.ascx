<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Signum.Web.Properties" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities" %>
<%
    TypeContext modelTC = (TypeContext)ViewData.Model;
%>
<h2>
    <%= Html.Hidden(ViewDataKeys.TabId, ViewData[ViewDataKeys.TabId]) %>
    <%= Html.Hidden(ViewDataKeys.PartialViewName, ViewData[ViewDataKeys.PartialViewName])%>
    
    
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
     <ul class="operations">
        <%if (Model != null && Navigator.Manager.ShowOkSave(modelTC.UntypedValue.GetType(), false))
          { %>
            <li><a id="btnSave" class="entity-operation save" onclick="javascript:TrySave({});"><%= Resources.Save %></a></li>  
        <%} %>
        <%= ButtonBarEntityHelper.GetForEntity(this.ViewContext, (ModifiableEntity)modelTC.UntypedValue, ViewData[ViewDataKeys.PartialViewName].ToString(), modelTC.ControlID).ToString(Html)%>     
     </ul>
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