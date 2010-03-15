<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>
<%@ Import Namespace="System.Web.Mvc" %>

<asp:Content ID="rememberPasswordContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Recordar contraseña</h2>
    <p>Introduzca su nombre de usuario y dirección de correo electrónico para recibir un mensaje en su cuenta con la contraseña</p>
    
    <%= Html.ValidationSummary() %>
    <% using (Html.BeginForm()) { %>
        <div>
            <table>
                <tr>
                    <td>Usuario:</td>
                    <td>
                        <%= Html.TextBox("username") %>
                        <%= Html.ValidationMessage("user") %>
                    </td>
                </tr>
                <tr>
                    <td>Email:</td>
                    <td>
                        <%= Html.TextBox("email")%>
                        <%= Html.ValidationMessage("email")%>
                    </td>
                </tr>
                <tr>
                    <td></td>
                    <td><input type="submit" value="Recordar contraseña" /></td>
                </tr>
            </table>
        </div>
    <% } %>
</asp:Content>
