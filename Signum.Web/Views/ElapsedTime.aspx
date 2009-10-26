<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="Signum.Utilities" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
    <link href="<%= System.Web.VirtualPathUtility.ToAbsolute("~/Content/Site.css")%>" rel="stylesheet" type="text/css" />
    <link href="<%= System.Web.VirtualPathUtility.ToAbsolute("~/Content/LineStyles.css")%>" rel="stylesheet" type="text/css" />
    
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_AjaxValidation.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_PopupPanel.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_DragAndDrop.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Autocomplete.js")%>" type="text/javascript"></script>
    <script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_SearchEngine.js")%>" type="text/javascript"></script>
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
                                Nombre
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Ejecutado x veces
                            </td>
                        </tr>
                    </table>
                </td>
                <td>
                    <table>
                        <tr>
                            <td>
                                Máximo
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Medio
                            </td>
                        </tr>
                        <tr>
                            <td>
                                Mínimo
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
            <input type="button" id="btnSave" class="OperationDiv" onclick="<%="TrySave('Signum/TrySave');" %>" value="Guardar" />   
        <%} %>
        <%= Html.GetButtonBarElements(Model, ViewData[ViewDataKeys.MainControlUrl].ToString(), "") %>  
        <br />
        <%= Html.ValidationSummaryAjax() %>
        <br />
        <%Html.RenderPartial(ViewData[ViewDataKeys.MainControlUrl].ToString(), Model); %>
        <div id="divASustituir"></div>
 <%}%>
</asp:Content>
