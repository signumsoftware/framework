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
        StaticColumnFactory EntityColumn();
        QueryDescription GetDescription(object queryName);
        ResultTable ExecuteQuery(List<UserColumn> userColumns, List<Filter> filters, List<Order> orders, int? limit);
        int ExecuteQueryCount(List<Filter> filters);
        Lite ExecuteUniqueEntity(List<Filter> filters, List<Order> orders, UniqueType uniqueType);
        Expression Expression { get; } //Optional
        StaticColumnFactory[] StaticColumns { get; } 
    }

    public abstract class DynamicQuery<T> : IDynamicQuery
    {
        public StaticColumnFactory[] StaticColumns { get; private set; } 

        public abstract ResultTable ExecuteQuery(List<UserColumn> userColumns, List<Filter> filters, List<Order> orders, int? limit);
        public abstract int ExecuteQueryCount(List<Filter> filters);
        public abstract Lite ExecuteUniqueEntity(List<Filter> filters, List<Order> orders, UniqueType uniqueType);

        protected void InitializeColumns(Func<MemberInfo, Meta> getMeta)
        {
            this.StaticColumns = MemberEntryFactory.GenerateList<T>(MemberOptions.Properties | MemberOptions.Fields)
              .Select((e, i) => new StaticColumnFactory(i, e.MemberInfo, getMeta(e.MemberInfo), CreateGetter(e.MemberInfo))).ToArray();

            StaticColumns.Where(a => a.IsEntity).Single(Resources.EntityColumnNotFound, Resources.MoreThanOneEntityColumn); 
        }

        public QueryDescription GetDescription(object queryName)
        {
            return new QueryDescription
            {
                QueryName = queryName,
                StaticColumns = StaticColumns.Where(a => a.IsAllowed()).Select(a => a.BuildStaticColumn()).ToList()
            };
        }

      

        public DynamicQuery<T> Column<S>(Expression<Func<T, S>> column, Action<StaticColumnFactory> change)
        {
            MemberInfo member = ReflectionTools.GetMemberInfo(column);
            StaticColumnFactory col = StaticColumns.Single(a => a.Name == member.Name);
            change(col);

            return this;
        }

        public StaticColumnFactory EntityColumn()
        {
            return StaticColumns.Where(c => c.IsEntity).Single(Resources.ThereIsNoEntityColumn, Resources.ThereAreMoreThanOneEntityColumn);
        }

        public virtual Expression Expression
        {
            get { return null; }
        }

        protected static Delegate CreateGetter(MemberInfo memberInfo)
        {
            ParameterExpression pe = Expression.Parameter(typeof(Expandable<T>), "e");
            return Expression.Lambda(
                      Expression.PropertyOrField(
                         Expression.PropertyOrField(pe, "Value"),
                       memberInfo.Name), pe).Compile();
        }

        protected ResultTable ToQueryResult(Expandable<T>[] result, List<UserColumn> userColumns)
        {
            var dic = StaticColumns.ToDictionary(a => a.BuildStaticColumn());

            return new ResultTable(dic.Keys.ToArray(), (userColumns ?? new List<UserColumn>()).ToArray(), result.Length,
                c => c is StaticColumn ?
                    CreateValuesStaticColumn(dic[(StaticColumn)c], result) :
                    CreateValuesUserColumn((UserColumn)c, result));
        }

        static Array CreateValuesUserColumn(UserColumn column, Expandable<T>[] collection)
        {
            ParameterExpression pe = Expression.Parameter(typeof(Expandable<T>), "e");

            Delegate getter = Expression.Lambda(Expression.Convert(
                 Expression.ArrayIndex(Expression.Property(pe, "Expansions"), Expression.Constant(column.UserColumnIndex)), column.Type), pe).Compile();

            return (Array)miCreateValues.GenericInvoke(new[] { column.Type }, null, new object[] { getter, collection });
        }
       
        static Array CreateValuesStaticColumn(StaticColumnFactory column, Expandable<T>[] collection)
        {
            return (Array)miCreateValues.GenericInvoke(new[] { column.Type }, null, new object[] { column.Getter, collection });
        }

        static MethodInfo miCreateValues = ReflectionTools.GetMethodInfo(() => CreateValues<int>(null, null)).GetGenericMethodDefinition();
        static Array CreateValues<S>(Func<Expandable<T>, S> getter, Expandable<T>[] collection)
        {
            return collection.Select(getter).ToArray();
        }
    }


    public static class DynamicQuery
    {
        public static DynamicQuery<T> ToDynamic<T>(this IQueryable<T> query)
        {
            return new AutoDynamicQuery<T>(query); 
        }

        public static DynamicQuery<T> Manual<T>(Func<List<UserColumn>, List<Filter>, List<Order>, int?, IEnumerable<Expandable<T>>> execute)
        {
            return new ManualDynamicQuery<T>(execute); 
        }

        #region SelectExpandable
        public static IQueryable<Expandable<T>> SelectExpandable<T>(this IQueryable<T> query, List<UserColumn> userColumns)
        {
            if (userColumns == null || userColumns.Count == 0)
                return query.Select(a => new Expandable<T>(a));

            return query.Select(GetExpansions<T>(userColumns));
        }

        public static IEnumerable<Expandable<T>> SelectExpandable<T>(this IEnumerable<T> query, List<UserColumn> userColumns)
        {
            if (userColumns == null || userColumns.Count == 0)
                return query.Select(a => new Expandable<T>(a));

            return query.Select(GetExpansions<T>(userColumns).Compile());
        }

        static Expression<Func<T, Expandable<T>>> GetExpansions<T>(List<UserColumn> userColumns)
        {
            ParameterExpression param = Expression.Parameter(typeof(T), "p");

            NewArrayExpression newArray = Expression.NewArrayInit(typeof(object),
                userColumns.Select(uc =>
                {
                    Expression build = uc.Token.BuildExpression(param);
                    if (build.Type.IsValueType && !build.Type.IsNullable())
                        build = Expression.Convert(build, build.Type.Nullify());

                    return (Expression)Expression.Convert(build, typeof(object));
                }));

            var ctor = typeof(Expandable<T>).GetConstructor(new[] { typeof(T), typeof(object[]) });

            return Expression.Lambda<Func<T, Expandable<T>>>(
                Expression.New(ctor, param, newArray),
                param);
        }

        
	    #endregion

        #region Where
        static MethodInfo miContains = ReflectionTools.GetMethodInfo((string s) => s.Contains(s));
        static MethodInfo miStartsWith = ReflectionTools.GetMethodInfo((string s) => s.StartsWith(s));
        static MethodInfo miEndsWith = ReflectionTools.GetMethodInfo((string s) => s.EndsWith(s));
        static MethodInfo miLike = ReflectionTools.GetMethodInfo((string s) => s.Like(s));

        static Expression<Func<Expandable<T>, bool>> GetWhereExpression<T>(List<Filter> filters)
        {
            ParameterExpression pe = Expression.Parameter(typeof(Expandable<T>), "p");

            if (filters == null || filters.Count == 0)
                return null;

            Expression body = filters.Select(f => GetCondition(f, pe)).Aggregate((e1, e2) => Expression.And(e1, e2));

            return Expression.Lambda<Func<Expandable<T>, bool>>(body, pe);
        }

        static Expression GetCondition(Filter f, ParameterExpression pe)
        {
            Expression left = f.Token.BuildExpression(Expression.Property(pe, "Value")); 
            Expression right = Expression.Constant(f.Value, f.Token.Type);

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

        public static IQueryable<Expandable<T>> Where<T>(this IQueryable<Expandable<T>> query, params Filter[] filters)
        {
            return Where(query, filters.NotNull().ToList()); 
        }

        public static IQueryable<Expandable<T>> Where<T>(this IQueryable<Expandable<T>> query, List<Filter> filters)
        {
            var where = GetWhereExpression<T>(filters);

            if (where != null)
                return query.Where(where);
            return query; 
        }

        public static IEnumerable<Expandable<T>> Where<T>(this IEnumerable<Expandable<T>> sequence, params Filter[] filters)
        {
            return Where(sequence, filters.NotNull().ToList()); 
        }

        public static IEnumerable<Expandable<T>> Where<T>(this IEnumerable<Expandable<T>> sequence, List<Filter> filters)
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

        public static IQueryable<Expandable<T>> OrderBy<T>(this IQueryable<Expandable<T>> query, List<Order> orders)
        {
            if (orders == null || orders.Count == 0)
                return query;

            IOrderedQueryable<Expandable<T>> result = query.OrderBy(orders.First());

            foreach (var order in orders.Skip(1))
            {
                result = result.ThenBy(order);
            }

            return result;
        }

        static IOrderedQueryable<Expandable<T>> OrderBy<T>(this IQueryable<Expandable<T>> query, Order order)
        {
            LambdaExpression lambda = CreateLambda<T>(order);

            MethodInfo mi = (order.OrderType == OrderType.Ascending? miOrderByQueryable: miOrderByQueryableDescending).MakeGenericMethod(lambda.Type.GetGenericArguments());

            return (IOrderedQueryable<Expandable<T>>)query.Provider.CreateQuery<Expandable<T>>(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
        }

        static IOrderedQueryable<Expandable<T>> ThenBy<T>(this IOrderedQueryable<Expandable<T>> query, Order order)
        {
            LambdaExpression lambda = CreateLambda<T>(order);

            MethodInfo mi = (order.OrderType == OrderType.Ascending ? miThenByQueryable : miThenByQueryableDescending).MakeGenericMethod(lambda.Type.GetGenericArguments());

            return (IOrderedQueryable<Expandable<T>>)query.Provider.CreateQuery<Expandable<T>>(Expression.Call(null, mi, new Expression[] { query.Expression, Expression.Quote(lambda) }));
        }

        static MethodInfo miOrderByEnumerable = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByEnumerable = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id).ThenBy(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miOrderByEnumerableDescending = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().ToList().OrderByDescending(t => t.Id)).GetGenericMethodDefinition();
        static MethodInfo miThenByEnumerableDescending = ReflectionTools.GetMethodInfo(() => Database.Query<TypeDN>().ToList().OrderBy(t => t.Id).ThenByDescending(t => t.Id)).GetGenericMethodDefinition();

        public static IEnumerable<Expandable<T>> OrderBy<T>(this IEnumerable<Expandable<T>> collection, List<Order> orders)
        {
            if (orders == null || orders.Count == 0)
                return collection;

            IOrderedEnumerable<Expandable<T>> result = collection.OrderBy(orders.First());

            foreach (var order in orders.Skip(1))
            {
                result = result.ThenBy(order);
            }

            return result;
        }

        static IOrderedEnumerable<Expandable<T>> OrderBy<T>(this IEnumerable<Expandable<T>> collection, Order order)
        {
            LambdaExpression lambda = CreateLambda<T>(order);

            MethodInfo mi = order.OrderType == OrderType.Ascending ? miOrderByEnumerable : miOrderByEnumerableDescending;

            return (IOrderedEnumerable<Expandable<T>>)mi.GenericInvoke(lambda.Type.GetGenericArguments(), null, new object[] { collection, lambda.Compile() });
        }

        static IOrderedEnumerable<Expandable<T>> ThenBy<T>(this IOrderedEnumerable<Expandable<T>> collection, Order order)
        {
            LambdaExpression lambda = CreateLambda<T>(order);

            MethodInfo mi = order.OrderType == OrderType.Ascending ? miThenByEnumerable : miThenByEnumerableDescending;

            return (IOrderedEnumerable<Expandable<T>>)mi.GenericInvoke(lambda.Type.GetGenericArguments(), null, new object[] { collection, lambda.Compile() });
        }

        static LambdaExpression CreateLambda<T>(Order order)
        {
            ParameterExpression a = Expression.Parameter(typeof(Expandable<T>), "a");

            return Expression.Lambda(order.Token.BuildExpression(Expression.Property(a, "Value")), a);
        }

        #endregion

        #region SelectEntity
        static Expression<Func<Expandable<T>, Lite>> GetSelectEntityExpression<T>()
        {
            ParameterExpression e = Expression.Parameter(typeof(Expandable<T>), "e");

            return Expression.Lambda<Func<Expandable<T>, Lite>>(
                Expression.Convert(
                    Expression.Property(
                        Expression.Property(e, "Value"),
                     StaticColumn.Entity),
                 typeof(Lite)), e);
        }

        public static IQueryable<Lite> SelectEntity<T>(this IQueryable<Expandable<T>> query)
        {
            Expression<Func<Expandable<T>, Lite>> select = GetSelectEntityExpression<T>();

            return query.Select(select);
        }

        public static IEnumerable<Lite> SelectEntity<T>(this IEnumerable<Expandable<T>> query)
        {
            Func<Expandable<T>, Lite> select = GetSelectEntityExpression<T>().Compile();

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
                case UniqueType.Only: return query.Only();
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
                case UniqueType.Only: return collection.Only();
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
