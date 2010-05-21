<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Ucalenda.Entities" %>
<%@ Import Namespace="Ucalenda.Web.Properties" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>
<%= Html.Encode(Resources.PasswordReseted) %>
<p />
<%= Html.Encode(Resources.YourNewPasswordIs0.Formato(ViewData["password"]) %>
