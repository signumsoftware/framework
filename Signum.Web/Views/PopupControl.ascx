<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%
    TypeContext modelTC = (TypeContext)ViewData.Model;
%>
<div id="<%=modelTC.Compose("externalPopupDiv")%>">
<div id="<%=modelTC.Compose("modalBackground")%>" class="transparent popupBackground"></div>
  
<div id="<%=modelTC.Compose("panelPopup")%>" class="popupWindow">
    <%if (ViewData[ViewDataKeys.OnCancel] != null){ %>
        <div class="closebox" id="<%=modelTC.Compose(ViewDataKeys.BtnCancel)%>" onclick="<%=ViewData[ViewDataKeys.OnCancel]%>"></div>
    <%} else { %>
        <div class="closebox" id="<%=modelTC.Compose(ViewDataKeys.BtnCancel)%>"></div>
    <%} %>
    <div id="<%=modelTC.Compose("divPopupDragHandle")%>" class="dragHandle" onmousedown="comienzoMovimiento(event, '<%=modelTC.Compose("panelPopup")%>');">
        <%
            string pageTitle = (string)ViewData[ViewDataKeys.PageTitle];
            if (pageTitle != null)
          { 
                %>
        <span class="popupEntityName"><%= pageTitle%></span>
        <%}
          else
          { %>
        <span class="popupEntityName"><%= modelTC.UntypedValue.GetType().NiceName()%></span><span class="popupTitle"><%= modelTC.UntypedValue.TryToString() %></span>
        <%} %>
    </div>
    <div id="<%=modelTC.Compose("divButtonBar")%>" class="buttonBar">
        <%if (Model != null && Navigator.Manager.ShowOkSave(modelTC.UntypedValue.GetType(), false)){ %>
            <% if(ViewData[ViewDataKeys.OnOk]!=null) { %>
            <input type="button" class="OperationDiv" id="<%=modelTC.Compose(ViewDataKeys.BtnOk)%>" value="OK" onclick="<%=ViewData[ViewDataKeys.OnOk]%>" />
        <%} else{ %>
            <input type="button" class="OperationDiv" id="<%=modelTC.Compose(ViewDataKeys.BtnOk)%>" value="OK" />
         <%} %>    
            
        <%} %>
        <%= ButtonBarEntityHelper.GetForEntity(this.ViewContext, (ModifiableEntity)modelTC.UntypedValue, ViewData[ViewDataKeys.MainControlUrl].ToString()).ToString(Html, modelTC.ControlID)%>
    </div>
    <div class="clearall"></div>
    <%= Html.ValidationSummaryAjax(modelTC) %>
    <% Html.WritePopupHeader(); %>
    <div class="clearall"></div>
    <div id="<%=modelTC.Compose("divMainControl")%>" class="divMainControl">
        <%
          Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); 
        %>
    </div>
</div>
</div>

