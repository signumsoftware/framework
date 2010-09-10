<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reports" %>
<%@ Import Namespace="Signum.Entities.Basics" %>
<%@ Import Namespace="Signum.Entities.Files" %>
<%@ Import Namespace="Signum.Web.Files" %>

<%
using (var e = Html.TypeContext<ExcelReportDN>()) 
{
    using (var query = e.SubContext(f => f.Query))
    {
        Html.WriteEntityInfo(query);
        Html.ValueLine(query, f => f.DisplayName, f => { f.ReadOnly = true; f.LabelText = "Query"; });
    %>
        <%= Html.Hidden(query.Compose("Key"), query.Value.Key)%>
        <%= Html.Hidden(query.Compose("DisplayName"), query.Value.DisplayName)%>
    <%
    }

    Html.ValueLine(e, f => f.DisplayName);
    Html.ValueLine(e, f => f.Deleted, vl => vl.ReadOnly = true);

    Html.FileLine(e, f => f.File, fl => fl.AsyncUpload = false);
}
 %>