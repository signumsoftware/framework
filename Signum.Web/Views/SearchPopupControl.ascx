<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%
    string sufix = (string)ViewData[ViewDataKeys.PopupSufix];
    string prefix = (string)ViewData[ViewDataKeys.PopupPrefix];
    Type entityType = (Type)ViewData[ViewDataKeys.EntityType];
    FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
    string popupTitle = (string)ViewData[ViewDataKeys.PageTitle] ?? "";
%>
<div id="<%=Html.GlobalName("externalPopupDiv" + sufix)%>">
<div id="<%=Html.GlobalName("modalBackground" + sufix)%>" class="transparent popupBackground"></div>
  
<div id="<%=Html.GlobalName("panelPopup" + sufix)%>" class="popupWindow">
    <%if (ViewData[ViewDataKeys.OnCancel] != null){ %>
        <div class="closebox" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel + sufix)%>" onclick="<%=ViewData[ViewDataKeys.OnCancel]%>"></div>
    <%} else { %>
        <div class="closebox" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel + sufix)%>"></div>
    <%} %>
    <div id="<%=Html.GlobalPrefixedName("divPopupDragHandle" + sufix)%>" onmousedown="comienzoMovimiento(event, '<%=Html.GlobalPrefixedName("panelPopup" + sufix)%>');" class="dragHandle">
        <span class="popupTitle"><%= popupTitle %></span>
    </div>    
    <div id="<%=Html.GlobalPrefixedName("divButtonBar" + sufix)%>" class="buttonBar">
        <%
            if (Navigator.Manager.ShowSearchOkButton(findOptions.QueryName, false) && findOptions.AllowMultiple != null)
          { %>
            <% 
              if(ViewData[ViewDataKeys.OnOk]!=null) { %>
            <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk + sufix)%>" value="OK" onclick="<%=ViewData[ViewDataKeys.OnOk]%>" />
        <%} else{ %>
            <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk + sufix)%>" value="OK" />
         <%} %>               
        <%} %>
        &nbsp; 
        <%--<%= Html.GetButtonBarElements(Model, ViewData[ViewDataKeys.MainControlUrl].ToString(), prefix) %>--%>
    </div>  
    <div class="clearall"></div>
    <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
    <br />
    <%= Html.ValidationSummaryAjax(prefix) %>
</div>
</div>

