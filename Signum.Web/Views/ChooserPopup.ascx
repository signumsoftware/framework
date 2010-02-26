<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>


<div id="<%=Html.GlobalName("externalPopupDiv")%>">
<div id="<%=Html.GlobalName("modalBackground")%>" class="transparent popupBackground"></div>
    
<div id="<%=Html.GlobalName("panelPopup")%>" class="popupWindow">
    <%if (ViewData[ViewDataKeys.OnCancel] != null){ %>
        <div class="closebox" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel)%>" onclick="<%=ViewData[ViewDataKeys.OnCancel]%>"></div>
    <%} else { %>
        <div class="closebox" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel)%>"></div>
    <%} %>
    <div id="<%=Html.GlobalName("divPopupDragHandle")%>" class="dragHandle" onmousedown="comienzoMovimiento(event, '<%=Html.GlobalName("panelPopup")%>');">
        &nbsp;
    </div>
    
    <%= ViewData[ViewDataKeys.CustomHtml].ToString() %>
</div>
</div>

