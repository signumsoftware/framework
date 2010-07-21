<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<%@ Import Namespace="Signum.Utilities" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <title>Error</title>
    <base href="<%= Request.Url.GetLeftPart( UriPartial.Authority ) + VirtualPathUtility.ToAbsolute( "~/" ) %>" />
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
    <div id="main-home">
        <%
            HandleErrorInfo hei = ViewData.Model as HandleErrorInfo;
            Exception ex = hei != null ? hei.Exception : ViewData.Model as Exception;
            if (ex is ApplicationException)
            {
        %>
        <h1>
            <%=Html.Encode(ex.Message)%></h1>
        <%
            }
            else
            {
        %>
        <h1>
            <%=Html.Encode("Error " + this.ViewContext.HttpContext.Response.StatusCode)%></h1>
        <h2>
            <%=Html.Encode("Error thrown")%></h2>
        <% } 
            if (hei != null)
            {
                
        %>
        <div class="error-region">
            <p>
                <span>Controller: </span><code>
                    <%=hei.ControllerName%></code>
            </p>
            <p>
                <span>Action: </span><code>
                    <%=hei.ActionName%></code>
            </p>
        </div>
        <%
            }
        %>
        <div class="error-region">
            <span>Message: </span>
            <pre>
                <code>
                    <%= ex.Message%>
                </code>
            </pre>
            <span>StackTrace: </span>
            <pre>
                <code>
                    <%= ex.StackTrace%>
                </code>
            </pre>
        </div>
    </div>
</asp:Content>
