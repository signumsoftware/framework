<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ID="loginTitle" ContentPlaceHolderID="head" runat="server">
    <title>Log On</title>
    <link href="Content/Site.css" rel="stylesheet" type="text/css" />
    <link href="Content/LineStyles.css" rel="stylesheet" type="text/css" />
</asp:Content>

<asp:Content ID="loginContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>
        Login</h2>
    <p>
        Introduzca su nombre de usuario y contraseña.
    </p>
    <%= Html.ValidationSummary() %>
    <% using (Html.BeginForm())
       {
     %>
    <div>
        <table>
            <tr>
                <td>
                    Nombre de usuario:
                </td>
                <td>
                    <%= Html.TextBox("username") %>
                    <%= Html.ValidationMessage("username") %>
                </td>
            </tr>
            <tr>
                <td>
                    Contraseña:
                </td>
                <td>
                    <%= Html.Password("password") %>
                    <%= Html.ValidationMessage("password") %>
                </td>
            </tr>
            <tr>
                <td>
                </td>
                <td>
                    &nbsp
                    <%if (Request.Browser.Cookies)
                      {
                    %>
                    <%= Html.CheckBox("rememberMe") %> Recordarme
                    <%
                        } 
                    %>                  
                </td>
            </tr>
            <tr>
                <td>
                    &nbsp
                </td>
                <td>
                    <input type="submit" value="Login" />
                </td>
            </tr>
        </table>
    </div>
    <% } %>
</asp:Content>
