<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web.Authorization" %>
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
                        <%= Html.Password(UserMapping.OldPasswordKey) %>
                        <%= Html.ValidationMessage(UserMapping.OldPasswordKey)%>
                    </td>
                </tr>
                <tr>
                    <td class="label">Nueva contraseña:</td>
                    <td>
                        <%= Html.Password(UserMapping.NewPasswordKey)%>
                        <%= Html.ValidationMessage(UserMapping.NewPasswordKey)%>
                    </td>
                </tr>
                <tr>
                    <td class="label">Confirmar nueva contraseña:</td>
                    <td>
                        <%= Html.Password(UserMapping.NewPasswordBisKey)%>
                        <%= Html.ValidationMessage(UserMapping.NewPasswordBisKey)%>
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
