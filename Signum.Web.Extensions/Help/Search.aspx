<%@ Page Title="" Language="C#" MasterPageFile="~/Views/Shared/Help.Master" Inherits="System.Web.Mvc.ViewPage" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="Signum.Engine.Help" %>
<%@ Import Namespace="Signum.Web.Help" %>
<%@ Import Namespace="Signum.Web.Extensions" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

<script type="text/javascript">
    function ShowMore(node)
    {
        $(node).siblings().show();
        $(node).hide();
    }
</script>
<div class="grid_16" id="entityContent">
    <h1>Buscador</h1>    
    <% string q = Request.Params["q"];
       int maxResults = 3;
       int count = ((System.Collections.Generic.Dictionary<string, System.Collections.Generic.IEnumerable<SearchResult>>)Model).Count;        
        
         %>
    <p id="title"><%= count %> <%= count == 1 ? "resultado" : "resultados"%> para <b><%= q %></b> (en <%=ViewData["time"] %> ms)</p><hr/>
    <ul>
<%
    foreach (var v in ((System.Collections.Generic.Dictionary<string,System.Collections.Generic.IEnumerable<SearchResult>>) Model))
    {
        int currentResults = 0; 
    %>
        <li class="result"><span class="entityName"><%= v.Key.AddHtml("<b>", "</b>", q)%></span>
        <ul>
    <%
    
    foreach (var sr in v.Value)
    {
        if (currentResults != maxResults)
        {
            currentResults++;            
     %>       
    <li class="content"><span class="typeSearchResult <%=sr.TypeSearchResult.ToString() %>"><%=sr.TypeSearchResult.ToString()%></span>
    <a href="<%= sr.Link%>"><%=sr.Content.WikiParse(HelpClient.NoLinkWikiSettings).AddHtml("<b>", "</b>", q)%></a></li>
    <%
        }
        else
        {
            if (currentResults == maxResults)
            {
                currentResults++;
                %>
                <li><a class="more-link" onclick="javascript:ShowMore(this);">Mostrar <%= v.Value.Count() - maxResults %> <%= (v.Value.Count() - maxResults) != 1 ? "resultados restantes" : "resultado restante"%></a>
                <div class="more">
                    <li class="content"><span class="typeSearchResult <%=sr.TypeSearchResult.ToString() %>"><%=sr.TypeSearchResult.ToString()%></span>

                    <a href="<%= HelpLogic.EntityUrl(sr.Type)%>"><%=sr.Content.WikiParse(HelpClient.NoLinkWikiSettings).AddHtml("<b>", "</b>", q)%></a></li>
                
          <%  }
        }
    }
    if (v.Value.Count() > maxResults)
    { %> </div> <% }            
    %>
    </ul>
    </li>
    
  <%} %>
</ul>
</div>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="head" runat="server">
    <link href="<%= Request.Url.GetLeftPart( UriPartial.Authority ) + VirtualPathUtility.ToAbsolute( "~/Content/help.css" ) %>"
        rel="stylesheet" type="text/css" />
</asp:Content>
