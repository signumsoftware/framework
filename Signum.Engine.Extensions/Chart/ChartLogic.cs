using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Chart;
using Signum.Engine.DynamicQuery;
using Signum.Utilities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.Basics;
using Signum.Entities.Reports;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;

namespace Signum.Engine.Extensions.Chart
{
    public static class ChartLogic
    {
        static MethodInfo mi = ReflectionTools.GetMethodInfo(() => DynamicQuery.DynamicQuery.Where<int>(null, new List<Filter>())).GetGenericMethodDefinition();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                LiteFilterValueConverter.TryParseLite = TypeLogic.TryParseLite;

                sb.Include<UserChartDN>();

                dqm[typeof(UserChartDN)] = (from uq in Database.Query<UserChartDN>()
                                            select new
                                            {
                                                Entity = uq.ToLite(),
                                                Query = uq.Query.ToLite(),
                                                uq.Id,
                                                uq.DisplayName,
                                                Filters = uq.Filters.Count,
                                                uq.ChartType,
                                                uq.ChartResultType,
                                                uq.GroupResults,
                                            }).ToDynamic().Column(a => a.Query, c => c.Visible = false);

                sb.Schema.EntityEvents<UserChartDN>().Retrieved += new EntityEventHandler<UserChartDN>(UserQueryLogic_Retrieved);
            }
        }

        static void UserQueryLogic_Retrieved(UserChartDN userQuery, bool isRoot)
        {
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            foreach (var f in userQuery.Filters)
                f.PostRetrieving(description);

            if (userQuery.FirstDimension != null)
                userQuery.FirstDimension.PostRetrieving(description);

            if (userQuery.SecondDimension != null)
                userQuery.SecondDimension.PostRetrieving(description);

            if (userQuery.FirstValue != null)
                userQuery.FirstValue.PostRetrieving(description);

            if (userQuery.SecondValue != null)
                userQuery.SecondValue.PostRetrieving(description);
        }

        public static ResultTable ExecuteChart(ChartRequest request)
        {
            IDynamicQuery dq = DynamicQueryManager.Current[request.QueryName];

            if (dq.GetType().FollowC(t => t.BaseType).Any(t => t.IsInstantiationOf(typeof(DynamicQuery<>))))
            {
                switch (request.ChartResultType)
                {
                    case ChartResultType.TypeValue:
                        return (ResultTable)miExecuteChartAutoTypeValue.GenericInvoke(
                            new[] { dq.GetType().GetGenericArguments()[0], request.FirstDimension.Token.Type.Nullify(), request.FirstValue.Type.Nullify() }, null,
                            new object[] { request, dq });
                    case ChartResultType.TypeTypeValue:
                        return (ResultTable)miExecuteChartAutoTypeTypeValue.GenericInvoke(
                          new[] { dq.GetType().GetGenericArguments()[0], request.FirstDimension.Type.Nullify(), request.SecondDimension.Type.Nullify(), request.FirstValue.Type.Nullify() }, null,
                          new object[] { request, dq });
                    case ChartResultType.Points:
                        return (ResultTable)miExecuteChartPoints.GenericInvoke(
                          new[] { dq.GetType().GetGenericArguments()[0], request.FirstDimension.Type.Nullify(), request.SecondDimension.Type.Nullify(), request.FirstValue.Type.Nullify() }, null,
                            new object[] { request, dq });
                    case ChartResultType.Bubbles:
                        return (ResultTable)miExecuteChartBubbles.GenericInvoke(
                        new[] { dq.GetType().GetGenericArguments()[0], request.FirstDimension.Type.Nullify(), request.SecondDimension.Type.Nullify(), request.FirstValue.Type.Nullify(), request.SecondValue.Type.Nullify() }, null,
                          new object[] { request, dq });
                }
            }

            throw new NotImplementedException(); 
        }


        static MethodInfo miExecuteChartAutoTypeValue = ReflectionTools.GetMethodInfo(() => ExecuteChartAutoTypeValue<int, int, int>(null, null)).GetGenericMethodDefinition();
        static ResultTable ExecuteChartAutoTypeValue<T, T1, V1>(ChartRequest request, DynamicQuery<T> dq)
        {
            IEnumerable<T> collection;
            if (dq is AutoDynamicQuery<T>)
            {
                IQueryable<T> query = ((AutoDynamicQuery<T>)dq).Query;

                if (request.Filters != null && request.Filters.Count != 0)
                    query = DynamicQuery.DynamicQuery.Where(query, request.Filters);

                collection = query;
            }
            else
            {
                collection = ((ManualDynamicQuery<T>)dq).Execute(new QueryRequest { QueryName = request.QueryName, Filters = request.Filters }).Select(a => a.Value); 
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "e");

            ConstructorInfo ci = typeof(KeyValuePair<T1, V1>).GetConstructor(new[] { typeof(T1), typeof(V1) });
            
            List<KeyValuePair<T1, V1>> dic;
            if (!request.GroupResults)
            {
                var selector = Expression.Lambda<Func<T, KeyValuePair<T1, V1>>>(
                    Expression.New(ci,
                        request.FirstDimension.BuildExpression(pe).Nullify(),
                        request.FirstValue.BuildExpression(pe).Nullify()), pe);

                dic = (collection is IQueryable ?
                    ((IQueryable<T>)collection).Select(selector) :
                    collection.Select(selector.Compile())).AssertToDictionary();
            }
            else
            {
                ParameterExpression p = Expression.Parameter(typeof(T1), "p");
                ParameterExpression ge = Expression.Parameter(typeof(IEnumerable<T>), "g");

                var keySelector = Expression.Lambda<Func<T, T1>>(request.FirstDimension.BuildExpression(pe).Nullify(), pe);

                var selector = Expression.Lambda<Func<T1, IEnumerable<T>, KeyValuePair<T1, V1>>>(Expression.New(ci,
                          p, request.FirstValue.BuildExpression(ge).Nullify()), p, ge);

                dic = (collection is IQueryable ?
                   ((IQueryable<T>)collection).GroupBy(keySelector, selector) :
                   collection.GroupBy(keySelector.Compile(), selector.Compile())).AssertToDictionary();
            }

            return new ResultTable(
                new ColumnValues(GetUserColumn(0, request.FirstDimension), dic.Select(a=>a.Key).ToArray()),
                new ColumnValues(GetUserColumn(1, request.FirstValue), dic.Select(a => a.Value).ToArray()));   
        }

        static MethodInfo miExecuteChartAutoTypeTypeValue = ReflectionTools.GetMethodInfo(() => ExecuteChartAutoTypeTypeValue<int, int, int, int>(null, null)).GetGenericMethodDefinition();
        static ResultTable ExecuteChartAutoTypeTypeValue<T, T1, T2, V1>(ChartRequest request, DynamicQuery<T> dq)
        {
            IEnumerable<T> collection;
            if (dq is AutoDynamicQuery<T>)
            {
                IQueryable<T> query = ((AutoDynamicQuery<T>)dq).Query;

                if (request.Filters != null && request.Filters.Count != 0)
                    query = DynamicQuery.DynamicQuery.Where(query, request.Filters);

                collection = query;
            }
            else
            {
                collection = ((ManualDynamicQuery<T>)dq).Execute(new QueryRequest { QueryName = request.QueryName, Filters = request.Filters }).Select(a => a.Value);
            }


            ParameterExpression pe = Expression.Parameter(typeof(T), "e");

            ConstructorInfo ci = typeof(KeyValuePair<Tuple<T1, T2>, V1>).GetConstructor(new[] { typeof(Tuple<T1, T2>), typeof(V1) });
            ConstructorInfo cit = typeof(Tuple<T1, T2>).GetConstructor(new[] { typeof(T1), typeof(T2) });

            List<KeyValuePair<Tuple<T1, T2>, V1>> dic; 

            if (!request.GroupResults)
            {
                var selector = Expression.Lambda<Func<T, KeyValuePair<Tuple<T1, T2>, V1>>>(
                    Expression.New(ci,
                        Expression.New(cit,
                            request.FirstDimension.BuildExpression(pe).Nullify(),
                            request.SecondDimension.BuildExpression(pe).Nullify()),
                        request.FirstValue.BuildExpression(pe).Nullify()), pe);

                dic = (collection is IQueryable ?
                    ((IQueryable<T>)collection).Select(selector) :
                    collection.Select(selector.Compile())).AssertToDictionary();
            }
            else
            {
                ParameterExpression p = Expression.Parameter(typeof(Tuple<T1, T2>), "p");
                ParameterExpression ge = Expression.Parameter(typeof(IEnumerable<T>), "g");

                var keySelector = Expression.Lambda<Func<T, Tuple<T1, T2>>>(Expression.New(cit,
                            request.FirstDimension.BuildExpression(pe).Nullify(),
                            request.SecondDimension.BuildExpression(pe).Nullify()), pe);

                var selector = Expression.Lambda<Func<Tuple<T1, T2>, IEnumerable<T>, KeyValuePair<Tuple<T1, T2>, V1>>>(Expression.New(ci,
                          p, request.FirstValue.BuildExpression(ge).Nullify()), p, ge);

                dic = (collection is IQueryable ?
                    ((IQueryable<T>)collection).GroupBy(keySelector, selector) :
                    collection.GroupBy(keySelector.Compile(), selector.Compile())).AssertToDictionary(); 
            }

            return new ResultTable(
               new ColumnValues(GetUserColumn(0, request.FirstDimension), dic.Select(a => a.Key.First).ToArray()),
               new ColumnValues(GetUserColumn(1, request.SecondDimension), dic.Select(a => a.Key.Second).ToArray()),
               new ColumnValues(GetUserColumn(2, request.FirstValue), dic.Select(a => a.Value).ToArray()));   
        }

        static MethodInfo miExecuteChartPoints = ReflectionTools.GetMethodInfo(() => ExecuteChartPoints<int, int, int, int>(null, null)).GetGenericMethodDefinition();
        static ResultTable ExecuteChartPoints<T, X, Y, C>(ChartRequest request, DynamicQuery<T> dq)
        {
            IEnumerable<T> collection;
            if (dq is AutoDynamicQuery<T>)
            {
                IQueryable<T> query = ((AutoDynamicQuery<T>)dq).Query;

                if (request.Filters != null && request.Filters.Count != 0)
                    query = DynamicQuery.DynamicQuery.Where(query, request.Filters);

                collection = query;
            }
            else
            {
                collection = ((ManualDynamicQuery<T>)dq).Execute(new QueryRequest { QueryName = request.QueryName, Filters = request.Filters }).Select(a => a.Value);
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "e");

            ConstructorInfo ci = typeof(Point<X, Y, C>).GetConstructor(new[] { typeof(X), typeof(Y), typeof(C) });

            List<Point<X, Y, C>> list;

            if (!request.GroupResults)
            {
                var selector = Expression.Lambda<Func<T, Point<X, Y, C>>>(
                    Expression.New(ci,
                        request.FirstDimension.BuildExpression(pe).Nullify(),
                        request.SecondDimension.BuildExpression(pe).Nullify(),
                        request.FirstValue.BuildExpression(pe).Nullify()), pe);

                list = (collection is IQueryable ? ((IQueryable<T>)collection).Select(selector) : collection.Select(selector.Compile())).ToList();

            
            }
            else
            {
                ParameterExpression ge = Expression.Parameter(typeof(IEnumerable<T>), "g");
                ParameterExpression k = Expression.Parameter(typeof(C), "k");

                var keySelector = Expression.Lambda<Func<T, C>>(request.FirstValue.BuildExpression(pe).Nullify(), pe);

                var selector = Expression.Lambda<Func<C, IEnumerable<T>, Point<X, Y, C>>>(Expression.New(ci,
                         request.FirstDimension.BuildExpression(ge).Nullify(),
                         request.SecondDimension.BuildExpression(ge).Nullify(),
                         k), k, ge);

                list = (collection is IQueryable ?
                    ((IQueryable<T>)collection).GroupBy(keySelector, selector) :
                    collection.GroupBy(keySelector.Compile(), selector.Compile())).ToList();
            }

            return new ResultTable(
                new ColumnValues(GetUserColumn(0, request.FirstDimension), list.Select(a => a.XValue).ToArray()),
                new ColumnValues(GetUserColumn(1, request.SecondDimension), list.Select(a => a.YValue).ToArray()),
                new ColumnValues(GetUserColumn(2, request.FirstValue), list.Select(a => a.Color).ToArray()));  
        }


        static MethodInfo miExecuteChartBubbles = ReflectionTools.GetMethodInfo(() => ExecuteChartBubbles<int, int, int, int, int>(null, null)).GetGenericMethodDefinition();
        static ResultTable ExecuteChartBubbles<T, X, Y, C, S>(ChartRequest request, DynamicQuery<T> dq)
        {
            IEnumerable<T> collection;
            if (dq is AutoDynamicQuery<T>)
            {
                IQueryable<T> query = ((AutoDynamicQuery<T>)dq).Query;

                if (request.Filters != null && request.Filters.Count != 0)
                    query = DynamicQuery.DynamicQuery.Where(query, request.Filters);

                collection = query;
            }
            else
            {
                collection = ((ManualDynamicQuery<T>)dq).Execute(new QueryRequest { QueryName = request.QueryName, Filters = request.Filters }).Select(a => a.Value);
            }

            ParameterExpression pe = Expression.Parameter(typeof(T), "e");

            ConstructorInfo ci = typeof(Bubble<X, Y, C, S>).GetConstructor(new[] { typeof(X), typeof(Y), typeof(C), typeof(S) });

            List<Bubble<X, Y, C, S>> list;

            if (!request.GroupResults)
            {
                var selector = Expression.Lambda<Func<T, Bubble<X, Y, C, S>>>(
                    Expression.New(ci,
                        request.FirstDimension.BuildExpression(pe).Nullify(),
                        request.SecondDimension.BuildExpression(pe).Nullify(),
                        request.FirstValue.BuildExpression(pe).Nullify(),
                        request.SecondValue.BuildExpression(pe).Nullify()), pe);

                list = (collection is IQueryable ? ((IQueryable<T>)collection).Select(selector) : collection.Select(selector.Compile())).ToList();

            }
            else
            {
                ParameterExpression ge = Expression.Parameter(typeof(IEnumerable<T>), "g");
                ParameterExpression k = Expression.Parameter(typeof(C), "k");

                var keySelector = Expression.Lambda<Func<T, C>>(request.FirstValue.BuildExpression(pe).Nullify(), pe);

                var selector = Expression.Lambda<Func<C, IEnumerable<T>, Bubble<X, Y, C, S>>>(Expression.New(ci,
                         request.FirstDimension.BuildExpression(ge).Nullify(),
                         request.SecondDimension.BuildExpression(ge).Nullify(),
                         k,
                         request.SecondValue.BuildExpression(ge).Nullify()), k, ge);

                list = (collection is IQueryable ?
                    ((IQueryable<T>)collection).GroupBy(keySelector, selector) :
                    collection.GroupBy(keySelector.Compile(), selector.Compile())).ToList();
            }

            return new ResultTable(
                new ColumnValues(GetUserColumn(0, request.FirstDimension), list.Select(a => a.XValue).ToArray()),
                new ColumnValues(GetUserColumn(1, request.SecondDimension), list.Select(a => a.YValue).ToArray()),
                new ColumnValues(GetUserColumn(2, request.FirstValue), list.Select(a => a.Color).ToArray()),
                new ColumnValues(GetUserColumn(3, request.SecondValue), list.Select(a => a.Size).ToArray()));
        }

        static UserColumn GetUserColumn(int baseIndex, ChartTokenDN token)
        {
            if (token.Aggregate == AggregateFunction.Count)
                return new UserColumn(baseIndex, "Count", token.Type)
                {
                    DisplayName = "Count",
                };

            return new UserColumn(baseIndex, token.Token)
            {
                DisplayName = token.Title,
            };
        }

        static List<KeyValuePair<K, V>> AssertToDictionary<K, V>(this IEnumerable<KeyValuePair<K, V>> collection)
        {
            var result = collection.ToList();

            var keys = new HashSet<K>(result.Select(a => a.Key));

            if (result.Count != keys.Count)
                throw new ApplicationException("There are repeated keys, try grouping");

            return result; 
        }

        public static List<Lite<UserChartDN>> GetUserCharts(object queryName)
        {
            return (from er in Database.Query<UserChartDN>()
                    where er.Query.Key == QueryUtils.GetQueryName(queryName)
                    select er.ToLite()).ToList();
        }

        public static void RemoveUserChart(Lite<UserChartDN> lite)
        {
            Database.Delete(lite);
        }

        public static void RegisterUserEntityGroup(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartDN uq) => uq.Related, typeof(UserDN));

            EntityGroupLogic.Register<UserChartDN>(newEntityGroupKey, uq => uq.Related.RefersTo(UserDN.Current));
        }

        public static void RegisterRoleEntityGroup(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartDN uq) => uq.Related, typeof(RoleDN));

            EntityGroupLogic.Register<UserChartDN>(newEntityGroupKey, uq => AuthLogic.CurrentRoles().Contains(uq.Related.ToLite<RoleDN>()));
        }
    }

    internal struct Point<X,Y,C>
    {
        X x;
        public X XValue { get { return x; } }

        Y y;
        public Y YValue { get { return y; } }

        C color;
        public C Color { get { return color; } }

        public Point(X x, Y y, C color)
        {
            this.x = x;
            this.y = y;
            this.color = color;
        }
    }

    internal struct Bubble<X, Y, C, S>
    {
        X x;
        public X XValue { get { return x; } }

        Y y;
        public Y YValue { get { return y; } }

        C color;
        public C Color { get { return color; } }

        S size;
        public S Size { get { return size; } }

        public Bubble(X x, Y y, C color, S size)
        {
            this.x = x;
            this.y = y;
            this.color = color;
            this.size = size;
        }
    }
}
