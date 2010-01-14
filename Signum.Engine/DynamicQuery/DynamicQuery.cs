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
using Signum.Entities.Reflection;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.DynamicQuery
{
    public interface IDynamicQuery
    {
        Type EntityCleanType();
        QueryDescription GetDescription();
        ResultTable ExecuteQuery(List<Filter> filters, List<Order> orders, int? limit);
        int ExecuteQueryCount(List<Filter> filters);
        Lite ExecuteUniqueEntity(List<Filter> filters, List<Order> orders, UniqueType uniqueType);
        string GetErrors();
        Expression Expression { get; } //Optional
        }

    public abstract class DynamicQuery<T> : IDynamicQuery
    {
        protected Column[] columns; 

        public abstract ResultTable ExecuteQuery(List<Filter> filters, List<Order> orders, int? limit);
        public abstract int ExecuteQueryCount(List<Filter> filters);
        public abstract Lite ExecuteUniqueEntity(List<Filter> filters, List<Order> orders, UniqueType uniqueType);

        public string GetErrors()
        {
            int count = columns.Where(c => c.IsEntity).Count();

            string errors = count == 0 ? Resources.ThereIsNoEntityColumn :
                            count > 1 ? Resources.ThereAreMoreThanOneEntityColumn : null;

            return errors.Add(columns.Where(c => typeof(ModifiableEntity).IsAssignableFrom(c.Type)).ToString(c => c.Name, ", "), ", ");
        }

        public QueryDescription GetDescription()
        {
            return new QueryDescription { Columns = columns.Where(DynamicQuery.ColumnIsAllowed).ToList() };
        }
       
        protected ResultTable ToQueryResult(IEnumerable<T> result)
        {
            return ResultTable.Create(result, columns.Where(DynamicQuery.ColumnIsAllowed));
        }

        public DynamicQuery<T> ChangeColumn<S>(Expression<Func<T, S>> column, Action<Column> change)
        {
            MemberInfo member = ReflectionTools.GetMemberInfo(column);
            Column col = columns.Single(a => a.Name == member.Name);
            change(col);

            return this;
        }

        public Type EntityCleanType()
        {
            Type type = columns.Where(c => c.IsEntity).Single(Resources.ThereIsNoEntityColumn, Resources.ThereAreMoreThanOneEntityColumn).Type;

            return Reflector.ExtractLite(type).ThrowIfNullC(Resources.EntityColumnIsNotALite);
        }

        public virtual Expression Expression
        {
            get { return null; }
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

        public static DynamicQuery<T> Manual<T>(Func<List<Filter>, List<Order>, int?, IEnumerable<T>> execute)
        {
            return new ManualDynamicQuery<T>(execute); 
        }

        #region WhereExpression
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

        #endregion

        #region OrederBy

        static MethodInfo miOrderByQueryable = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByQueryable = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id).ThenBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miOrderByQueryableDescending = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderByDescending(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByQueryableDescending = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().OrderBy(t => t.Id).ThenByDescending(t => t.Id)).GetGenericMethodDefinition();

        public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, List<Order> orders)
        {
            if (orders == null || orders.Count == 0)
                return query;

            IOrderedQueryable<T> result = query.OrderBy(orders.First());

            foreach (var order in orders.Skip(1))
            {
                result = result.ThenBy(order);
            }

            return result;
        }
  
        static IOrderedQueryable<T> OrderBy<T>(this IQueryable<T> query, Order order)
        {
            LambdaExpression lambda = CreateLambda<T>(order);

            MethodInfo mi = (order.OrderType == OrderType.Ascending? miOrderByQueryable: miOrderByQueryableDescending).MakeGenericMethod(lambda.Type.GetGenericArguments());

            return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
        }

        static IOrderedQueryable<T> ThenBy<T>(this IOrderedQueryable<T> query, Order order)
        {
            LambdaExpression lambda = CreateLambda<T>(order);

            MethodInfo mi = (order.OrderType == OrderType.Ascending ? miThenByQueryable : miThenByQueryableDescending).MakeGenericMethod(lambda.Type.GetGenericArguments());

            return (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
        }

        static MethodInfo miOrderByEnumerable = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByEnumerable = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id).ThenBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miOrderByEnumerableDescending = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().ToList().OrderByDescending(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByEnumerableDescending = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id).ThenByDescending(t => t.Id)).GetGenericMethodDefinition();
   
        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> collection, List<Order> orders)
        {
            if (orders == null || orders.Count == 0)
                return collection;

            IOrderedEnumerable<T> result = collection.OrderBy(orders.First());

            foreach (var order in orders.Skip(1))
            {
                result = result.ThenBy(order);
            }

            return result;
        }

        static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> collection, Order order)
        {
            LambdaExpression lambda = CreateLambda<T>(order);

            MethodInfo mi = (order.OrderType == OrderType.Ascending ? miOrderByEnumerable : miOrderByEnumerableDescending).MakeGenericMethod(lambda.Type.GetGenericArguments());

            return (IOrderedEnumerable<T>)mi.Invoke(null, new object[] { collection, lambda.Compile() });
        }

        static IOrderedEnumerable<T> ThenBy<T>(this IOrderedEnumerable<T> collection, Order order)
        {
            LambdaExpression lambda = CreateLambda<T>(order);

            MethodInfo mi = (order.OrderType == OrderType.Ascending ? miThenByEnumerable : miThenByEnumerableDescending).MakeGenericMethod(lambda.Type.GetGenericArguments());

            return (IOrderedEnumerable<T>)mi.Invoke(null, new object[] { collection, lambda.Compile() });
        }

        static LambdaExpression CreateLambda<T>(Order order)
        {
            ParameterExpression a = Expression.Parameter(typeof(T), "a");

            PropertyInfo property = typeof(T).GetProperty(order.ColumnName)
               .ThrowIfNullC(Resources.TheProperty0ForType1IsnotFound.Formato(order.ColumnName, typeof(T).TypeName()));

            return Expression.Lambda(Expression.Property(a, property), a);
        }

        #endregion

        #region SelectEntity
        private static Expression<Func<T, Lite>> GetSelectEntityExpression<T>()
        {
            ParameterExpression e = Expression.Parameter(typeof(T), "e");

            return Expression.Lambda<Func<T, Lite>>(Expression.Convert(Expression.Property(e, Column.Entity), typeof(Lite)), e);
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
        #endregion

        #region Unique
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

        #endregion

        #region TryTake
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
        #endregion

        public static Dictionary<string, Meta> QueryMetadata(IQueryable query)
        {
            return MetadataVisitor.GatherMetadata(query.Expression); 
        }
    }
}
