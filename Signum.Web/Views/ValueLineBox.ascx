<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Engine" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web.Properties" %>

<%
    using (var e = Html.TypeContext<ValueLineBoxModel>()) 
{
    if (e.Value.TopText.HasText())
    {
        Response.Write(e.Value.TopText + "<div class='clearall'></div>");
    }
	        Html.WriteEntityInfo(e, f => f.Related);
            switch (e.Value.BoxType)
            {
                case ValueLineBoxType.Boolean:
                    Html.ValueLine(e, f => f.BoolValue, vl => vl.LabelText = e.Value.FieldName);
                    break;
                case ValueLineBoxType.Integer:
                    Html.ValueLine(e, f => f.IntValue, vl => vl.LabelText = e.Value.FieldName);
                    break;
                case ValueLineBoxType.Decimal:
                    Html.ValueLine(e, f => f.DecimalValue, vl => vl.LabelText = e.Value.FieldName);
                    break;
                case ValueLineBoxType.DateTime:
                    Html.ValueLine(e, f => f.DateValue, vl => vl.LabelText = e.Value.FieldName);
                    break;
                case ValueLineBoxType.String:
                    Html.ValueLine(e, f => f.StringValue, vl => vl.LabelText = e.Value.FieldName);
                    break;
                default:
                    throw new ArgumentException(Resources.ValueLineBoxType0DoesNotExist.Formato(e.Value.BoxType));
            }
}
%>
