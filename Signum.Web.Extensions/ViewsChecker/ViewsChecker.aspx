<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Web.ViewsChecker" %>

<asp:Content ID="loginTitle" ContentPlaceHolderID="head" runat="server">
    <title>Views Checker</title>
    <link href="Content/Site.css" rel="stylesheet" type="text/css" />
    <link href="Content/LineStyles.css" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ID="loginContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>
        Views Checker</h2>
    <% 
        List<ViewError> errors = (List<ViewError>)Model;
        if (errors == null || errors.Count == 0)
            Response.Write("<b>No se econtraron errores</b>");
        else
        {
        %>
     <div>
        <table border="solid 1px black" cellpadding="5px" cellspacing="1">
            <tr valign="top" style="font-weight:bold">
                <td>
                    <%= ViewError.ViewNameLbl%>
                </td>
                <td>
                    <%= ViewError.MessageLbl%>
                </td>
                <td>
                    <%= ViewError.SourceLbl%>
                </td>
                <td>
                    <%= ViewError.StackTraceLbl%>
                </td>
                <td>
                    <%= ViewError.TargetSiteLbl%>
                </td>
            </tr>
            <%
                foreach (ViewError error in errors)
                { 
                    %>
                    <tr valign="top">
                        <td><%= error.ViewName%></td>
                        <td><%= error.Message%></td>
                        <td><%= error.Source%></td>
                        <td><%= error.StackTrace%></td>
                        <td><%= error.TargetSite%></td>
                    </tr>
                    <%
                }
             %>
        </table>
    </div>
    <% } %>
</asp:Content>

