<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Help.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Utilities" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
<script src="<%=ClientScript.GetWebResourceUrl(typeof(Navigator), "Signum.Web.Scripts.SF_Globals.js")%>" type="text/javascript"></script>
    <% Html.RenderPartial(HelpClient.ViewPrefix + HelpClient.Menu); %>
    <%
        NamespaceHelp nh = (NamespaceHelp)Model;
         %>
    <form action="Help/Namespace/<%=nh.Name%>/Save" id="form-save">
        <div class="grid_12" id="entityName">    
            <h1><%=nh.Name %></h1> 
            <%= Html.TextArea("description", nh.Description, 5, 80, new { @class = "editable" })
                                            + "<span class=\"editor\" id=\"description-editor\">" + nh.Description.WikiParse(HelpClient.DefaultWikiSettings).Replace("\n", "<p>") + "</span>"%>
        </div>
        <div class="grid_4">
            <div class="sidebar">
                <h3>Temas relacionados</h3>
                <ul>
                   <%
                       List<Type> types = (List<Type>)ViewData["nameSpace"];
                           foreach (Type t in types)
                           {%>
                            <li><a href="<%=HelpLogic.EntityUrl(t) %>"><%=t.NiceName()%></a></li>
                            <%
                           }
                    %>
                </ul>        
            </div>
        </div>
        <div class="clear"></div>
    </form>
</asp:Content>