<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Entities.ControlPanel" %>
<%@ Import Namespace="Signum.Web.ControlPanel" %>
<%@ Import Namespace="Signum.Entities.Reports" %>

<script type="text/javascript">
    function toggleCol(cbFillId, colInputId) {
        if ($("#" + cbFillId + ":checked").length > 0)
            $('#' + colInputId).val(1).attr('disabled', 'disabled');  
        else
            $('#' + colInputId).attr('disabled', '');  
    }
</script>

<% 
    using(var tc = Html.TypeContext<PanelPart>())
    {
        using (var sc = tc.SubContext())
        {
            sc.BreakLine = false;
            sc.ValueFirst = true;
            
            Html.ValueLine(sc, pp => pp.Row, vl => vl.ValueHtmlProps["size"] = 2);
            string colId = Html.ValueLine(sc, pp => pp.Column, vl => vl.ValueHtmlProps["size"] = 2);
            string fillId = Html.ValueLine(sc, pp => pp.Fill, vl => vl.ValueHtmlProps["onclick"] = "toggleCol(this.id,'" + colId + "');");
            %>
<script type="text/javascript">
    $(document).ready(function() { toggleCol('<%= fillId %>','<%= colId %>'); });
</script>
            <%            
            Html.ValueLine(sc, pp => pp.Title);
            Html.EntityLine(sc, pp => pp.Content, el => el.Autocomplete = false);
        }
    }
%>