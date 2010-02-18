<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>

<asp:Content ID="loginTitle" ContentPlaceHolderID="head" runat="server">
    <title>Log On</title>
</asp:Content>

<asp:Content ID="loginContent" ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript">
        $(function(){$("#username").focus();});
        function sending()
        {
            $("#login").val("Enviando...");
        }
    </script>
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
                    Usuario:
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
                    <input id="login" type="submit" value="Entrar" onclick="sending();" />
                </td>
            </tr>
        </table>
    </div>
    <% } %>
</asp:Content>
