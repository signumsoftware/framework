<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%
    string sufix = (string)ViewData[ViewDataKeys.PopupSufix];
    string prefix = Html.GlobalPrefixedName("");
    string popupTitle = "";
    string typeNiceName = "";
    if(Model is TypeContext)
    {
        TypeContext tc = (TypeContext)Model;
        popupTitle = tc.UntypedValue.TryToString();
        typeNiceName = tc.UntypedValue.TryCC(uv => uv.GetType()).TryCC(t => t.NiceName());
    }
    else
    { 
        popupTitle = Model.ToString();
        typeNiceName = Model.GetType().NiceName();
    }
%>
<script type ="text/javascript">
    $(function(){
        $("#<%=Html.GlobalPrefixedName("panelPopup" + sufix)%>").easydrag(false)
        .setHandler("<%=Html.GlobalPrefixedName("divPopupDragHandle" + sufix)%>");
    });
</script>

<div id="<%=Html.GlobalPrefixedName("externalPopupDiv" + sufix)%>">
<div id="<%=Html.GlobalPrefixedName("modalBackground" + sufix)%>" class="transparent popupBackground"></div>
  
<div id="<%=Html.GlobalPrefixedName("panelPopup" + sufix)%>" class="popupWindow">
    <%if (ViewData[ViewDataKeys.OnCancel] != null){ %>
        <div class="closebox" id="<%=Html.GlobalPrefixedName(ViewDataKeys.BtnCancel + sufix)%>" onclick="<%=ViewData[ViewDataKeys.OnCancel]%>"></div>
    <%} else { %>
        <div class="closebox" id="<%=Html.GlobalPrefixedName(ViewDataKeys.BtnCancel + sufix)%>"></div>
    <%} %>
    <div id="<%=Html.GlobalPrefixedName("divPopupDragHandle" + sufix)%>" class="dragHandle">
        <span class="popupEntityName"><%= typeNiceName%></span><span class="popupTitle"><%= popupTitle %></span>
    </div>
    <div class="buttonBar">
        <%if (Model != null && Navigator.Manager.ShowOkSave(Model.GetType(), false)){ %>
            <% if(ViewData[ViewDataKeys.OnOk]!=null) { %>
            <input type="button" class="OperationDiv" id="<%=Html.GlobalPrefixedName(ViewDataKeys.BtnOk)%>" value="OK" onclick="<%=ViewData[ViewDataKeys.OnOk]%>" />
        <%} else{ %>
            <input type="button" class="OperationDiv" id="<%=Html.GlobalPrefixedName(ViewDataKeys.BtnOk)%>" value="OK" />
         <%} %>    
            
        <%} %>
        <%= Html.GetButtonBarElements(Model, ViewData[ViewDataKeys.MainControlUrl].ToString(), prefix) %>
    </div>
    <div class="clearall"></div>
    <%= Html.ValidationSummaryAjax(prefix) %>
    <div class="clearall"></div>
    <div id="<%=Html.GlobalPrefixedName("divMainControl" + sufix)%>" class="divMainControl">
        <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
    </div>
    <div id="<%=Html.GlobalPrefixedName("divASustituir" + sufix)%>"></div>
</div>
</div>

