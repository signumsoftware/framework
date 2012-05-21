using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Data;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Properties;
using Signum.Entities;
using Signum.Engine.Linq;
using System.Collections;
using Signum.Engine.Maps;

namespace Signum.Engine.DynamicQuery
{
    public static class AutoCompleteUtils
    {
        public static List<Lite> FindLiteLike(Type liteType, Implementations implementations, string subString, int count)
        {
            Type[] types;
            if (implementations == null)
            {
                if (!Schema.Current.Tables.ContainsKey(liteType))
                    throw new InvalidOperationException("Type {0} is not in the schema".Formato(liteType));

                types = new[] { liteType };
            }
            else if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll not supported for FindLiteLike");
            else
                types = ((ImplementedByAttribute)implementations).ImplementedTypes;

            return FindLiteLike(liteType, types, subString, count);
        }

        public static List<Lite> FindLiteLike(Type liteType, Type[] types, string subString, int count)
        {
            return (from mi in new[] { miLiteStarting, miLiteContaining }
                    from type in types
                    from lite in mi.GetInvoker(liteType, type)(subString, count)
                    select lite).Take(count).ToList();
        }

        static GenericInvoker<Func<string, int, List<Lite>>> miLiteStarting = new GenericInvoker<Func<string, int, List<Lite>>>((ss, c) => LiteStarting<TypeDN, TypeDN>(ss, c));
        static List<Lite> LiteStarting<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToString().StartsWith(subString)).Select(a => a.ToLite<LT>()).Take(count).AsEnumerable().OrderBy(l=>l.ToString()).Cast<Lite>().ToList();
        }

        static GenericInvoker<Func<string, int, List<Lite>>> miLiteContaining = new GenericInvoker<Func<string, int, List<Lite>>>((ss, c) => LiteContaining<TypeDN, TypeDN>(ss, c));
        static List<Lite> LiteContaining<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToString().Contains(subString) && !a.ToString().StartsWith(subString)).Select(a => a.ToLite<LT>()).Take(count).AsEnumerable().OrderBy(l => l.ToString()).Cast<Lite>().ToList();
        }

        public static List<Lite> RetrieveAllLite(Type liteType, Implementations implementations)
        {
            if (implementations == null)
            {
                return Database.RetrieveAllLite(liteType);
            }

            if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for RetrieAllLite");

            return ((ImplementedByAttribute)implementations).ImplementedTypes.SelectMany(type => Database.RetrieveAllLite(type)).ToList();
        }

        static GenericInvoker<Func<List<Lite>>> miAllLite = new GenericInvoker<Func<List<Lite>>>(() => AllLite<TypeDN, TypeDN>());
        static List<Lite> AllLite<ST, RT>()
            where ST : class, IIdentifiable
            where RT : IdentifiableEntity, ST
        {
            return Database.Query<RT>().Select(a => a.ToLite<ST>()).AsEnumerable().Cast<Lite>().ToList();
        }
    }
}
