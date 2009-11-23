<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Files" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Files" %>

<%
    string fileType = "", idValueField = "";
    
    if (ViewData["FileType"] != null) {
        fileType = ViewData["FileType"].ToString();
    }

    if (ViewData["IdValueField"] != null)
    {
        idValueField = ViewData["IdValueField"].ToString();
    }

    Response.Write(fileType + " - " + idValueField);
    /*using (var a = Html.TypeContext<FilePathDN>())
    {
        Response.Write(Html.FileLine(idValueField, a.Value, fileType));
    }*/
%>