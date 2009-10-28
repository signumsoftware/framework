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
        public static List<Lite> FindLiteLike(Type liteType, Type[] types, string subString, int count)
        {
            List<Lite> result = new List<Lite>();

            foreach (var mi in new[] { miLiteStarting, miLiteContaining })
            {
                foreach (var type in types ?? new[] { liteType })
                {
                    MethodInfo mig = mi.MakeGenericMethod(liteType, type);
                    try
                    {
                        List<Lite> part = (List<Lite>)mig.Invoke(null, new object[] { subString, count - result.Count });
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

        static MethodInfo miLiteStarting = ReflectionTools.GetMethodInfo(()=>LiteStarting<TypeDN,TypeDN>(null, 1)).GetGenericMethodDefinition();
        static List<Lite> LiteStarting<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToStr.StartsWith(subString)).Select(a => a.ToLite<LT>()).Take(count).AsEnumerable().OrderBy(l=>l.ToStr).Cast<Lite>().ToList();
        }

        static MethodInfo miLiteContaining = ReflectionTools.GetMethodInfo(() => LiteContaining<TypeDN, TypeDN>(null, 1)).GetGenericMethodDefinition();
        static List<Lite> LiteContaining<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToStr.Contains(subString) && !a.toStr.StartsWith(subString)).Select(a => a.ToLite<LT>()).Take(count).AsEnumerable().OrderBy(l => l.ToStr).Cast<Lite>().ToList();
        }

        public static List<Lite> RetriveAllLite(Type liteType, Type[] types)
        {
            List<Lite> result = new List<Lite>();

            foreach (var type in types ?? new[] { liteType })
            {
                MethodInfo mi = miAllLite.MakeGenericMethod(liteType, type);
                try
                {
                    List<Lite> part = (List<Lite>)mi.Invoke(null, null);
                    result.AddRange(part);
                }
                catch (TargetInvocationException te)
                {
                    throw te.InnerException;
                }
            }
            return result;
        }

        static MethodInfo miAllLite = ReflectionTools.GetMethodInfo(() => AllLite<TypeDN, TypeDN>()).GetGenericMethodDefinition();
        static List<Lite> AllLite<LT, RT>()
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Select(a => a.ToLite<LT>()).AsEnumerable().Cast<Lite>().ToList();
        }
    }
}
