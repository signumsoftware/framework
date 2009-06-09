<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>

<%string sufix = (string)ViewData[ViewDataKeys.PopupSufix]; %>
<div id="<%=Html.GlobalName("externalPopupDiv" + sufix)%>">
<div id="<%=Html.GlobalName("modalBackground" + sufix)%>" class="transparent"
    style="display: block; position: fixed; left: 0px; top: 0px; z-index: 10000; width: 1280px;
    height: 871px;" ></div>
  
<div id="<%=Html.GlobalName("panelPopup" + sufix)%>" style="width:auto; background-color: Black; color: white; position: absolute; z-index: 10001;">
    <div id="<%=Html.GlobalName("divPopupDragHandle" + sufix)%>" onmousedown="comienzoMovimiento(event, '<%=Html.GlobalName("panelPopup" + sufix)%>');">
        <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk + sufix)%>" value="OK" />
        <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel + sufix)%>" value="Cancel" />
    </div>
    
    <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
    <div id="<%=Html.GlobalName("divASustituir" + sufix)%>"></div>
</div>
</div>

