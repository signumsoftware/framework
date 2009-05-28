<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="System.Collections.Generic" %>

<%Html.RenderPartial("~/Plugin/Signum.Web.dll/Signum.Web.Views.FilterBuilder.ascx", ViewData); %>