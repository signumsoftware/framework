<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%= ((HandleErrorInfo)ViewData.Model).Exception.Message %>
