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

namespace Signum.Engine.DynamicQuery
{
    public static class AutoCompleteUtils
    {
        public static List<Lite> FindLiteLike(Type liteType, Implementations implementations, string subString, int count)
        {
            Type[] types;
            if (implementations == null)
                types = new[] { liteType };
            else if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll not supported for FindLiteLike");
            else
                types = ((ImplementedByAttribute)implementations).ImplementedTypes;

            return (from mi in new[] { miLiteStarting, miLiteContaining }
                    from type in types
                    from lite in (List<Lite>)mi.GetInvoker(liteType, type)(subString, count)
                    select lite).Take(count).ToList();
        }

        static GenericInvoker miLiteStarting = GenericInvoker.Create(() => LiteStarting<TypeDN, TypeDN>(null, 1));
        static List<Lite> LiteStarting<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToStr.StartsWith(subString)).Select(a => a.ToLite<LT>()).Take(count).AsEnumerable().OrderBy(l=>l.ToStr).Cast<Lite>().ToList();
        }

        static GenericInvoker miLiteContaining = GenericInvoker.Create(() => LiteContaining<TypeDN, TypeDN>(null, 1));
        static List<Lite> LiteContaining<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToStr.Contains(subString) && !a.toStr.StartsWith(subString)).Select(a => a.ToLite<LT>()).Take(count).AsEnumerable().OrderBy(l => l.ToStr).Cast<Lite>().ToList();
        }

        public static List<Lite> RetriveAllLite(Type liteType, Implementations implementations)
        {
            if (implementations == null)
            {
                return (List<Lite>)miAllLite.GetInvoker(liteType, liteType)();
            }

            if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for RetrieAllLite");

            return (from type in ((ImplementedByAttribute)implementations).ImplementedTypes
                    from l in (List<Lite>)miAllLite.GetInvoker(liteType, type)()
                    select l).ToList();
        }

        static GenericInvoker miAllLite = GenericInvoker.Create(() => AllLite<TypeDN, TypeDN>());
        static List<Lite> AllLite<LT, RT>()
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Select(a => a.ToLite<LT>()).AsEnumerable().Cast<Lite>().ToList();
        }
    }
}
