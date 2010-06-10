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
                throw new InvalidOperationException(Resources.QueryUrlNameWasNotProvided);

            fo.QueryName = Navigator.ResolveQueryFromUrlName(queryUrlName);

            fo.FilterOptions = ExtractFilterOptions(controllerContext.HttpContext, fo.QueryName);

            fo.OrderOptions = ExtractOrderOptions(controllerContext.HttpContext, fo.QueryName);

            if (parameters.AllKeys.Any(k => k == "sfAllowMultiple"))
            {
                bool aux;
                if (bool.TryParse(parameters["sfAllowMultiple"], out aux))
                    fo.AllowMultiple = aux;
            }

            if (parameters.AllKeys.Any(k => k == "sfAsync"))
            {
                bool aux;
                if (bool.TryParse(parameters["sfAsync"], out aux))
                    fo.Async = aux;
            }

            if (parameters.AllKeys.Any(k => k == "sfFilterMode"))
                fo.FilterMode = (FilterMode)Enum.Parse(typeof(FilterMode), parameters["sfFilterMode"]);

            if (parameters.AllKeys.Any(k => k == "sfCreate"))
                fo.Create = bool.Parse(parameters["sfCreate"]);

            if (parameters.AllKeys.Any(k => k == "sfView"))
                fo.View = bool.Parse(parameters["sfView"]);

            if (parameters.AllKeys.Any(k => k == "sfSearchOnLoad"))
                fo.SearchOnLoad = bool.Parse(parameters["sfSearchOnLoad"]);

            return fo;
        }

        public static List<FilterOption> ExtractFilterOptions(HttpContextBase httpContext, object queryName)
        {
            List<FilterOption> result = new List<FilterOption>();

            QueryDescription queryDescription = DynamicQueryManager.Current.QueryDescription(queryName);

            NameValueCollection parameters = httpContext.Request.Params;
            var names = parameters.AllKeys.Where(k => k.StartsWith("cn"));
            foreach (string nameKey in names)
            {
                int index;
                if (!int.TryParse(nameKey.RemoveLeft(2), out index))
                    continue;

                string name = parameters[nameKey];
                string value = parameters["val" + index.ToString()];
                string operation = parameters["sel" + index.ToString()];
                bool frozen = parameters.AllKeys.Any(k => k == "fz" + index.ToString());

                //Type type = queryDescription.StaticColumns
                //            .SingleOrDefault(c => c.Name == name)
                //            .ThrowIfNullC(Resources.InvalidFilterColumn0NotFound.Formato(name))
                //           .Type;

                QueryToken token = QueryToken.Parse(queryDescription, name);

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
                    Token = QueryToken.Parse(queryDescription, token),
                    Type = orderType
                });
            }

            return result;
        }

        internal static object Convert(string value, Type type)
        {
            if (type.UnNullify() == typeof(bool))
            {
                string[] vals = ((string)value).Split(',');
                return (vals[0] == "true");
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
                    return Lite.Create(liteType, intValue);
                }
                else
                    return null;
            }

            return ReflectionTools.Parse(value, type); 
        }
    }
}
