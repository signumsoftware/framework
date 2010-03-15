<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<asp:Content ID="changePasswordContent" ContentPlaceHolderID="MainContent" runat="server">
     <h2>Cambiar contraseña</h2>
     <p>Introduzca su contraseña actual y la nueva contraseña</p>
     <%= Html.ValidationSummary() %>

    <% using (Html.BeginForm()) { %>
        <div id="changePassword">
            <table>
                <tr>
                    <td class="label">Contraseña actual:</td>
                    <td>
                        <%= Html.Password("currentPassword") %>
                        <%= Html.ValidationMessage("currentPassword") %>
                    </td>
                </tr>
                <tr>
                    <td class="label">Nueva contraseña:</td>
                    <td>
                        <%= Html.Password("newPassword") %>
                        <%= Html.ValidationMessage("newPassword") %>
                    </td>
                </tr>
                <tr>
                    <td class="label">Confirmar nueva contraseña:</td>
                    <td>
                        <%= Html.Password("confirmPassword") %>
                        <%= Html.ValidationMessage("confirmPassword") %>
                    </td>
                </tr>
                <tr>
                    <td></td>
                    <td><input type="submit" value="Cambiar contraseña" /></td>
                </tr>
            </table>
        </div>
    <% } %>
</asp:Content>
