<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Utilities" %>

<% Context context = (Context)Model; %>

<div id="<%=context.Compose("externalPopupDiv")%>">
<div id="<%=context.Compose("modalBackground")%>" class="transparent popupBackground"></div>
    
<div id="<%=context.Compose("panelPopup")%>" class="popupWindow">
    <%if (ViewData[ViewDataKeys.OnCancel] != null){ %>
        <div class="closebox" id="<%=context.Compose(ViewDataKeys.BtnCancel)%>" onclick="<%=ViewData[ViewDataKeys.OnCancel]%>"></div>
    <%} else { %>
        <div class="closebox" id="<%=context.Compose(ViewDataKeys.BtnCancel)%>"></div>
    <%} %>
    <div id="<%=context.Compose("divPopupDragHandle")%>" class="dragHandle"">
        <% string pageTitle = (string)ViewData[ViewDataKeys.PageTitle];
           if (pageTitle != null) { %> <span class="popupEntityName"><%= Html.Encode(pageTitle)%></span> <%}%>           
    </div>
    <%= ViewData[ViewDataKeys.CustomHtml].ToString() %>
</div>
</div>
<%: Html.DynamicJs("~/signum/Scripts/SF_DragAndDrop.js").Callback(@"function () {{
     SF.DragAndDrop(document.getElementById(""{0}""), document.getElementById(""{1}""));}}"
        .Formato(context.Compose("divPopupDragHandle"), context.Compose("panelPopup"))) %> 
