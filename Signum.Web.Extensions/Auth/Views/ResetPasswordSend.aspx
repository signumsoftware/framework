<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Extensions.Properties" %>

<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <script type="text/javascript"> $(function() { $("#email").focus(); }); </script>

    <div id="reset-password-container">    
        <h2><%= Resources.EmailSent %></h2>
        <p><%= ViewData["Message"]%></p>
    </div>
</asp:Content>