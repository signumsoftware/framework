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
using Signum.Engine.Linq;
using Signum.Entities;
using System.Diagnostics;

namespace Signum.Engine.DynamicQuery
{
    public interface IDynamicQuery
    {
        QueryDescription GetDescription();
        QueryResult ExecuteQuery(List<Filter> filters, int? limit);
        int ExecuteQueryCount(List<Filter> filters);
        Lite ExecuteUniqueEntity(List<Filter> filters, UniqueType uniqueType);
        string GetErrors();
    }

    public class AutoDynamicQuery<T>: DynamicQuery<T>
    {
        IQueryable<T> query;
        Dictionary<string, Meta> metas;

        public AutoDynamicQuery(IQueryable<T> query)
        {
            if (query == null)
                throw new ArgumentNullException("query"); 

            this.query = query;

            metas = DynamicQuery.QueryMetadata(query); 
            
            members = MemberEntryFactory.GenerateList<T>();

            columns = members.Cast<IMemberEntry>().Select(e =>
                    new Column(e.MemberInfo, metas[e.MemberInfo.Name])).ToList();
        }

        public override QueryResult ExecuteQuery(List<Filter> filters, int? limit)
        {
            IQueryable<T> result = query.WhereFilters(filters);

            if (limit != null)
                result = result.Take(limit.Value);

            return ToQueryResult(result);
        }

        public override int ExecuteQueryCount(List<Filter> filters)
        {
            return query.WhereFilters(filters).Count();
        }

        public override Lite ExecuteUniqueEntity(List<Filter> filters, UniqueType uniqueType)
        {
            return query.WhereFilters(filters).SelectEntity().Unique(uniqueType); 
        }
    }

    public class ManualDynamicQuery<T>: DynamicQuery<T>
    {
        Func<List<Filter>, int?, IEnumerable<T>> execute;

        public ManualDynamicQuery(Func<List<Filter>, int?, IEnumerable<T>> execute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            this.execute = execute;
         
            this.members = MemberEntryFactory.GenerateList<T>();

            this.columns = members.Cast<IMemberEntry>().Select(e =>
                    new Column(e.MemberInfo, null)).ToList();
        }

        public override QueryResult ExecuteQuery(List<Filter> filters, int? limit)
        {
            return ToQueryResult(execute(filters, limit));
        }

        public override int ExecuteQueryCount(List<Filter> filters)
        {
            return execute(filters, null).Count();
        }

        public override Lite ExecuteUniqueEntity(List<Filter> filters, UniqueType uniqueType)
        {
            return execute(filters, 1).SelectEntity().Unique(uniqueType);
        }
    }

    public abstract class DynamicQuery<T> : IDynamicQuery
    {
        protected List<Column> columns; 
        protected List<MemberEntry<T>> members;

        public abstract QueryResult ExecuteQuery(List<Filter> filters, int? limit);
        public abstract int ExecuteQueryCount(List<Filter> filters);
        public abstract Lite ExecuteUniqueEntity(List<Filter> filters, UniqueType uniqueType);

        public string GetErrors()
        {
            return columns.Where(c => typeof(ModifiableEntity).IsAssignableFrom(c.Type)).ToString(c => c.Name, ", ");
        }

        public QueryDescription GetDescription()
        {
            return new QueryDescription { Columns = columns.Where(DynamicQuery.ColumnIsAllowed).ToList() };
        }
       
        protected QueryResult ToQueryResult(IEnumerable<T> result)
        {
            bool[] allowed = columns.Select(c=>DynamicQuery.ColumnIsAllowed(c)).ToArray();

            List<MemberEntry<T>> allowedMembers= members.Where((m, i) => allowed[i]).ToList();

            return new QueryResult
            {
                Columns = columns.Where((c,i) => allowed[i]).ToList(),
                Data = result.Select(e => allowedMembers.Select(d => d.Getter(e)).ToArray()).ToArray()
            };
        }

        public DynamicQuery<T> ChangeColumn<S>(Expression<Func<T, S>> column, Action<Column> change)
        {
            MemberInfo member = ReflectionTools.GetMemberInfo(column);
            Column col = columns.Single(a => a.Name == member.Name);
            change(col);

            return this;
        }
    }


    public static class DynamicQuery
    {
        public static event Func<Meta, bool> IsAllowed;

        internal static bool ColumnIsAllowed(Column column)
        {
            if (IsAllowed != null)
                return IsAllowed(column.Meta);
            return true;    
        }

        public static DynamicQuery<T> ToDynamic<T>(this IQueryable<T> query)
        {
            return new AutoDynamicQuery<T>(query); 
        }

        public static DynamicQuery<T> Manual<T>(Func<List<Filter>, int?, IEnumerable<T>> execute)
        {
            return new ManualDynamicQuery<T>(execute); 
        }

        static MethodInfo miContains = ReflectionTools.GetMethodInfo((string s) => s.Contains(s));
        static MethodInfo miStartsWith = ReflectionTools.GetMethodInfo((string s) => s.StartsWith(s));
        static MethodInfo miEndsWith = ReflectionTools.GetMethodInfo((string s) => s.EndsWith(s));
        static MethodInfo miLike = ReflectionTools.GetMethodInfo((string s) => s.Like(s));

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

        public static IQueryable<Lite> SelectEntity<T>(this IQueryable<T> query)
        {
            Expression<Func<T, Lite>> select = GetSelectEntityExpression<T>();

            return query.Select(select);
        }

        public static IEnumerable<Lite> SelectEntity<T>(this IEnumerable<T> query)
        {
            Func<T, Lite> select = GetSelectEntityExpression<T>().Compile();

            return query.Select(select);
        }

        public static T Unique<T>(this IQueryable<T> query, UniqueType uniqueType)
        {
            switch (uniqueType)
            {
                case UniqueType.First: return query.First();
                case UniqueType.FirstOrDefault: return query.FirstOrDefault();
                case UniqueType.Single: return query.Single();
                case UniqueType.SingleOrDefault: return query.SingleOrDefault();
                case UniqueType.SingleOrMany: return query.SingleOrMany();
                default: throw new InvalidOperationException();
            }
        }

        public static T Unique<T>(this IEnumerable<T> collection, UniqueType uniqueType)
        {
            switch (uniqueType)
            {
                case UniqueType.First: return collection.First();
                case UniqueType.FirstOrDefault: return collection.FirstOrDefault();
                case UniqueType.Single: return collection.Single();
                case UniqueType.SingleOrDefault: return collection.SingleOrDefault();
                case UniqueType.SingleOrMany: return collection.SingleOrMany();
                default: throw new InvalidOperationException();
            }
        }

        private static Expression<Func<T, Lite>> GetSelectEntityExpression<T>()
        {
            ParameterExpression e = Expression.Parameter(typeof(T), "e");

            return Expression.Lambda<Func<T, Lite>>(Expression.Convert(Expression.Property(e, Column.Entity), typeof(Lite)), e);
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

        public static Dictionary<string, Meta> QueryMetadata(IQueryable query)
        {
            return MetadataVisitor.GatherMetadata(query.Expression); 
        }
    }
}
