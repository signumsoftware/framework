<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Web.ViewsChecker" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>
<%@ Import Namespace="Signum.Utilities" %>

<asp:Content ID="loginTitle" ContentPlaceHolderID="head" runat="server">
    <title><%= Resources.ViewsChecker %></title>
</asp:Content>

<asp:Content ID="loginContent" ContentPlaceHolderID="MainContent" runat="server">
    <h1><%= Resources.ViewsChecker %></h1>
    <% 
        List<ViewError> errors = (List<ViewError>)Model;
        if (errors == null || errors.Count == 0)
            Response.Write("<h2>" + "No errors found" + "</h2>");
        else
        {
            Response.Write("<h2>" + "There are a total of {0} errors".Formato(errors.Count) + "</h2>");
        %>
     <div>
        <table border="solid 1px black" cellpadding="5px" cellspacing="1">
            <tr valign="top" style="font-weight:bold">
                <td>
                    <%= Resources.ViewName %>
                </td>
                <td>
                    <%= Resources.Message %>
                </td>
                <td>
                    <%= Resources.Source %>
                </td>
                <td>
                    <%= Resources.StackTrace %>
                </td>
                <td>
                    <%= Resources.TargetSite %>
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

