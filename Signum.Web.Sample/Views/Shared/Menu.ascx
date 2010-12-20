<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Test" %>

    <%=new WebMenuItem
    {
        Children =
        {
            new WebMenuItem { 
                Text="Items", 
                Children = 
                {
                }
            }
        }
    }.ToString((string)ViewData["current"],"")
     %> 