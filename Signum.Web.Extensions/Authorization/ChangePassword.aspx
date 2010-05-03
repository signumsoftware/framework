<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<%@ Import Namespace="Signum.Web.Authorization" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<asp:Content ID="changePasswordContent" ContentPlaceHolderID="MainContent" runat="server">
     <h2><%=Html.Encode(Resources.ChangePasswordAspx_ChangePassword) %></h2>
     <p><%=Html.Encode(Resources.ChangePasswordAspx_WriteActualPasswordAndNewOne)%></p>
     <%= Html.ValidationSummary() %>

    <% using (Html.BeginForm()) { %>
        <div id="changePassword">
            <table>
                <tr>
                    <td class="label"><%=Html.Encode(Resources.ChangePasswordAspx_ActualPassword)%>:</td>
                    <td>
                        <%= Html.Password(UserMapping.OldPasswordKey) %>
                        <%= Html.ValidationMessage(UserMapping.OldPasswordKey)%>
                    </td>
                </tr>
                <tr>
                    <td class="label"><%=Html.Encode(Resources.ChangePasswordAspx_NewPassword)%>:</td>
                    <td>
                        <%= Html.Password(UserMapping.NewPasswordKey)%>
                        <%= Html.ValidationMessage(UserMapping.NewPasswordKey)%>
                    </td>
                </tr>
                <tr>
                    <td class="label"><%=Html.Encode(Resources.ChangePasswordAspx_ConfirmNewPassword)%>:</td>
                    <td>
                        <%= Html.Password(UserMapping.NewPasswordBisKey)%>
                        <%= Html.ValidationMessage(UserMapping.NewPasswordBisKey)%>
                    </td>
                </tr>
                <tr>
                    <td></td>
                    <td><input type="submit" value="<%=Html.Encode(Resources.ChangePasswordAspx_ChangePassword) %>" /></td>
                </tr>
            </table>
        </div>
    <% } %>
</asp:Content>
