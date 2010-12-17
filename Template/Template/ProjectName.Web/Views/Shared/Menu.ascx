<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="$custommessage$.Entities" %>

<%= new WebMenuItem
    {
        Children =
        {
            new WebMenuItem 
            { 
                Text="Entities", 
                Children = 
                {
                    new WebMenuItem { Link = new FindOptions(typeof(MyEntityDN)) }
                },
            }
        }
    }.ToString((string)ViewData["current"],"")
%> 
