<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.Reports" %>
<%@ Import Namespace="Signum.Web.Queries.Models" %>
<%@ Import Namespace="Signum.Entities.Basics" %>

<%
using (var e = Html.TypeContext<UserQueryModel>()) 
{
    %>
    <%= Html.Hidden(e.Compose(UserQueryModel.IdUserQueryKey), e.Value.IdUserQuery) %>
    <%
    using (var query = e.SubContext(f => f.Query))
    {
        Html.WriteEntityInfo(query);
        Html.ValueLine(query, f => f.DisplayName, f => f.ReadOnly = true);
    %>
        <%= Html.Hidden(query.Compose(QueryDN.KeyName), query.Value.Key)%>
        <%= Html.Hidden(query.Compose(QueryDN.DisplayNameName), query.Value.DisplayName)%>
    <%
    }
    Html.ValueLine(e, f => f.DisplayName);
	%>
	<br />
	<%    
    Html.EntityRepeater(e, f => f.Filters, er => er.Creating = 
        EntityRepeater.JsCreating(
            er, 
            new JsViewOptions
            { 
                ControllerUrl = "Queries/NewQueryFilter", 
                RequestExtraJsonData = "{{queryKey:\"{0}\"}}".Formato(e.Value.Query.Key) 
            }).ToJS());
	%>
	<br />
	<%    
    Html.EntityRepeater(e, f => f.Columns, er => er.Creating = 
        EntityRepeater.JsCreating(
            er, 
            new JsViewOptions
            {
                ControllerUrl = "Queries/NewQueryColumn",
                RequestExtraJsonData = "{{queryKey:\"{0}\"}}".Formato(e.Value.Query.Key)
            }).ToJS());
	%>
	<br />
	<%    
        Html.EntityRepeater(e, f => f.Orders, er => er.Creating =
            EntityRepeater.JsCreating(
                er,
                new JsViewOptions
                {
                    ControllerUrl = "Queries/NewQueryOrder",
                    RequestExtraJsonData = "{{queryKey:\"{0}\"}}".Formato(e.Value.Query.Key)
                }).ToJS());
    %>
	<br />
	<%   
    Html.ValueLine(e, f => f.Top);
}
%>
