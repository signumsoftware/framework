<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript"> $(function() { $("#<%=UserMapping.NewPasswordKey %>").focus(); }); </script>

    <div id="reset-password-container">    
        <h2>New password</h2>
        <p>Please, enter your chosen new password.</p>
        
        <%= Html.ValidationSummary() %>
        <% using (Html.BeginForm()) { %>
            <%= Html.Hidden("rpr", ViewData["rpr"].ToString())%>
        <div id="changePassword">
            <table>
                <tr>
                    <td class="label"><label for="<%=UserMapping.NewPasswordKey %>">New password</label>:</td>
                    <td>
                        <%= Html.Password(UserMapping.NewPasswordKey)%>
                        <%= Html.ValidationMessage(UserMapping.NewPasswordKey)%>
                    </td>
                </tr>
                <tr>
                    <td class="label"><label for="<%=UserMapping.NewPasswordBisKey %>">Confirm new password</label>:</td>
                    <td>
                        <%= Html.Password(UserMapping.NewPasswordBisKey)%>
                        <%= Html.ValidationMessage(UserMapping.NewPasswordBisKey)%>
                    </td>
                </tr>
                <tr>
                    <td></td>
                    <td><input type="submit" value="Change password" /></td>
                </tr>
            </table>
        </div>
        <% } %>
    </div>
</asp:Content>