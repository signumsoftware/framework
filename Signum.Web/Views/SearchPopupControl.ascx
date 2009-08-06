<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>

<%
    string sufix = (string)ViewData[ViewDataKeys.PopupSufix];
    string prefix = (string)ViewData[ViewDataKeys.PopupPrefix];
    Type entityType = (Type)ViewData[ViewDataKeys.EntityType];
    FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
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
        <%if (Navigator.Manager.ShowSearchOkButton(findOptions.QueryName, false))
          { %>
            <% if(ViewData[ViewDataKeys.OnOk]!=null) { %>
            <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk)%>" value="OK" onclick="<%=ViewData[ViewDataKeys.OnOk]%>" />
        <%} else{ %>
            <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk)%>" value="OK" />
         <%} %>               
        <%} %>
        <%--<%= Html.GetButtonBarElements(Model, ViewData[ViewDataKeys.MainControlUrl].ToString(), prefix) %>--%>
    </div>
    
    <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
    <div id="<%=Html.GlobalName("divASustituir" + sufix)%>"></div>
    <br />
    <%= Html.ValidationSummaryAjax(prefix) %>
</div>
</div>

