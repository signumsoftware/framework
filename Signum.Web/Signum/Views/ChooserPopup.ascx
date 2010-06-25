<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>

<% Context context = (Context)Model; %>

<div id="<%=context.Compose("externalPopupDiv")%>">
<div id="<%=context.Compose("modalBackground")%>" class="transparent popupBackground"></div>
    
<div id="<%=context.Compose("panelPopup")%>" class="popupWindow">
    <%if (ViewData[ViewDataKeys.OnCancel] != null){ %>
        <div class="closebox" id="<%=context.Compose(ViewDataKeys.BtnCancel)%>" onclick="<%=ViewData[ViewDataKeys.OnCancel]%>"></div>
    <%} else { %>
        <div class="closebox" id="<%=context.Compose(ViewDataKeys.BtnCancel)%>"></div>
    <%} %>
    <div id="<%=context.Compose("divPopupDragHandle")%>" class="dragHandle" onmousedown="comienzoMovimiento(event, '<%=context.Compose("panelPopup")%>');">
        <% string pageTitle = (string)ViewData[ViewDataKeys.PageTitle];
           if (pageTitle != null) { %> <span class="popupEntityName"><%= Html.Encode(pageTitle)%></span> <%}%>           
    </div>
    <%= ViewData[ViewDataKeys.CustomHtml].ToString() %>
</div>
</div>

