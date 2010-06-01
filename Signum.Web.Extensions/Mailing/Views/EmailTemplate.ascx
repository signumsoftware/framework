<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Utilities" %>

<html>
<head>
<title>Signum Framework</title>
</head>
<body style="background-color: #FFF; padding: 15px; font-family: 'lucida grande',tahoma,verdana,arial,sans-serif;">
    <div>
        <%
            string viewName = (string)ViewData["viewName"];
            if (viewName.HasText())
            {
                Html.RenderPartial((string)ViewData["viewName"]); 
            }
            else
            {
                Response.Write((string)ViewData["message"]);
            }
        %>
    </div>
</body>
</html>
