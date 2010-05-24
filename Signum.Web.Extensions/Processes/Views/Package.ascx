<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Processes" %>

<%
using (var e = Html.TypeContext<PackageDN>()) 
{
	Html.ValueLine(e, f => f.Name);
	Html.EntityLine(e, f => f.Operation, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.NumLines, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.NumErrors, f => f.ReadOnly = true);
	Html.ValueLine(e, f => f.IdOrNull, f => f.ReadOnly = true);
%>
<fieldset>
    <legend>Lines</legend>
    <%
    Html.SearchControl(
      new FindOptions()
      {
          QueryName = typeof(PackageLineDN),
          FilterOptions = { new FilterOption("Package", e.Value) },
          SearchOnLoad = true,
          FilterMode = FilterMode.Hidden,
          Create = false,
          View = false,
          Async = true
      }, e);%>
</fieldset>
<%
}
 %>