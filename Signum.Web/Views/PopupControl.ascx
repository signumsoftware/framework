<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Utilities" %>
<%
    string sufix = (string)ViewData[ViewDataKeys.PopupSufix];
    string prefix = (string)ViewData[ViewDataKeys.PopupPrefix];
%>
<div id="<%=Html.GlobalName("externalPopupDiv" + sufix)%>">
<div id="<%=Html.GlobalName("modalBackground" + sufix)%>" class="transparent popupBackground"></div>
  
<div id="<%=Html.GlobalName("panelPopup" + sufix)%>" class="popupWindow">
    <%if (ViewData[ViewDataKeys.OnCancel] != null){ %>
        <div class="closebox" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel + sufix)%>" onclick="<%=ViewData[ViewDataKeys.OnCancel]%>"></div>
    <%} else { %>
        <div class="closebox" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel + sufix)%>"></div>
    <%} %>
    <div id="<%=Html.GlobalName("divPopupDragHandle" + sufix)%>" onmousedown="comienzoMovimiento(event, '<%=Html.GlobalName("panelPopup" + sufix)%>');" class="dragHandle">
        <span id="windowTitle"><%= (Model != null) ? (Model.ToString().HasText() ? Model.ToString() : Navigator.TypesToURLNames[Model.GetType()] ) : "Sin título"%></span>
    </div>
    <div class="buttonBar">
        <%if (Model != null && Navigator.Manager.ShowOkSave(Model.GetType(), false)){ %>
            <% if(ViewData[ViewDataKeys.OnOk]!=null) { %>
            <input type="button" class="OperationDiv" id="<%=Html.GlobalName(ViewDataKeys.BtnOk)%>" value="OK" onclick="<%=ViewData[ViewDataKeys.OnOk]%>" />
        <%} else{ %>
            <input type="button" class="OperationDiv" id="<%=Html.GlobalName(ViewDataKeys.BtnOk)%>" value="OK" />
         <%} %>    
            
        <%} %>
        <%= Html.GetButtonBarElements(Model, ViewData[ViewDataKeys.MainControlUrl].ToString(), prefix) %>
    </div>
    <div class="clearall"></div>
    <div id="<%=Html.GlobalName("divMainControl" + sufix)%>" class="divMainControl">
        <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
    </div>
    <div id="<%=Html.GlobalName("divASustituir" + sufix)%>"></div>
    <br />
    <%= Html.ValidationSummaryAjax(prefix) %>
</div>
</div>

