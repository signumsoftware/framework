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
            if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll not supported for FindLiteLike");

            return FindLiteLike(liteType, implementations.Types, subString, count);
        }

        public static List<Lite> FindLiteLike(Type liteType, IEnumerable<Type> types, string subString, int count)
        {
            types = types.Where(t => Schema.Current.IsAllowed(t) == null);

            List<Lite> results = new List<Lite>();
            int? id = subString.ToInt();
            if (id.HasValue)
            {
                foreach (var t in types)
                {
                    var lite = miLiteById.GetInvoker(liteType, t).Invoke(id.Value);
                    if (lite != null)
                    {
                        results.Add(lite);

                        if (results.Count >= count)
                            return results;
                    }
                }
            }

            foreach (var t in types)
            {
                results.AddRange(miLiteStarting.GetInvoker(liteType, t)(subString, count - results.Count));

                if (results.Count >= count)
                    return results;
            }

            foreach (var t in types)
            {
                results.AddRange(miLiteContaining.GetInvoker(liteType, t)(subString, count - results.Count));

                if (results.Count >= count)
                    return results;
            }

            return results;
        }

        static GenericInvoker<Func<int, Lite>> miLiteById = new GenericInvoker<Func<int, Lite>>(id => LiteById<TypeDN, TypeDN>(id));
        static Lite LiteById<LT, RT>(int id)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.id == id).Select(a => a.ToLite<LT>()).SingleOrDefault();
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
            if (implementations.IsByAll)
                throw new InvalidOperationException("ImplementedByAll is not supported for RetrieveAllLite");

            return implementations.Types.SelectMany(type => Database.RetrieveAllLite(type)).ToList();
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
