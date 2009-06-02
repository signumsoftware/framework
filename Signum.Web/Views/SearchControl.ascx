<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>

<%=Html.Hidden("sfQueryName", ((FindOptions)ViewData[ViewDataKeys.FindOptions]).QueryName.ToString())%>

<%Html.RenderPartial("~/Plugin/Signum.Web.dll/Signum.Web.Views.FilterBuilder.ascx", ViewData); %>

<div id="divResults" name="divResults">

</div>