<%@ Control Language="C#" Inherits="System.Web.Mvc.ViewUserControl" %>
<%@ Import Namespace="Signum.Web" %>
<%@ Import Namespace="Signum.Entities" %>
<%@ Import Namespace="Signum.Entities.DynamicQuery" %>
<%@ Import Namespace="Signum.Entities.Reflection" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="Signum.Utilities" %>
<%@ Import Namespace="Signum.Web.Properties" %>
<% Context context = (Context)Model;
   FindOptions findOptions = (FindOptions)ViewData[ViewDataKeys.FindOptions];
   QueryDescription queryDescription = (QueryDescription)ViewData[ViewDataKeys.QueryDescription];
   Type entitiesType = Reflector.ExtractLite(queryDescription.StaticColumns.Single(a => a.IsEntity).Type);
   bool viewable = findOptions.View && Navigator.IsNavigable(entitiesType, true);

    var resultJson = new List<List<string>>();
    
    ResultTable queryResult = (ResultTable)ViewData[ViewDataKeys.Results];
    var entityColumn = (queryResult == null) ? null : queryResult.Columns.OfType<StaticColumn>().Single(c => c.IsEntity);
    Dictionary<int, Func<HtmlHelper, object, string>> formatters = (Dictionary<int, Func<HtmlHelper, object, string>>)ViewData[ViewDataKeys.Formatters];

    foreach (var row in queryResult.Rows)
    {
        var rowJson = new List<string>();
        
        Lite entityField = (Lite)row[entityColumn];

        if (findOptions.AllowMultiple.HasValue)
        {
            if (findOptions.AllowMultiple.Value)
            {
                rowJson.Add(Html.CheckBox(
                    context.Compose("rowSelection", row.Index.ToString()),
                    new { value = entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr }).ToHtmlString());
            }
            else
            {
                rowJson.Add(Html.RadioButton(
                    context.Compose("rowSelection"),
                    entityField.Id.ToString() + "__" + entityField.RuntimeType.Name + "__" + entityField.ToStr).ToHtmlString());
            }
               
        }
        
        if (viewable)
        {
            rowJson.Add(Html.Href(Navigator.ViewRoute(entityField.RuntimeType, entityField.Id), Html.Encode(Resources.View)));
        }
        
        foreach (var col in queryResult.VisibleColumns)
        {
            rowJson.Add(formatters[col.Index](Html, row[col]));
        }
        
        resultJson.Add(rowJson);
    }%>
    <%= new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(resultJson) %>