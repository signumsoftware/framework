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
    public static class DynamicQueryUtils
    {
        static MethodInfo miContains = ReflectionTools.GetMethodInfo<string>(s => s.Contains(s));
        static MethodInfo miStartsWith = ReflectionTools.GetMethodInfo<string>(s => s.StartsWith(s));
        static MethodInfo miEndsWith = ReflectionTools.GetMethodInfo<string>(s => s.EndsWith(s));
        static MethodInfo miLike = ReflectionTools.GetMethodInfo<string>(s => s.Like(s));

        static Dictionary<Type, IEnumerable> memberListCache = new Dictionary<Type, IEnumerable>();

        private static IEnumerable GetDescription(Type type)
        {
            lock (memberListCache)
            {
                return memberListCache.GetOrCreate(type,
                    () => MemberEntryFactory.GenerateIList(type));
            }
        }
        public static Expression<Func<T, bool>> GetWhereExpression<T>(List<Filter> filters)
        {
            Type type = typeof(T);

            ParameterExpression pe = Expression.Parameter(type, type.Name.Substring(0, 1).ToUpper());

            if (filters == null || filters.Count == 0)
                return null;

            Expression body = filters.Select(f => GetCondition(f, pe, type)).Aggregate((e1, e2) => Expression.And(e1, e2));

            return Expression.Lambda<Func<T, bool>>(body, pe);
        }

        static Expression GetCondition(Filter f, ParameterExpression pe, Type type)
        {
            PropertyInfo pi = type.GetProperty(f.Column.Name)
                .ThrowIfNullC(Resources.TheProperty0ForType1IsnotFound.Formato(f.Column.Name, type.TypeName()));

            Expression left = Expression.MakeMemberAccess(pe, pi);
            Expression right = Expression.Constant(f.Value, f.Column.Type);

            switch (f.Operation)
            {
                case FilterOperation.EqualTo:
                    return Expression.Equal(left, right);
                case FilterOperation.DistinctTo:
                    return Expression.NotEqual(left, right);
                case FilterOperation.GreaterThan:
                    return Expression.GreaterThan(left, right);
                case FilterOperation.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(left, right);
                case FilterOperation.LessThan:
                    return Expression.LessThan(left, right);
                case FilterOperation.LessThanOrEqual:
                    return Expression.LessThanOrEqual(left, right);
                case FilterOperation.Contains:
                    return Expression.Call(left, miContains, right);
                case FilterOperation.StartsWith:
                    return Expression.Call(left, miStartsWith, right);
                case FilterOperation.EndsWith:
                    return Expression.Call(left, miEndsWith, right);
                case FilterOperation.Like:
                    return Expression.Call(miLike, left, right);
                default:
                    throw new NotSupportedException();
            }
        }

        public static QueryDescription GetViewDescription(Type type)
        {
            return new QueryDescription
            {
                Columns = GetDescription(type).Cast<IMemberEntry>().Select(e => new Column(e.MemberInfo)).ToList()
            };
        }

        public static QueryResult ToView<T>(this IEnumerable<T> list)
        {
            List<MemberEntry<T>> descrption = (List<MemberEntry<T>>)GetDescription(typeof(T));

            return new QueryResult
            {
                Columns = descrption.Select(d => new Column(d.MemberInfo)).ToList(),
                Data = list.Select(e => descrption.Select(d => d.Getter(e)).ToArray()).ToArray()
            };
        }
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

        static MethodInfo miLazyStarting = typeof(DynamicQueryUtils).GetMethod("LazyStarting", BindingFlags.NonPublic | BindingFlags.Static);
        static List<Lazy> LazyStarting<LT, RT>(string subString, int count)
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Where(a => a.ToStr.StartsWith(subString)).Select(a => a.ToLazy<LT>()).Take(count).AsEnumerable().Cast<Lazy>().ToList();
        }

        static MethodInfo miLazyContaining = typeof(DynamicQueryUtils).GetMethod("LazyContaining", BindingFlags.NonPublic | BindingFlags.Static);
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

        static MethodInfo miAllLazy = typeof(DynamicQueryUtils).GetMethod("AllLazy", BindingFlags.NonPublic | BindingFlags.Static);
        static List<Lazy> AllLazy<LT, RT>()
            where LT : class, IIdentifiable
            where RT : IdentifiableEntity, LT
        {
            return Database.Query<RT>().Select(a => a.ToLazy<LT>()).AsEnumerable().Cast<Lazy>().ToList();
        }

        public static QueryDescription ViewDescription(IQueryable q)
        {
            Type parameter = ExtractQueryType(q);

            return DynamicQueryUtils.GetViewDescription(parameter);
        }

        public static Type ExtractQueryType(IQueryable q)
        {
            Type parameter = q.GetType().GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryable<>))
                .GetGenericArguments()[0];
            return parameter;
        }

        internal static MethodInfo miExecuteQueryGeneric = ReflectionTools.GetMethodInfo<int>(a => DynamicQueryUtils.ExecuteQueryGeneric<int>(null, null, null)).GetGenericMethodDefinition();
        public static QueryResult ExecuteQueryGeneric<T>(IQueryable<T> query, List<Filter> filter, int? limit)
        {
            var f = DynamicQueryUtils.GetWhereExpression<T>(filter);
            if (f != null)
                query = query.Where(f);

            if (limit != null)
                query = query.Take(limit.Value);

            return query.ToView();
        }


    }
}
