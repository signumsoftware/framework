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
        <table>
            <tr>
                <td>
                    <%= ViewError.DescriptionLbl%>
                </td>
                <td>
                    <%= ViewError.CompilerErrorMsgLbl%>
                </td>
                <td>
                    <%= ViewError.SourceCodeErrorLbl%>
                </td>
                <td>
                    <%= ViewError.SourceFileLbl%>
                </td>
                <td>
                    <%= ViewError.LineLbl%>
                </td>
            </tr>
            <%
                foreach (ViewError error in errors)
                { 
                    %>
                    <tr>
                        <td><%= error.Description %></td>
                        <td><%= error.CompilerErrorMsg %></td>
                        <td><%= error.SourceCodeError %></td>
                        <td><%= error.SourceFile %></td>
                        <td><%= error.Line%></td>
                    </tr>
                    <%
                }
             %>
        </table>
    </div>
    <% } %>
</asp:Content>

