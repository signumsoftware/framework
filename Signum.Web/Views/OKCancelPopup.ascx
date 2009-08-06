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
    <div id="<%=Html.GlobalName("divPopupDragHandle")%>" onmousedown="comienzoMovimiento(event, '<%=Html.GlobalName("panelPopup")%>');">
        <% if(ViewData[ViewDataKeys.OnOk]!=null) { %>
            <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk)%>" value="OK" onclick="<%=ViewData[ViewDataKeys.OnOk]%>" />
        <%} else{ %>
            <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk)%>" value="OK" />
        <%} %>    
    </div>
    
    <%= ViewData[ViewDataKeys.CustomHtml].ToString() %>
    <div id="<%=Html.GlobalName("divASustituir")%>"></div>
    <br />
    <%= Html.ValidationSummaryAjax(prefix) %>
</div>
</div>

