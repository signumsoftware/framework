<%@ Page Title="Remember password" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript"> $(function(){$("#username").focus();}); </script>
    <div id="remember-login-form">
    <h2><%: Resources.RememberPassword %></h2>
    <p><%: Resources.RememberPasswordExplanation %></p>

    <%= Html.ValidationSummary() %>
    <% using (Html.BeginForm()) { %>
    <table id="remember">
            <tr>
                <td>
                    <label for="username"><%: Signum.Entities.Extensions.Properties.Resources.UserDN_UserName %></label>:
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
                    <%= Html.TextBox("email", "", new { size = 30 })%>
                    <%= Html.ValidationMessage("email") %>
                </td>
            </tr>
            <tr>
            <td colspan="2" class="submit-container">
                <input type="submit" value="<%: Resources.Remember %>" />
            </td>
            </tr>
        </table>            
    </div>
    <% } %>
</asp:Content>