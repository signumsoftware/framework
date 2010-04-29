<%@ Page Title="Remember password" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript"> $(function(){$("#username").focus();}); </script>
    <div id="remember-login-form">
    <h2>Remember password</h2>
    <p>Type your user name and your e-mail address to receive a message with a new password</p>

    <%= Html.ValidationSummary() %>
    <% using (Html.BeginForm()) { %>
    <table id="remember">
            <tr>
                <td>
                    <label for="username">User name</label>:
                </td>
                <td>
                    <%= Html.TextBox("username", "", new { size = 30 })%>
                    <%= Html.ValidationMessage("username") %>
                </td>
            </tr>
            <tr>
                <td>
                    <label for="email">Email</label>:
                </td>
                <td>
                    <%= Html.Password("email", "", new { size = 30 }) %>
                    <%= Html.ValidationMessage("email") %>
                </td>
            </tr>
            <tr>
            <td colspan="2" class="submit-container">
                <input type="submit" value="Remember" />
            </td>
            </tr>
        </table>            
    </div>
    <% } %>
</asp:Content>