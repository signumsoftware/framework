<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" AutoEventWireup="true" CodeBehind="ChangePassword.aspx.cs" Inherits="TrazadorMVC.Views.Account.ChangePassword" %>

<asp:Content ID="changePasswordContent" ContentPlaceHolderID="MainContent" runat="server">
    <h2>Cambiar Contraseña</h2>
    <p>
        Utilice el siguiente formulario para cambiar su contraseña. 
    </p>
    <p>
        Una contraseña requiere un mínimo de <%=Html.Encode(ViewData["PasswordLength"])%> caracteres de longitud.
    </p>
    <%= Html.ValidationSummary() %>

    <% using (Html.BeginForm()) { %>
        <div>
            <table>
                <tr>
                    <td>Contraseña actual:</td>
                    <td>
                        <%= Html.Password("currentPassword") %>
                        <%= Html.ValidationMessage("currentPassword") %>
                    </td>
                </tr>
                <tr>
                    <td>Nueva contraseña:</td>
                    <td>
                        <%= Html.Password("newPassword") %>
                        <%= Html.ValidationMessage("newPassword") %>
                    </td>
                </tr>
                <tr>
                    <td>Confirmar nueva contraseña:</td>
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
