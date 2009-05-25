<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>


<div id="<%=Html.GlobalName("externalPopupDiv")%>">
<div id="<%=Html.GlobalName("modalBackground")%>" class="transparent"
    style="display: block; position: fixed; left: 0px; top: 0px; z-index: 10000; width: 1280px;
    height: 871px;" ></div>
  
  <% 
      string onOk = "OnPopupOK('/Signum/TrySavePartial','" + ViewData[ViewDataKeys.PopupPrefix].ToString() + "')";
      if (ViewData.ContainsKey(ViewDataKeys.OnOk) && !string.IsNullOrEmpty(ViewData[ViewDataKeys.OnOk].ToString()))
          onOk = ViewData[ViewDataKeys.OnOk].ToString();
      string onCancel = "OnPopupCancel('" + ViewData[ViewDataKeys.PopupPrefix].ToString()  + "');";
      if (ViewData.ContainsKey(ViewDataKeys.OnCancel) && !string.IsNullOrEmpty(ViewData[ViewDataKeys.OnCancel].ToString()))
          onCancel = ViewData[ViewDataKeys.OnCancel].ToString();
  %>
  
    
<div id="<%=Html.GlobalName("panelPopup")%>" style="width:auto; background-color: Black; color: white; position: absolute; z-index: 100001;">
    <div id="<%=Html.GlobalName("divPopupDragHandle")%>" onmousedown="comienzoMovimiento(event, '<%=Html.GlobalName("panelPopup")%>');">
        <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnOk)%>" value="OK" onclick="<%= onOk %>" />
        <input type="button" id="<%=Html.GlobalName(ViewDataKeys.BtnCancel)%>" value="Cancel" onclick="<%= onCancel %>" />
    </div>
    
    <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
    <div id="<%=Html.GlobalName("divASustituir")%>"></div>
</div>
</div>

