<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities.Authorization" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<asp:Content ID="loginTitle" ContentPlaceHolderID="head" runat="server">
    <title>Log On</title>
</asp:Content>

<asp:Content ID="loginContent" ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript">
        $(function(){$("#username").focus();});
    </script>
    <h2>
        Login</h2>
    <p>
        <%= Resources.IntroduceYourUserNameAndPassword %>
    </p>
    <%= Html.ValidationSummary() %>
    <% 
        using (Html.BeginForm())
       {
     %>
    <div>
        <table>
            <tr>
                <td>
                    <%= Resources.User %>:
                </td>
                <td>
                    <%= Html.TextBox("username") %>
                    <%= Html.ValidationMessage("username") %>
                </td>
            </tr>
            <tr>
                <td>
                    <%= Resources.Password %>:
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
                    <%if (Request.Browser.Cookies)
                      {
                    %>
                    <%= Html.CheckBox("rememberMe") %> <%= Resources.RememberMe %>
                    <%
                        } 
                    %>                  
                </td>
            </tr>
            <% if (Navigator.TypesToNames.ContainsKey(typeof(ResetPasswordRequestDN))) { %>
            <tr>
                <td>
                </td>
                <td>
                <div id="login-others"><%: Html.ActionLink(Resources.IHaveForgottenMyPassword, "resetPassword", "auth") %></div>
                </td>
            </tr>
            <%} %>
            <tr>
                <td>
                </td>
                <td>
                    <input class="login" type="submit" value="<%= Resources.LoginEnter %>" />
                </td>
            </tr>
        </table>
        <%
           if (ViewData.ContainsKey("referrer")) { %> 
               <%=Html.Hidden("referrer", ViewData["referrer"])%> 
        <% } %>
    </div>
    <% } %>
</asp:Content>
