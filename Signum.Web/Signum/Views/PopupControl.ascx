<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%
    TypeContext modelTC = (TypeContext)ViewData.Model;
%>
<div id="<%=modelTC.Compose("externalPopupDiv")%>">
<div class="transparent popupBackground"></div>
  
<div id="<%=modelTC.Compose("panelPopup")%>" class="popupWindow">
    <%if (ViewData[ViewDataKeys.OnCancel] != null){ %>
        <div class="closebox" id="<%=modelTC.Compose(ViewDataKeys.BtnCancel)%>" onclick="<%=ViewData[ViewDataKeys.OnCancel]%>"></div>
    <%} else { %>
        <div class="closebox" id="<%=modelTC.Compose(ViewDataKeys.BtnCancel)%>"></div>
    <%} %>
    <div id="<%=modelTC.Compose("divPopupDragHandle")%>" class="dragHandle">
        <%
            string pageTitle = (string)ViewData[ViewDataKeys.PageTitle];
            if (pageTitle != null)
          { 
                %>
        <span class="popupEntityName"><%= pageTitle%></span>
        <%}
          else
          { %>
        <span class="popupEntityName"><%= modelTC.UntypedValue.GetType().NiceName()%></span> <span class="popupTitle"><%= modelTC.UntypedValue.TryToString() %></span>
        <%} %>
    </div>
    <ul class="operations">
        <%if (Model != null && Navigator.Manager.ShowOkSave(modelTC.UntypedValue.GetType(), false)){ %>
            <% if(ViewData[ViewDataKeys.OnOk]!=null) { %>
            <li><input type="button" id="<%=modelTC.Compose(ViewDataKeys.BtnOk)%>" value="OK" onclick="<%=ViewData[ViewDataKeys.OnOk]%>" /></li>
        <%} else{ %>
            <li><input type="button" id="<%=modelTC.Compose(ViewDataKeys.BtnOk)%>" value="OK" /></li>
         <%} %>                
        <%} %>
        <%= ButtonBarEntityHelper.GetForEntity(this.ViewContext, (ModifiableEntity)modelTC.UntypedValue, ViewData[ViewDataKeys.PartialViewName].ToString(), modelTC.ControlID).ToString(Html)%>
    </ul>
    <%= Html.ValidationSummaryAjax(modelTC) %>
    <% Html.WritePopupHeader(); %>
    <div id="<%=modelTC.Compose("divMainControl")%>" class="divMainControl">
        <%
            Html.RenderPartial(ViewData[ViewDataKeys.PartialViewName].ToString(), Model); 
        %>
    </div>
</div>
</div>

<script>
    SF.loadJs("<%= ModuleResources.ResourceForModule("draganddrop") %>", function () {
        SF.DragAndDrop(document.getElementById("<%=modelTC.Compose("divPopupDragHandle")%>"),
                    document.getElementById("<%=modelTC.Compose("panelPopup")%>"));
    });
</script>