#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Web.Properties;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using System.Web;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using Signum.Engine;
#endregion

namespace Signum.Web
{
    public class FindOptionsModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            FindOptions fo = new FindOptions();

            NameValueCollection parameters = controllerContext.HttpContext.Request.Params;

            if (parameters.AllKeys.Any(name => !name.HasText()))
                throw new Exception("Incorrect URL: " + controllerContext.HttpContext.Request.Url.ToString());

            string webQueryName = "";
            object rawValue = bindingContext.ValueProvider.GetValue("webQueryName").TryCC(vp => vp.RawValue);
            if (rawValue.GetType() == typeof(string[]))
                webQueryName = ((string[])rawValue)[0];
            else 
                webQueryName = (string)rawValue;

            if (!webQueryName.HasText())
                throw new InvalidOperationException("webQueryName not provided");

            fo.QueryName = Navigator.ResolveQueryName(webQueryName);

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(fo.QueryName);

            fo.FilterOptions = ExtractFilterOptions(controllerContext.HttpContext, queryDescription);
            fo.OrderOptions = ExtractOrderOptions(controllerContext.HttpContext, queryDescription);
            fo.ColumnOptions = ExtractColumnsOptions(controllerContext.HttpContext, queryDescription);

            if (parameters.AllKeys.Contains("allowMultiple"))
            {
                bool aux;
                if (bool.TryParse(parameters["allowMultiple"], out aux))
                    fo.AllowMultiple = aux;
            }

            if (parameters.AllKeys.Contains("allowChangeColumns"))
                fo.AllowChangeColumns = bool.Parse(parameters["allowChangeColumns"]);

            if (parameters.AllKeys.Contains("filterMode"))
            {
                FilterMode mode = parameters["filterMode"].ToEnum<FilterMode>();
                if (mode == FilterMode.AlwaysHidden || mode == FilterMode.OnlyResults)
                {
                    if (controllerContext.HttpContext.Request.QueryString.AllKeys.Contains("filterMode"))
                        throw new InvalidOperationException("QueryString cannot contain FilterMode set to Always Hidden or Only Results");
                }
                fo.FilterMode = mode;
            }

            if (parameters.AllKeys.Contains("columnMode"))
                fo.ColumnOptionsMode = parameters["columnMode"].ToEnum<ColumnOptionsMode>();

            if (parameters.AllKeys.Contains("create"))
                fo.Create = bool.Parse(parameters["create"]);

            if (parameters.AllKeys.Contains("view"))
                fo.Navigate = bool.Parse(parameters["view"]);

            if (parameters.AllKeys.Contains("elems"))
            {
                int elems;
                if (int.TryParse(parameters["elems"], out elems))
                    fo.ElementsPerPage = elems;
            }

            if (parameters.AllKeys.Contains("searchOnLoad"))
                fo.SearchOnLoad = bool.Parse(parameters["searchOnLoad"]);

            return fo;
        }

        //name1,operation1,value1;name2,operation2,value2; being values CSV encoded
        internal static Regex FilterRegex = new Regex(
            "(?<token>[^;,]+),(?<op>[^;,]+),(?<value>'(?:[^']+|'')*'|[^;,]*);".Replace('\'', '"'),
            RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        public static List<FilterOption> ExtractFilterOptions(HttpContextBase httpContext, QueryDescription qd, bool canAggregate = false)
        {
            List<FilterOption> result = new List<FilterOption>();

            NameValueCollection parameters = httpContext.Request.Params;
            
            string field = parameters["filters"];

            if (!field.HasText())
                return result;

            var matches = FilterRegex.Matches(field).Cast<Match>();

            return matches.Select(m =>
            {
                string name = m.Groups["token"].Value;
                var token = QueryUtils.Parse(name, qd, canAggregate);
                return new FilterOption
                {
                    ColumnName = name,
                    Token = token,
                    Operation = EnumExtensions.ToEnum<FilterOperation>(m.Groups["op"].Value),
                    Value = Convert(DecodeValue(m.Groups["value"].Value), token.Type),
                    //Frozen = frozen,
                };
            }).ToList();
        }

        //order1,-order2; minus symbol indicating descending
        internal static Regex OrderRegex = new Regex(
            "(?<token>-?[^;,]+);".Replace('\'', '"'),
            RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        public static List<OrderOption> ExtractOrderOptions(HttpContextBase httpContext, QueryDescription qd, bool canAggregate = false)
        {
            List<OrderOption> result = new List<OrderOption>();

            NameValueCollection parameters = httpContext.Request.Params;
            string field = parameters["orders"];
            
            if (!field.HasText())
                return result;

            var matches = OrderRegex.Matches(field).Cast<Match>();

            return matches.Select(m =>
            {
                var tokenCapture = m.Groups["token"].Value;
                OrderType orderType = tokenCapture.StartsWith("-") ? OrderType.Descending : OrderType.Ascending;
                string token = orderType == OrderType.Ascending ? tokenCapture : tokenCapture.Substring(1, tokenCapture.Length - 1);
                return new OrderOption
                {
                    ColumnName = token,
                    Token = QueryUtils.Parse(token, qd, canAggregate),
                    OrderType = orderType
                };
            }).ToList();
        }

        //columnName1,displayName1;columnName2,displayName2; being displayNames CSV encoded
        internal static Regex ColumnRegex = new Regex(
            "(?<token>[^;,]+)(,(?<name>'(?:[^']+|'')*'|[^;,]*))?;".Replace('\'', '"'),
            RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        public static List<ColumnOption> ExtractColumnsOptions(HttpContextBase httpContext, QueryDescription qd, bool canAggregate = false)
        {
            List<ColumnOption> result = new List<ColumnOption>();

            NameValueCollection parameters = httpContext.Request.Params;
            string field = parameters["columns"];
            
            if (!field.HasText())
                return result;

            var matches = ColumnRegex.Matches(field).Cast<Match>();

            return matches.Select(m =>
            {
                var colName = m.Groups["token"].Value;
                var displayCapture = m.Groups["name"].Captures;
                var token = QueryUtils.Parse(colName, qd, canAggregate);
                return new ColumnOption
                {
                    Token = token,
                    ColumnName = colName,
                    DisplayName = displayCapture.Count > 0 ? DecodeValue(m.Groups["name"].Value) : colName
                };
            }).ToList();
        }

        public static string DecodeValue(string s)
        {
            if (s.StartsWith("\""))
            {
                if (!s.EndsWith("\""))
                    throw new FormatException("Value starts by quotes but not ends with quotes".Formato(s));

                return s.Substring(1, s.Length - 2).Replace("\"\"", "\"");
            }
            else
            {
                return s;
            }
        }

        public static object Convert(string value, Type type)
        {
            if (type.UnNullify() == typeof(bool))
            {
                string[] vals = ((string)value).Split(',');
                return (vals[0] == "true" || vals[0] == "True");
            }
            if (type.UnNullify() == typeof(DateTime))
            {
                if (value.HasText())
                    return DateTime.Parse(value).FromUserInterface();
                return null;
            }
            if (type.UnNullify().IsLite())
                return Database.FillToString(Lite.Parse(value));

            return ReflectionTools.Parse(value, type); 
        }
    }
}
