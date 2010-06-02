<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%
    List<WidgetItem> widgets;
    
    if (ViewData["WidgetNode"] == null && Model as ModifiableEntity != null)
        widgets = Html.GetWidgetsListForViewName((ModifiableEntity)Model, (string)ViewData[ViewDataKeys.PartialViewName]);
    else {
        widgets = new List<WidgetItem>();
        widgets.Add((WidgetItem)ViewData["WidgetNode"]);
    }

    foreach (WidgetItem wn in widgets)
    {
        if (wn != null && wn.Show)
        { %>                     
                <li class="secondary">
                    <%=wn.Label%>
                    <%=wn.Content%>

                </li>
        <%}
    }%>  