<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Captcha" %>

<%= Html.CaptchaImage(50, 180, Request.Url.GetLeftPart(UriPartial.Authority), new Dictionary<string, object> { {"style", "float:left"} }) %>
