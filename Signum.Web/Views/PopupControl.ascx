<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>

<%string sufix = (string)ViewData[ViewDataKeys.PopupSufix]; %>
<div id="<%=Html.GlobalName("externalPopupDiv" + sufix)%>">
    <div id="<%=Html.GlobalName("modalBackground" + sufix)%>" class="transparent popupBackground"></div>
      
    <div id="<%=Html.GlobalName("panelPopup" + sufix)%>" class="popupWindow">
        <div class="closebox" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel + sufix)%>"></div>
        <div id="<%=Html.GlobalName("divPopupDragHandle" + sufix)%>" onmousedown="comienzoMovimiento(event, '<%=Html.GlobalName("panelPopup" + sufix)%>');" class="dragHandle">
            <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk + sufix)%>" value="OK" />
           <!-- <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel + sufix)%>" value="Cancel" />-->
        </div>
        
        <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
        <div id="<%=Html.GlobalName("divASustituir" + sufix)%>"></div>
    </div>
</div>

