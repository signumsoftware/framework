using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Signum.Engine.Properties;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.DynamicQuery
{
    public interface IDynamicQuery
    {
        QueryDescription Description { get; }
        QueryResult ExecuteQuery(List<Filter> filters, int? limit);
    }

    public class DynamicQuery<T> : IDynamicQuery
    {
        IQueryable<T> query;
        Func<List<Filter>, int?, IEnumerable<T>> execute;

        QueryDescription description;
        List<MemberEntry<T>> members; 

        public DynamicQuery(IQueryable<T> query)
        {
            if (query == null)
                throw new ArgumentNullException("query"); 

            this.query = query;

            Initialize();
        }

        public DynamicQuery(Func<List<Filter>, int?, IEnumerable<T>> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            this.execute = execute;

            Initialize();
        }

        void Initialize()
        {
            members = MemberEntryFactory.GenerateList<T>();

            description =  new QueryDescription
            {
                Columns = members.Cast<IMemberEntry>().Select(e => new Column(e.MemberInfo, new Attribute[0])).ToList()
            };
        }

        public QueryDescription Description
        {
            get { return description; }
        }

        public QueryResult ExecuteQuery(List<Filter> filters, int? limit)
        {
            if (execute != null)
                return ToQueryResult(execute(filters, limit));
            else
            {
                IQueryable<T> result = query.WhereFilters(filters); 

                if (limit != null)
                    result = result.Take(limit.Value);

                return ToQueryResult(result);
            }

        }

        QueryResult ToQueryResult(IEnumerable<T> result)
        {
            return new QueryResult
            {
                Columns = description.Columns,
                Data = result.Select(e => members.Select(d => d.Getter(e)).ToArray()).ToArray()
            };
        }

        public DynamicQuery<T> ChangeColumn(Expression<Func<T, object>> column, Action<Column> change)
        {
            MemberInfo member = ReflectionTools.GetMemberInfo(column);
            Column col = description.Columns.Single(a => a.Name == member.Name);
            change(col);

            return this;
        }
    }


    public static class DynamicQuery
    {
        public static DynamicQuery<T> ToDynamic<T>(this IQueryable<T> query)
        {
            return new DynamicQuery<T>(query); 
        }

        public static DynamicQuery<T> Manual<T>(Func<List<Filter>, int?, IEnumerable<T>> execute)
        {
            return new DynamicQuery<T>(execute); 
        }

        static MethodInfo miContains = ReflectionTools.GetMethodInfo<string>(s => s.Contains(s));
        static MethodInfo miStartsWith = ReflectionTools.GetMethodInfo<string>(s => s.StartsWith(s));
        static MethodInfo miEndsWith = ReflectionTools.GetMethodInfo<string>(s => s.EndsWith(s));
        static MethodInfo miLike = ReflectionTools.GetMethodInfo<string>(s => s.Like(s));

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
            PropertyInfo pi = type.GetProperty(f.Name)
                .ThrowIfNullC(Resources.TheProperty0ForType1IsnotFound.Formato(f.Name, type.TypeName()));

            Expression left = Expression.MakeMemberAccess(pe, pi);
            Expression right = Expression.Constant(f.Value, f.Type);

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

        public static IQueryable<T> WhereFilters<T>(this IQueryable<T> query, params Filter[] filters)
        {
            return WhereFilters(query, filters.NotNull().ToList()); 
        }

        public static IQueryable<T> WhereFilters<T>(this IQueryable<T> query, List<Filter> filters)
        {
            var where = GetWhereExpression<T>(filters);

            if (where != null)
                return query.Where(where);
            return query; 
        }

        public static IEnumerable<T> WhereFilters<T>(this IEnumerable<T> sequence, params Filter[] filters)
        {
            return WhereFilters(sequence, filters.NotNull().ToList()); 
        }

        public static IEnumerable<T> WhereFilters<T>(this IEnumerable<T> sequence, List<Filter> filters)
        {
            var where = GetWhereExpression<T>(filters);

            if (where != null)
                return sequence.Where(where.Compile());
            return sequence;
        }

        public static IQueryable<T> TryTake<T>(this IQueryable<T> query, int? num)
        {
            if (num.HasValue)
                return query.Take(num.Value);
            return query;
        }

        public static IEnumerable<T> TryTake<T>(this IEnumerable<T> sequence, int? num)
        {
            if (num.HasValue)
                return sequence.Take(num.Value);
            return sequence;
        }
    }
}
