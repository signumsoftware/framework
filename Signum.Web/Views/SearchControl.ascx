<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>

<% FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions]; %>
<%=Html.Hidden(Html.GlobalName("sfQueryName"), findOptions.QueryName.ToString())%>
<%=Html.Hidden(Html.GlobalName("sfAllowMultiple"), findOptions.AllowMultiple.ToString())%>

<%Html.RenderPartial("~/Plugin/Signum.Web.dll/Signum.Web.Views.FilterBuilder.ascx", ViewData); %>
<br />
<input type="button" onclick="<%="Search('/Signum/Search','{0}');".Formato(ViewData[ViewDataKeys.PopupPrefix] ?? "") %>" value="Buscar" /> 
<br />
<div id="<%=Html.GlobalName("divResults")%>" name="<%=Html.GlobalName("divResults")%>">

</div>