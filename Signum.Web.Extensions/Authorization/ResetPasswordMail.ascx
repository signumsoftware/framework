<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Ucalenda.Entities" %>
<%@ Import Namespace="Ucalenda.Web.Properties" %>

Your Password have been reseted.
<p />
Your new password is <%=(string)ViewData["password"] %>