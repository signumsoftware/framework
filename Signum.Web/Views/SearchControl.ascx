<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>

<% FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions]; %>
<%=Html.Hidden("sfQueryName", findOptions.QueryName.ToString())%>
<%=Html.Hidden("sfAllowMultiple", findOptions.AllowMultiple.ToString())%>

<%Html.RenderPartial("~/Plugin/Signum.Web.dll/Signum.Web.Views.FilterBuilder.ascx", ViewData); %>
<br />
<input type="button" onclick="Search('/Signum/Search')" value="Buscar" /> 
<br />
<div id="divResults" name="divResults">

</div>