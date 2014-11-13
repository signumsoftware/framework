using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Engine.Help
{
    public class HelpUrls
    {
        public static string BaseUrl;

        public static string ImagesFolder = "Images";

        public static Func<Type, string> EntityUrl = t => BaseUrl + "Help/Entity/" + TypeLogic.GetCleanName(t);
        public static Func<string, string> NamespaceUrl = ns => BaseUrl + "Help/Namespace/" + ns;
        public static Func<string, string> AppendixUrl = a => BaseUrl + "Help/Appendix/" + a;

        public static string OperationUrl(Type entityType, OperationSymbol operation)
        {
            return EntityUrl(entityType) + "#" + IdOperation(operation);
        }

        public static string IdOperation(OperationSymbol operation)
        {
            return "o-" + operation.Key.Replace('.', '_');
        }

        public static string PropertyUrl(PropertyRoute route)
        {
            return EntityUrl(route.RootType) + "#" + IdProperty(route);
        }

        public static string IdProperty(PropertyRoute route)
        {
            return "p-" + route.PropertyString().Replace('.', '_').Replace('/', '_').Replace('[', '_').Replace(']', '_');
        }

        public static string QueryUrl(object queryName, Type type = null)
        {
            return EntityUrl(type ?? GetQueryType(queryName)) + "#" + IdQuery(queryName);
        }

        public static string IdQuery(object queryName)
        {
            return "q-" + QueryUtils.GetQueryUniqueKey(queryName).ToString().Replace(".", "_");
        }

        public static Type GetQueryType(object query)
        {
            return DynamicQueryManager.Current.GetQuery(query).Core.Value.EntityColumnFactory().Implementations.Value.Types.FirstEx();
        }
    }
}
