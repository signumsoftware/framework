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
using Signum.Entities.Reflection;

namespace Signum.Engine.Extensions.Chart
{
    public static class ChartLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);


                sb.Include<UserChartDN>();

                dqm[typeof(UserChartDN)] = (from uq in Database.Query<UserChartDN>()
                                            select new
                                            {
                                                Entity = uq.ToLite(),
                                                Query = uq.Query.ToLite(),
                                                uq.Id,
                                                uq.DisplayName,
                                                Filters = uq.Filters.Count,
                                                uq.Chart.ChartType,
                                                uq.Chart.ChartResultType,
                                                uq.Chart.GroupResults,
                                            }).ToDynamic();

                sb.Schema.EntityEvents<UserChartDN>().Retrieved += ChartLogic_Retrieved;
            }
        }

        public static UserChartDN ParseData(this UserChartDN userChart)
        {
            if (!userChart.IsNew || userChart.queryName == null)
                throw new InvalidOperationException("userChart should be new and have queryName");

            userChart.Query = QueryLogic.RetrieveOrGenerateQuery(userChart.queryName);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(userChart.queryName);

            userChart.ParseData(description);

            return userChart;
        }

        static void ChartLogic_Retrieved(UserChartDN userQuery)
        {
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            userQuery.ParseData(description);
        }

        public static List<Lite<UserChartDN>> GetUserCharts(object queryName)
        {
            return (from er in Database.Query<UserChartDN>()
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(queryName)
                    select er.ToLite()).ToList();
        }

        public static void RemoveUserChart(Lite<UserChartDN> lite)
        {
            Database.Delete(lite);
        }

        public static void RegisterUserEntityGroup(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartDN uq) => uq.Related, typeof(UserDN));

            TypeConditionLogic.Register<UserChartDN>(newEntityGroupKey, uq => uq.Related.RefersTo(UserDN.Current));
        }

        public static void RegisterRoleEntityGroup(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartDN uq) => uq.Related, typeof(RoleDN));

            TypeConditionLogic.Register<UserChartDN>(newEntityGroupKey, uq => AuthLogic.CurrentRoles().Contains(uq.Related.ToLite<RoleDN>()));
        }


        public static ResultTable ExecuteChart(ChartRequest request)
        {
            IDynamicQuery dq = DynamicQueryManager.Current[request.QueryName];

            if (dq.GetType().FollowC(t => t.BaseType).Any(t => t.IsInstantiationOf(typeof(DynamicQuery<>))))
            {
                return miExecuteChart.GetInvoker(dq.GetType().GetGenericArguments()[0])(request, dq);
            }

            throw new NotImplementedException(); 
        }

        static GenericInvoker<Func<IEnumerable<object>, Delegate, Delegate, IEnumerable<object>>> giGroupByE =
            new GenericInvoker<Func<IEnumerable<object>, Delegate, Delegate, IEnumerable<object>>>(
                (col, ks, rs) => (IEnumerable<object>)Enumerable.GroupBy<string, int, double>((IEnumerable<string>)col, (Func<string, int>)ks, (Func<int, IEnumerable<string>, double>)rs)); 
        static IEnumerable<object> GroupBy(this IEnumerable<object> collection, LambdaExpression keySelector, LambdaExpression resultSelector)
        {
            return giGroupByE.GetInvoker(typeof(object), keySelector.Body.Type, typeof(object))(collection, keySelector.Compile(), resultSelector.Compile()); 
        }

        static MethodInfo miGroupByQ = ReflectionTools.GetMethodInfo(() => Queryable.GroupBy<string, int, double>((IQueryable<string>)null, (Expression<Func<string, int>>)null, (Expression<Func<int, IEnumerable<string>, double>>)null)).GetGenericMethodDefinition();
        static IQueryable<object> GroupBy(this IQueryable<object> query, LambdaExpression keySelector, LambdaExpression resultSelector)
        {
            return (IQueryable<object>)query.Provider.CreateQuery<object>(Expression.Call(null, miGroupByQ.MakeGenericMethod(typeof(object), keySelector.Body.Type, typeof(object)),
                new Expression[] { query.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector) }));
        }

        static LambdaExpression BuildKeySelector(IDynamicInfo dinamicInfo, ChartTokenDN[] groupTokens)
        {
            return Expression.Lambda(
                TupleReflection.TupleChainConstructor(groupTokens.Select(a => a.Token.BuildExpression(dinamicInfo.Context))),
                dinamicInfo.Context.Parameter);
        }

        static LambdaExpression BuildResultSelector(ChartTokenDN[] chartTokens, ChartTokenDN[] groupTokens, IDynamicInfo dynamicInfo, Type keyTupleType, out Type resultSelectorTypleType)
        {
            ParameterExpression pk = Expression.Parameter(keyTupleType, "key");
            ParameterExpression pg = Expression.Parameter(typeof(IEnumerable<object>), "e");

            var list = chartTokens.Select(ct => ct.Aggregate == null ?
                TupleReflection.TupleChainProperty(pk, groupTokens.IndexOf(ct)) :
                BuildAggregateExpression(pg, ct.Aggregate.Value, Expression.Lambda(ct.Token.BuildExpression(dynamicInfo.Context), dynamicInfo.Context.Parameter))).ToArray();

            var constructor = TupleReflection.TupleChainConstructor(list);

            resultSelectorTypleType = constructor.Type;

            return Expression.Lambda(Expression.Convert(constructor, typeof(object)), pk, pg);
        }

        static Expression BuildAggregateExpression(Expression collection, AggregateFunction aggregate, LambdaExpression lambda)
        {
            Type groupType = collection.Type.GetGenericInterfaces(typeof(IEnumerable<>)).SingleEx(()=>"expression should be a IEnumerable").GetGenericArguments()[0];

            if (aggregate == AggregateFunction.Count)
                return Expression.Call(typeof(Enumerable), "Count", new[] { groupType }, new[] { collection });

            //ParameterExpression a = Expression.Parameter(groupType, "a");
            //LambdaExpression lambda = Expression.Lambda(TupleReflection.TupleChainProperty(Expression.Convert(a, tupleType), index), a);

            if (aggregate == AggregateFunction.Min || aggregate == AggregateFunction.Max)
                return Expression.Call(typeof(Enumerable), aggregate.ToString(), new[] { groupType, lambda.Body.Type }, new[] { collection, lambda });
            else
                return Expression.Call(typeof(Enumerable), aggregate.ToString(), new[] { groupType }, new[] { collection, lambda });
        }

        static GenericInvoker<Func<ChartRequest, IDynamicQuery, ResultTable>> miExecuteChart = new GenericInvoker<Func<ChartRequest, IDynamicQuery, ResultTable>>((req, dq) => ExecuteChart<int>(req, (DynamicQuery<int>)dq));
        static ResultTable ExecuteChart<T>(ChartRequest request, DynamicQuery<T> dq)
        {
            ChartTokenDN[] chartTokens = request.ChartTokens().ToArray(); 
            List<Column> columns = chartTokens.Select(t=>t.CreateSimpleColumn()).ToList();

            if (!request.Chart.GroupResults)
                columns.Add(new _EntityColumn(dq.EntityColumn().BuildColumnDescription()));

            IDynamicInfo collection;
            if (dq is AutoDynamicQuery<T>)
            {
                collection = ((AutoDynamicQuery<T>)dq).Query.ToDQueryable(dq.GetColumnDescriptions()).SelectMany(request.Multiplications).Where(request.Filters).Select(columns);
            }
            else
            {
                collection = ((ManualDynamicQuery<T>)dq).Execute(new QueryRequest
                {
                    Columns = columns,
                    Filters = request.Filters,
                    QueryName = request.QueryName
                }, dq.GetColumnDescriptions());
            }

            if (!request.Chart.GroupResults)
            {
                var orders = (from ct in chartTokens
                              where ct.OrderPriority.HasValue
                              orderby ct.OrderPriority.Value
                              select new Order(ct.Token, ct.OrderType.Value)).ToList();

                object[] result;
                if (collection is DQueryable<T>)
                    result = ((DQueryable<T>)collection).OrderBy(orders).Query.ToArray();
                else
                    result = ((DEnumerable<T>)collection).OrderBy(orders).Collection.ToArray();

                var cols = columns.Select(c => Tuple.Create(c,
                    Expression.Lambda(c.Token.BuildExpression(collection.Context), collection.Context.Parameter))).ToList();

                return result.ToResultTable(cols, result.Length, 0, null);
            }
            else
            {
                ChartTokenDN[] groupTokens = chartTokens.Where(t => t.Aggregate == null).ToArray();
                LambdaExpression keySelector = BuildKeySelector(collection, groupTokens);

                Type resultType;
                LambdaExpression resultSelector = BuildResultSelector(chartTokens, groupTokens, collection, keySelector.Body.Type, out resultType);

                var orders = (from t in chartTokens.Select((ct, i) => new { ct, i })
                              where t.ct.OrderPriority.HasValue
                              orderby t.ct.OrderPriority.Value
                              select Tuple.Create(
                                  TupleReflection.TupleChainPropertyLambda(resultType, t.i),
                                  t.ct.OrderType.Value)).ToList();

                object[] result;
                if (collection is DQueryable<T>)
                    result = ((DQueryable<T>)collection).Query.GroupBy(keySelector, resultSelector).OrderBy(orders).ToArray();
                else
                    result = ((DEnumerable<T>)collection).Collection.GroupBy(keySelector, resultSelector).OrderBy(orders).ToArray();

                var cols = columns.Select((c, i) =>
                    Tuple.Create(c, TupleReflection.TupleChainPropertyLambda(resultType, i))).ToList();

                return result.ToResultTable(cols, cols.Count, 0, null);
            }
        }

    }

}
