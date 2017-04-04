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
    public class QueryRequestModelBinder : IModelBinder
    {
        public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            QueryRequest qr = new QueryRequest();

            NameValueCollection parameters = controllerContext.HttpContext.Request.Params;

            if (parameters.AllKeys.Any(name => !name.HasText()))
                throw new Exception("Incorrect URL: " + controllerContext.HttpContext.Request.Url.ToString());

            string webQueryName = "";
            object rawValue = bindingContext.ValueProvider.GetValue("webQueryName")?.RawValue;
            if (rawValue.GetType() == typeof(string[]))
                webQueryName = ((string[])rawValue)[0];
            else
                webQueryName = (string)rawValue;

            if (!webQueryName.HasText())
                throw new InvalidOperationException("webQueryName not provided");

            qr.QueryName = Finder.ResolveQueryName(webQueryName);

            if (parameters.AllKeys.Contains("queryUrl"))
                qr.QueryUrl = parameters["queryUrl"];

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(qr.QueryName);

            qr.Filters = ExtractFilterOptions(controllerContext.HttpContext, queryDescription, canAggregate: false);
            qr.Orders = ExtractOrderOptions(controllerContext.HttpContext, queryDescription, canAggregate: false);
            qr.Columns = ExtractColumnsOptions(controllerContext.HttpContext, queryDescription, canAggregate: false);

            if (parameters.AllKeys.Contains("pagination"))
            {
                switch (parameters["pagination"].ToEnum<PaginationMode>())
                {
                    case PaginationMode.All:
                        qr.Pagination = new Pagination.All();
                        break;
                    case PaginationMode.Firsts:
                        qr.Pagination = new Pagination.Firsts(
                            parameters.AllKeys.Contains("elems") ? int.Parse(parameters["elems"]) : Pagination.Firsts.DefaultTopElements);
                        break;
                    case PaginationMode.Paginate:
                        qr.Pagination = new Pagination.Paginate(
                            parameters.AllKeys.Contains("elems") ? int.Parse(parameters["elems"]) : Pagination.Paginate.DefaultElementsPerPage,
                            parameters.AllKeys.Contains("page") ? int.Parse(parameters["page"]) : 1);
                        break;
                    default:
                        break;
                }
            }

            return qr;
        }

        public static List<Signum.Entities.DynamicQuery.Filter> ExtractFilterOptions(HttpContextBase httpContext, QueryDescription queryDescription, bool canAggregate, string key = null)
        {
            List<Signum.Entities.DynamicQuery.Filter> result = new List<Signum.Entities.DynamicQuery.Filter>();

            NameValueCollection parameters = httpContext.Request.Params;
            
            string field = parameters[key ?? "filters"];
            if (!field.HasText())
                return result;

            var matches = FindOptionsModelBinder.FilterRegex.Matches(field).Cast<Match>();

            return matches.Select(m =>
            {
                string name = m.Groups["token"].Value;
                var token = QueryUtils.Parse(name, queryDescription, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));
                return new Signum.Entities.DynamicQuery.Filter(token,
                    EnumExtensions.ToEnum<FilterOperation>(m.Groups["op"].Value),
                    FindOptionsModelBinder.Convert(FindOptionsModelBinder.DecodeValue(m.Groups["value"].Value), token.Type));
            }).ToList();
        }

        public static List<Order> ExtractOrderOptions(HttpContextBase httpContext, QueryDescription description, bool canAggregate)
        {
            List<Order> result = new List<Order>();

            NameValueCollection parameters = httpContext.Request.Params;
            string field = parameters["orders"];
            
            if (!field.HasText())
                return result;

            var matches = FindOptionsModelBinder.OrderRegex.Matches(field).Cast<Match>();

            return matches.Select(m =>
            {
                var tokenCapture = m.Groups["token"].Value;
                
                OrderType orderType = tokenCapture.StartsWith("-") ? OrderType.Descending : OrderType.Ascending;
                string token = orderType == OrderType.Ascending ? tokenCapture : tokenCapture.Substring(1, tokenCapture.Length - 1);

                return new Order(QueryUtils.Parse(token, description, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0)), orderType);
            }).ToList();
        }

        public static List<Column> ExtractColumnsOptions(HttpContextBase httpContext, QueryDescription description, bool canAggregate)
        {
            List<Column> result = new List<Column>();

            NameValueCollection parameters = httpContext.Request.Params;
            string field = parameters["columns"];
            
            if (!field.HasText())
                return result;

            var matches = FindOptionsModelBinder.ColumnRegex.Matches(field).Cast<Match>();

            return matches.Select(m =>
            {
                var colName = m.Groups["token"].Value;
                string displayName = m.Groups["name"].Success ? FindOptionsModelBinder.DecodeValue(m.Groups["name"].Value) : null;

                var token = QueryUtils.Parse(colName, description, SubTokensOptions.CanElement | (canAggregate ? SubTokensOptions.CanAggregate : 0));

                return new Column(token, displayName ?? token.NiceName());
            }).ToList();
        }
    }
}
