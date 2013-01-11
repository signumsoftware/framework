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
using Signum.Engine.Operations;

namespace Signum.Engine.Chart
{
    public static class ChartLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                PermissionAuthLogic.RegisterTypes(typeof(ChartPermission));

                ChartColorLogic.Start(sb, dqm);
                ChartScriptLogic.Start(sb, dqm);
                UserChartLogic.Start(sb, dqm);

                ChartUtils.RemoveNotNullValidators();
            }
        }

        public static ResultTable ExecuteChart(ChartRequest request)
        {
            IDynamicQuery dq = DynamicQueryManager.Current[request.QueryName];

            if (dq.GetType().FollowC(t => t.BaseType).Any(t => t.IsInstantiationOf(typeof(DynamicQuery<>))))
            {
                using (ExecutionMode.UserInterface())
                    return miExecuteChart.GetInvoker(dq.GetType().GetGenericArguments()[0])(request, dq);
            }

            throw new NotImplementedException(); 
        }

        static GenericInvoker<Func<IEnumerable<object>, Delegate, Delegate, IEnumerable<object>>> giGroupByE =
            new GenericInvoker<Func<IEnumerable<object>, Delegate, Delegate, IEnumerable<object>>>(
                (col, ks, rs) => (IEnumerable<object>)Enumerable.GroupBy<string, int, double>((IEnumerable<string>)col, (Func<string, int>)ks, (Func<int, IEnumerable<string>, double>)rs)); 
        static DEnumerable<T> GroupBy<T>(this DEnumerable<T> collection, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens)
        {
            var keySelector = KeySelector(collection.Context, keyTokens);

            BuildExpressionContext newContext;
            LambdaExpression resultSelector = ResultSelectSelectorAndContext(collection.Context, keyTokens, aggregateTokens, keySelector.Type, out newContext);

            var resultCollection = giGroupByE.GetInvoker(typeof(object), keySelector.Body.Type, typeof(object))(collection.Collection, keySelector.Compile(), resultSelector.Compile());

            return new DEnumerable<T>(resultCollection, newContext);
        }

        static MethodInfo miGroupByQ = ReflectionTools.GetMethodInfo(() => Queryable.GroupBy<string, int, double>((IQueryable<string>)null, (Expression<Func<string, int>>)null, (Expression<Func<int, IEnumerable<string>, double>>)null)).GetGenericMethodDefinition();
        static DQueryable<T> GroupBy<T>(this DQueryable<T> query, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens)
        {
            var keySelector = KeySelector(query.Context, keyTokens);

            BuildExpressionContext newContext;
            LambdaExpression resultSelector = ResultSelectSelectorAndContext(query.Context, keyTokens, aggregateTokens, keySelector.Body.Type, out newContext);

            var resultQuery = (IQueryable<object>)query.Query.Provider.CreateQuery<object>(Expression.Call(null, miGroupByQ.MakeGenericMethod(typeof(object), keySelector.Body.Type, typeof(object)),
                new Expression[] { query.Query.Expression, Expression.Quote(keySelector), Expression.Quote(resultSelector) }));

            return new DQueryable<T>(resultQuery, newContext);
        }

        static LambdaExpression ResultSelectSelectorAndContext(BuildExpressionContext context, HashSet<QueryToken> keyTokens, HashSet<AggregateToken> aggregateTokens, Type keyTupleType, out BuildExpressionContext newContext)
        {
            Dictionary<QueryToken, Expression> resultExpressions = new Dictionary<QueryToken, Expression>();
            ParameterExpression pk = Expression.Parameter(keyTupleType, "key");
            resultExpressions.AddRange(keyTokens.Select((kt, i) => KVP.Create(kt,
                TupleReflection.TupleChainProperty(pk, i))));

            ParameterExpression pe = Expression.Parameter(typeof(IEnumerable<object>), "e");
            resultExpressions.AddRange(aggregateTokens.Select(at => KVP.Create((QueryToken)at,
                BuildAggregateExpression(pe, at, context))));

            var resultConstructor = TupleReflection.TupleChainConstructor(resultExpressions.Values);

            ParameterExpression pg = Expression.Parameter(typeof(object), "gr");
            newContext = new BuildExpressionContext(resultConstructor.Type, pg,
                resultExpressions.Keys.Select((t, i) => KVP.Create(t, TupleReflection.TupleChainProperty(Expression.Convert(pg, resultConstructor.Type), i))).ToDictionary());

            return Expression.Lambda(Expression.Convert(resultConstructor, typeof(object)), pk, pe);
        }

        static LambdaExpression KeySelector(BuildExpressionContext context, HashSet<QueryToken> keyTokens)
        {
            var keySelector = Expression.Lambda(
              TupleReflection.TupleChainConstructor(keyTokens.Select(t => t.BuildExpression(context)).ToList()),
              context.Parameter);
            return keySelector;
        }

        static Expression BuildAggregateExpression(Expression collection, AggregateToken at, BuildExpressionContext context)
        {
            Type groupType = collection.Type.GetGenericInterfaces(typeof(IEnumerable<>)).SingleEx(() => "expression should be a IEnumerable").GetGenericArguments()[0];

            if (at.AggregateFunction == AggregateFunction.Count)
                return Expression.Call(typeof(Enumerable), "Count", new[] { groupType }, new[] { collection });

            var body = at.Parent.BuildExpression(context);

            var type = at.ConvertTo();

            if (type != null)
                body = body.TryConvert(type); 

             var lambda = Expression.Lambda(body, context.Parameter); 

            if (at.AggregateFunction == AggregateFunction.Min || at.AggregateFunction  == AggregateFunction.Max)
                return Expression.Call(typeof(Enumerable), at.AggregateFunction.ToString(), new[] { groupType, lambda.Body.Type }, new[] { collection, lambda });

            return Expression.Call(typeof(Enumerable), at.AggregateFunction.ToString(), new[] { groupType }, new[] { collection, lambda });
        }

        static GenericInvoker<Func<ChartRequest, IDynamicQuery, ResultTable>> miExecuteChart = new GenericInvoker<Func<ChartRequest, IDynamicQuery, ResultTable>>((req, dq) => ExecuteChart<int>(req, (DynamicQuery<int>)dq));
        static ResultTable ExecuteChart<T>(ChartRequest request, DynamicQuery<T> dq)
        {
            List<Column> columns = request.Columns.Where(c => c.Token != null).Select(t => t.CreateColumn()).ToList();

            var multiplications = request.Multiplications;;

            if (!request.GroupResults)
            {
                columns.Add(new _EntityColumn(dq.EntityColumn().BuildColumnDescription()));

                if (dq is AutoDynamicQuery<T>)
                {
                    DQueryable<T> query = ((AutoDynamicQuery<T>)dq).Query.ToDQueryable(dq.GetColumnDescriptions())
                        .SelectMany(multiplications)
                        .Where(request.Filters)
                        .OrderBy(request.Orders)
                        .Select(columns);

                    return ResultTable(query.Query.ToArray(), columns, query.Context);
                }
                else
                {
                    DEnumerableCount<T> collection = ((ManualDynamicQuery<T>)dq).Execute(new QueryRequest
                    {
                        Columns = columns,
                        Filters = request.Filters,
                        QueryName = request.QueryName,
                        Orders = request.Orders,
                    }, dq.GetColumnDescriptions());

                    return ResultTable(collection.Collection.ToArray(), columns, collection.Context);
                }
            }
            else
            {
                var simpleFilters = request.Filters.Where(f => !(f.Token is AggregateToken)).ToList();
                var aggregateFilters = request.Filters.Where(f => f.Token is AggregateToken).ToList();

                var keys = columns.Select(t=>t.Token).Where(t => !(t is AggregateToken)).ToHashSet(); 

                var allAggregates = request.AllTokens().OfType<AggregateToken>().ToHashSet(); 
                
                if (dq is AutoDynamicQuery<T>)
                {
                    DQueryable<T> query = ((AutoDynamicQuery<T>)dq).Query.ToDQueryable(dq.GetColumnDescriptions())
                        .SelectMany(multiplications)
                        .Where(simpleFilters)
                        .GroupBy(keys, allAggregates)
                        .Where(aggregateFilters)
                        .OrderBy(request.Orders);    
                        
                    return ResultTable(query.Query.ToArray(), columns, query.Context);
                }
                else
                {
                    DEnumerableCount<T> plainCollection = ((ManualDynamicQuery<T>)dq).Execute(new QueryRequest
                    { 
                        Columns = keys.Concat(allAggregates.Select(at=>at.Parent).NotNull()).Distinct().Select(t=>new Column(t, t.NiceName())).ToList(),
                        Filters = simpleFilters,
                        QueryName = request.QueryName,
                    }, dq.GetColumnDescriptions());


                    var groupCollection = plainCollection
                        .GroupBy(keys, allAggregates)
                        .Where(aggregateFilters)
                        .OrderBy(request.Orders);

                    return ResultTable(groupCollection.Collection.ToArray(), columns, groupCollection.Context);
                }
            }
        }

        static ResultTable ResultTable(object[] values, List<Column> columns, BuildExpressionContext context)
        {
            var cols = columns.Select(c => Tuple.Create(c,
                Expression.Lambda(c.Token.BuildExpression(context), context.Parameter))).ToList();

            return values.ToResultTable(cols, values.Length, 0, QueryRequest.AllElements);
        }
    }
}
