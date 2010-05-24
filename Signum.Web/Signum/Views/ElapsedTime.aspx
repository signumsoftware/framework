<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web.Properties" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="<%= System.Web.VirtualPathUtility.ToAbsolute("~/Content/Site.css")%>" rel="stylesheet" type="text/css" />
    <link href="<%= System.Web.VirtualPathUtility.ToAbsolute("~/Content/LineStyles.css")%>" rel="stylesheet" type="text/css" />
    <script src="signum/Scripts/SF_Globals.js" type="text/javascript"></script>
</asp:Content>

<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
<ul id="tasks">
        <%
            foreach (Pair<string, ElapsedTimeEntity> pair in ElapsedTime.IdentifiedElapseds)
            {    
         %>
    <li class="task">
        <table>
            <tr>
                <td>
                    <table>
                        <tr>
                            <td>
                                <%=HttpUtility.HtmlEncode(Resources.Name) %>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <%=HttpUtility.HtmlEncode(Resources.ExecutedXTimes) %>
                            </td>
                        </tr>
                    </table>
                </td>
                <td>
                    <table>
                        <tr>
                            <td>
                                <%=HttpUtility.HtmlEncode(Resources.Maximum) %>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <%=HttpUtility.HtmlEncode(Resources.Average) %>
                            </td>
                        </tr>
                        <tr>
                            <td>
                                <%=HttpUtility.HtmlEncode(Resources.Minimum) %>
                            </td>
                        </tr>                                                
                    </table>
                </td>
            </tr>
        </table>
    </li>
    <% } %>
</ul>


<% using(Html.BeginForm("DoPostBack","Signum","POST")){ %>
     <h2><%= ViewData[ViewDataKeys.PageTitle] ?? ""%></h2>
        <%if (Model != null && Navigator.Manager.ShowOkSave(Model.GetType(), false)){ %>
            <input type="button" id="btnSave" class="ButtonDiv" onclick="<%="TrySave({});" %>" value="<%=HttpUtility.HtmlEncode(Resources.Save) %>" />   
        <%} %>
        <%= Html.GetButtonBarElements(Model, ViewData[ViewDataKeys.PartialViewName].ToString(), "")%>  
        <br />
        <%= Html.ValidationSummaryAjax() %>
        <br />
        <%Html.RenderPartial(ViewData[ViewDataKeys.PartialViewName].ToString(), Model); %>
        <div id="divASustituir"></div>
 <%}%>
</asp:Content>
