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

            string queryUrlName = "";
            object rawValue = bindingContext.ValueProvider.GetValue("sfQueryUrlName").TryCC(vp => vp.RawValue);
            if (rawValue.GetType() == typeof(string[]))
                queryUrlName = ((string[])rawValue)[0];
            else 
                queryUrlName = (string)rawValue;

            if (!queryUrlName.HasText())
                throw new InvalidOperationException("queryUrlName not provided");

            fo.QueryName = Navigator.ResolveQueryFromUrlName(queryUrlName);

            fo.FilterOptions = ExtractFilterOptions(controllerContext.HttpContext, fo.QueryName);
            fo.OrderOptions = ExtractOrderOptions(controllerContext.HttpContext, fo.QueryName);
            fo.ColumnOptions = ExtractColumnsOptions(controllerContext.HttpContext, fo.QueryName);

            if (parameters.AllKeys.Contains("sfAllowMultiple"))
            {
                bool aux;
                if (bool.TryParse(parameters["sfAllowMultiple"], out aux))
                    fo.AllowMultiple = aux;
            }

            if (parameters.AllKeys.Contains("sfAsync"))
            {
                bool aux;
                if (bool.TryParse(parameters["sfAsync"], out aux))
                    fo.Async = aux;
            }

            if (parameters.AllKeys.Contains("sfFilterMode"))
            {
                FilterMode mode = parameters["sfFilterMode"].ToEnum<FilterMode>();
                if (mode == FilterMode.AlwaysHidden || mode == FilterMode.OnlyResults)
                {
                    if (controllerContext.HttpContext.Request.QueryString.AllKeys.Contains("sfFilterMode"))
                        throw new InvalidOperationException("QueryString cannot contain FilterMode set to Always Hidden or Only Results");
                }
                fo.FilterMode = mode;
            }

            if (parameters.AllKeys.Contains("sfColumnMode"))
                fo.ColumnOptionsMode = parameters["sfColumnMode"].ToEnum<ColumnOptionsMode>();

            if (parameters.AllKeys.Contains("sfCreate"))
                fo.Create = bool.Parse(parameters["sfCreate"]);

            if (parameters.AllKeys.Contains("sfView"))
                fo.View = bool.Parse(parameters["sfView"]);

            if (parameters.AllKeys.Contains("sfTop"))
            {
                int aux;
                if (int.TryParse(parameters["sfTop"], out aux))
                    fo.Top = aux;
            }

            if (parameters.AllKeys.Contains("sfSearchOnLoad"))
                fo.SearchOnLoad = bool.Parse(parameters["sfSearchOnLoad"]);

            return fo;
        }

        public static List<FilterOption> ExtractFilterOptions(HttpContextBase httpContext, object queryName)
        {
            List<FilterOption> result = new List<FilterOption>();

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            NameValueCollection parameters = httpContext.Request.Params;
            if (parameters.AllKeys.Any(name => !name.HasText()))
                throw new Exception("Incorrect URL: " + httpContext.Request.Url.ToString());

            var names = parameters.AllKeys.Where(k => k.StartsWith("cn"));
            foreach (string nameKey in names)
            {
                int index;
                if (!int.TryParse(nameKey.RemoveLeft(2), out index))
                    continue;

                string name = parameters[nameKey];
                string value = parameters["val" + index.ToString()];
                string operation = parameters["sel" + index.ToString()];
                bool frozen = parameters.AllKeys.Contains("fz" + index.ToString());

                QueryToken token = QueryUtils.Parse(name, queryDescription);

                object valueObject = Convert(value, token.Type);

                result.Add(new FilterOption
                {
                    ColumnName = name,
                    Token = token,
                    Operation = EnumExtensions.ToEnum<FilterOperation>(operation),
                    Frozen = frozen,
                    Value = valueObject,
                });
            }
            return result;
        }

        public static List<OrderOption> ExtractOrderOptions(HttpContextBase httpContext, object queryName)
        {
            List<OrderOption> result = new List<OrderOption>();

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            NameValueCollection parameters = httpContext.Request.Params;
            string field = parameters["sfOrderBy"];
            
            if (!field.HasText())
                return result;

            string[] orderArray = field.Split(new []{","}, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string currentOrderString in orderArray)
            {
                OrderType orderType = currentOrderString.StartsWith("-") ? OrderType.Descending : OrderType.Ascending;
                string token = orderType == OrderType.Ascending ? currentOrderString : currentOrderString.Substring(1, currentOrderString.Length-1);
                result.Add(new OrderOption
                {
                    Token = QueryUtils.Parse(token, queryDescription),
                    OrderType = orderType
                });
            }

            return result;
        }

        public static List<ColumnOption> ExtractColumnsOptions(HttpContextBase httpContext, object queryName)
        {
            List<ColumnOption> result = new List<ColumnOption>();

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            NameValueCollection parameters = httpContext.Request.Params;
            string field = parameters["sfColumns"];
            
            if (!field.HasText())
                return result;

            string[] colArray = field.Split(new []{","}, StringSplitOptions.RemoveEmptyEntries);
            
            int numStaticCols = queryDescription.Columns.Count;

            for (int i = 0; i < colArray.Length; i++)
            {
                string[] currentColString = colArray[i].Split(';');

                result.Add(new ColumnOption
                {
                    ColumnName = currentColString[0],
                    DisplayName = currentColString[1],
                });
            }

            return result;
        }

        internal static object Convert(string value, Type type)
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
            if (typeof(Lite).IsAssignableFrom(type.UnNullify()))
            {
                string[] vals = ((string)value).Split(';');
                int intValue;
                if (vals[0].HasText() && int.TryParse(vals[0], out intValue))
                {
                    Type liteType = Navigator.NamesToTypes[vals[1]];
                    if (typeof(Lite).IsAssignableFrom(liteType))
                        liteType = Reflector.ExtractLite(liteType);
                    return Database.RetrieveLite(liteType, intValue);
                }
                else
                    return null;
            }

            return ReflectionTools.Parse(value, type); 
        }
    }
}
