using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using System.Web;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using System.Text.RegularExpressions;
using Signum.Engine;

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

            object rawValue = bindingContext.ValueProvider.GetValue("webQueryName")?.RawValue;
            if (rawValue == null)
                return null;

            string webQueryName = rawValue.GetType() == typeof(string[]) ? ((string[])rawValue)[0]: (string)rawValue;

            if (!webQueryName.HasText())
                throw new InvalidOperationException("webQueryName not provided");

            fo.QueryName = Finder.ResolveQueryName(webQueryName);

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(fo.QueryName);

            fo.FilterOptions = ExtractFilterOptions(controllerContext.HttpContext, queryDescription);
            fo.OrderOptions = ExtractOrderOptions(controllerContext.HttpContext, queryDescription);
            fo.ColumnOptions = ExtractColumnsOptions(controllerContext.HttpContext, queryDescription);

            if (parameters.AllKeys.Contains("allowSelection"))
            {
                bool aux;
                if (bool.TryParse(parameters["allowSelection"], out aux))
                    fo.AllowSelection = aux;
            }

            if (parameters.AllKeys.Contains("allowChangeColumns"))
                fo.AllowChangeColumns = bool.Parse(parameters["allowChangeColumns"]);

            if (parameters.AllKeys.Contains("allowOrder"))
                fo.AllowOrder = bool.Parse(parameters["allowOrder"]);

            if (parameters.AllKeys.Contains("showHeader"))
                fo.ShowHeader =  bool.Parse(parameters["showHeader"]);

            if (parameters.AllKeys.Contains("showFilters"))
                fo.ShowFilters = bool.Parse(parameters["showFilters"]);

            if (parameters.AllKeys.Contains("showFilterButton"))
                fo.ShowFilterButton = bool.Parse(parameters["showFilterButton"]);

            if (parameters.AllKeys.Contains("showFooter"))
                fo.ShowFooter = bool.Parse(parameters["showFooter"]);

            if (parameters.AllKeys.Contains("showContextMenu"))
                fo.ShowContextMenu = bool.Parse(parameters["showContextMenu"]);

            if (parameters.AllKeys.Contains("columnMode"))
                fo.ColumnOptionsMode = parameters["columnMode"].ToEnum<ColumnOptionsMode>();

            if (parameters.AllKeys.Contains("create"))
                fo.Create = bool.Parse(parameters["create"]);

            if (parameters.AllKeys.Contains("navigate"))
                fo.Navigate = bool.Parse(parameters["navigate"]);

            if (parameters.AllKeys.Contains("pagination"))
            {
                switch (parameters["pagination"].ToEnum<PaginationMode>())
                {
                    case PaginationMode.All:
                        fo.Pagination = new Pagination.All();
                        break;
                    case PaginationMode.Firsts:
                        fo.Pagination = new Pagination.Firsts(int.Parse(parameters["elems"]));
                        break;
                    case PaginationMode.Paginate:
                        fo.Pagination = new Pagination.Paginate(int.Parse(parameters["elems"]),
                            parameters.AllKeys.Contains("page") ? parameters["page"].ToInt() ?? 1 : 1);
                        break;
                    default:
                        break;
                }

                if (FindOptions.ReplacePagination(fo.QueryName, fo.Pagination) != fo.Pagination)
                    throw new InvalidOperationException("Pagination mode not allowed");
            }
            
            if (parameters.AllKeys.Contains("searchOnLoad"))
                fo.SearchOnLoad = bool.Parse(parameters["searchOnLoad"]);

            return fo;
        }

        //name1,operation1,value1;name2,operation2,value2 being values CSV encoded
        internal static Regex FilterRegex = new Regex(
            "(?<token>[^;,]+),(?<op>[^;,]+),(?<value>'(?:[^']+|'')*'|[^;,]*)(;|$)".Replace('\'', '"'),
            RegexOptions.Multiline | RegexOptions.ExplicitCapture);

        public static List<FilterOption> ExtractFilterOptions(HttpContextBase httpContext, QueryDescription qd, bool canAggregate = false, string key = null)
        {
            List<FilterOption> result = new List<FilterOption>();

            NameValueCollection parameters = httpContext.Request.Params;

            string field = parameters[key ?? "filters"];

            if (!field.HasText())
                return result;

            var matches = FilterRegex.Matches(field).Cast<Match>();

            return matches.Select(m =>
            {
                string name = m.Groups["token"].Value;
                var token = QueryUtils.Parse(name, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
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
            "(?<token>-?[^;,]+)(;|$)".Replace('\'', '"'),
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
                    Token = QueryUtils.Parse(token, qd, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0)),
                    OrderType = orderType
                };
            }).ToList();
        }

        //columnName1,displayName1;columnName2,displayName2; being displayNames CSV encoded
        internal static Regex ColumnRegex = new Regex(
            "(?<token>[^;,]+)(,(?<name>'(?:[^']+|'')*'|[^;,]*))?(;|$)".Replace('\'', '"'),
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
                var token = QueryUtils.Parse(colName, qd, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
                return new ColumnOption
                {
                    Token = token,
                    ColumnName = colName,
                    DisplayName =m.Groups["name"].Success ? DecodeValue(m.Groups["name"].Value) : null
                };
            }).ToList();
        }

        public static string DecodeValue(string s)
        {
            if (s.StartsWith("\""))
            {
                if (!s.EndsWith("\""))
                    throw new FormatException("Value starts by quotes but not ends with quotes".FormatWith(s));

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
                if (!value.HasText())
                    return null;

                string[] vals = ((string)value).Split(',');
                return (vals[0] == "true" || vals[0] == "True");
            }
            if (type.UnNullify() == typeof(DateTime))
            {
                if (!value.HasText())
                    return null;

                return DateTime.Parse(value).FromUserInterface();
            }
            if (type.UnNullify().IsLite())
                return Database.FillToString(Lite.Parse(value));

            return ReflectionTools.Parse(value, type);
        }
    }
}
