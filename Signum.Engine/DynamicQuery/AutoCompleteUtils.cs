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
        public static List<Lazy> FindLazyLike(Type lazyType, Type[] types, string subString, int count)
        {
            List<Lazy> result = new List<Lazy>();

            foreach (var mi in new[] { miLazyStarting, miLazyContaining })
            {
                foreach (var type in types ?? new[] { lazyType })
                {
                    MethodInfo mig = mi.MakeGenericMethod(lazyType, type);
                    try
                    {
                        List<Lazy> part = (List<Lazy>)mig.Invoke(null, new object[] { subString, count - result.Count });
                        result.AddRange(part);

                        if (result.Count >= count)
                            return result;
                    }
                    catch (TargetInvocationException te)
                    {
                        throw te.InnerException;
                    }
                }
            }
            return result;
        }

        static MethodInfo miLazyStarting = typeof(AutoCompleteUtils).GetMethod("LazyStarting", BindingFlags.NonPublic | BindingFlags.Static);
        static List<Lazy> LazyStarting<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToStr.StartsWith(subString)).Select(a => a.ToLazy<LT>()).Take(count).AsEnumerable().Cast<Lazy>().ToList();
        }

        static MethodInfo miLazyContaining = typeof(AutoCompleteUtils).GetMethod("LazyContaining", BindingFlags.NonPublic | BindingFlags.Static);
        static List<Lazy> LazyContaining<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToStr.Contains(subString) && !a.toStr.StartsWith(subString)).Select(a => a.ToLazy<LT>()).Take(count).AsEnumerable().Cast<Lazy>().ToList();
        }

        public static List<Lazy> RetriveAllLazy(Type lazyType, Type[] types)
        {
            List<Lazy> result = new List<Lazy>();

            foreach (var type in types ?? new[] { lazyType })
            {
                MethodInfo mi = miAllLazy.MakeGenericMethod(lazyType, type);
                try
                {
                    List<Lazy> part = (List<Lazy>)mi.Invoke(null, null);
                    result.AddRange(part);
                }
                catch (TargetInvocationException te)
                {
                    throw te.InnerException;
                }
            }
            return result;
        }

        static MethodInfo miAllLazy = typeof(AutoCompleteUtils).GetMethod("AllLazy", BindingFlags.NonPublic | BindingFlags.Static);
        static List<Lazy> AllLazy<LT, RT>()
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Select(a => a.ToLazy<LT>()).AsEnumerable().Cast<Lazy>().ToList();
        }
    }
}
