<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>

<%
    List<WidgetNode> widgets =
        Html.GetWidgetsListForViewName(Model,(string)ViewData[ViewDataKeys.MainControlUrl], "");

    foreach (WidgetNode wn in widgets)
    {
        if (wn.Show)
        { %>                     
                <li class="secondary">
                    <a id="<%=wn.Id%>" class="tooltipped" <%= (wn.Href != null) ? "href=\"" + wn.Href + "\"" : "" %>><%=wn.Label%><span class="update-number count<%=wn.Count%>"><%=wn.Count%></span></a>
                    <div id="tt<%=wn.Id%>" class="tooltip">
                        <%= wn.Content %>
                    </div>
                </li>
        <%}
    }%>