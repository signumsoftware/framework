<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Files" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Files" %>
<%@ Import Namespace="Signum.Engine.Basics" %>

<%
    string fileType = "", idValueField = "";
    
    if (ViewData["FileType"] != null) {
        fileType = ViewData["FileType"].ToString();
    }

    if (ViewData["IdValueField"] != null)
    {
        idValueField = ViewData["IdValueField"].ToString();
    }

    using (var a = Html.TypeContext<FilePathDN>())
    {
        Response.Write(Html.FileLine(a.Value, idValueField, new FileLine{FileType = EnumLogic<FileTypeDN>.ToEnum(fileType), Remove=false, LabelVisible=false }));
    }
%>