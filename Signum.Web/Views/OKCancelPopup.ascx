<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>


<div id="<%=Html.GlobalName("externalPopupDiv")%>">
<div id="<%=Html.GlobalName("modalBackground")%>" class="transparent"
    style="display: block; position: fixed; left: 0px; top: 0px; z-index: 10000; width: 1280px;
    height: 871px;" ></div>
    
<div id="<%=Html.GlobalName("panelPopup")%>" style="width:auto; background-color: Black; color: white; position: absolute; z-index: 10001;">
    <div id="<%=Html.GlobalName("divPopupDragHandle")%>" onmousedown="comienzoMovimiento(event, '<%=Html.GlobalName("panelPopup")%>');">
        <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk)%>" value="OK" />
        <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel)%>" value="Cancel" />
    </div>
    
    <%= ViewData[ViewDataKeys.CustomHtml].ToString() %>
    <div id="<%=Html.GlobalName("divASustituir")%>"></div>
</div>
</div>

