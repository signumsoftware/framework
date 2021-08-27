using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Engine.MachineLearning
{
    public static class PredictorPredictLogic
    {
        public static RecentDictionary<Lite<PredictorEntity>, PredictorPredictContext> TrainedPredictorCache = new RecentDictionary<Lite<PredictorEntity>, PredictorPredictContext>(50);


        public static Lite<PredictorEntity> GetCurrentPredictor(PredictorPublicationSymbol publication)
        {
            var predictor = Database.Query<PredictorEntity>().Where(a => a.Publication == publication).Select(a => a.ToLite()).SingleEx();

            return predictor;
        }

        public static PredictorPredictContext GetPredictContext(this PredictorPublicationSymbol publication) => GetCurrentPredictor(publication).GetPredictContext();

        public static PredictorPredictContext GetPredictContext(this Lite<PredictorEntity> predictor)
        {
            lock (TrainedPredictorCache)
                return TrainedPredictorCache.GetOrCreate(predictor, () =>
                {
                    using (ExecutionMode.Global())
                    using (var t = Transaction.ForceNew())
                    {
                        var p = predictor.RetrieveAndRemember();
                        if (p.State != PredictorState.Trained)
                            throw new InvalidOperationException($"Predictor '{p.Name}' not trained");

                        PredictorPredictContext ppc = CreatePredictContext(p);
                        return t.Commit(ppc);
                    }
                });
        }

        public static PredictorPredictContext CreatePredictContext(this PredictorEntity p)
        {
            var codifications = p.RetrievePredictorCodifications();
            var ppc = new PredictorPredictContext(p, PredictorLogic.Algorithms.GetOrThrow(p.Algorithm), codifications);
            ppc.Algorithm.LoadModel(ppc);
            return ppc;
        }

        public static PredictDictionary GetInputsFromEntity(this PredictorPredictContext ctx, Lite<Entity> entity, PredictionOptions? options = null)
        {
            var qd = QueryLogic.Queries.QueryDescription(ctx.Predictor.MainQuery.Query.ToQueryName());

            var entityToken = QueryUtils.Parse("Entity", qd, 0);

            return ctx.FromFilters(new List<Filter> { new FilterCondition(entityToken, FilterOperation.EqualTo, entity) }, options).SingleEx();
        }

        public static PredictDictionary GetInputsFromParentKeys(this PredictorPredictContext ctx, Dictionary<QueryToken, object?> parentKeyValues, PredictionOptions? options = null)
        {
            if (!ctx.Predictor.MainQuery.GroupResults)
            {
                var kvp = parentKeyValues.SingleEx();

                if (kvp.Key.FullKey() != "Entity")
                    throw new InvalidOperationException("only Entity expected");

                var filters = new List<Filter> { new FilterCondition(kvp.Key, FilterOperation.EqualTo, kvp.Value) };

                return ctx.FromFilters(filters, options).SingleEx();
            }
            else
            {
                var filters = ctx.Predictor.MainQuery.Columns
                    .Select(a => a.Token.Token)
                    .Where(t => !(t is AggregateToken))
                    .Select(t => (Filter)new FilterCondition(t, FilterOperation.EqualTo, parentKeyValues.GetOrThrow(t)))
                    .ToList();

                return ctx.FromFilters(filters, options).SingleEx();
            }
        }

        public static PredictDictionary GetInputsEmpty(this PredictorPredictContext ctx, PredictionOptions? options = null)
        {
            var result = new PredictDictionary(ctx.Predictor, options, null)
            {
                MainQueryValues = ctx.Predictor.MainQuery.Columns.Select((c, i) => KeyValuePair.Create(c, (object?)null)).ToDictionaryEx(),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = new Dictionary<object?[], Dictionary<PredictorSubQueryColumnEmbedded, object?>>(ObjectArrayComparer.Instance)
                })
            };

            return result;
        }

        public static List<PredictDictionary> FromFilters(this PredictorPredictContext ctx, List<Filter> filters, PredictionOptions? options = null)
        {
            var qd = QueryLogic.Queries.QueryDescription(ctx.Predictor.MainQuery.Query.ToQueryName());
            
            var qr = new QueryRequest
            {
                QueryName = qd.QueryName,

                GroupResults = ctx.Predictor.MainQuery.GroupResults,

                Filters = filters, /*Filters of Main Query not considered*/

                Columns = ctx.Predictor.MainQuery.Columns.Select(c => new Column(c.Token.Token, null)).ToList(),

                Pagination = new Pagination.All(),
                Orders = Enumerable.Empty<Order>().ToList(),
            };

            var mainQueryKeys = PredictorLogic.GetParentKeys(ctx.Predictor.MainQuery);

            var rt = QueryLogic.Queries.ExecuteQuery(qr);

            var subQueryResults = ctx.Predictor.SubQueries.ToDictionaryEx(sq => sq, sqe =>
            {
                List<QueryToken> parentKeys = sqe.Columns.Where(a => a.Usage == PredictorSubQueryColumnUsage.ParentKey).Select(a => a.Token.Token).ToList();

                QueryDescription sqd = QueryLogic.Queries.QueryDescription(sqe.Query.ToQueryName());

                Dictionary<string, string> tokenReplacements = mainQueryKeys.ZipStrict(parentKeys, (m, p) => KeyValuePair.Create(m.FullKey(), p.FullKey())).ToDictionaryEx();

                Filter[] mainFilters = filters.Select(f => Replace(f, tokenReplacements, sqd)).ToArray();

                List<Filter> additionalFilters = sqe.Filters.ToFilterList();

                List<Column> allColumns = sqe.Columns.Select(c => new Column(c.Token.Token, null)).ToList();

                var qgr = new QueryRequest
                {
                    QueryName = sqe.Query.ToQueryName(),
                    GroupResults = true,
                    Filters = mainFilters.Concat(additionalFilters).ToList(),
                    Columns = allColumns,
                    Orders = new List<Order>(),
                    Pagination = new Pagination.All(),
                };

                ResultTable resultTable = QueryLogic.Queries.ExecuteQuery(qgr);

                var tuples = sqe.Columns.Zip(resultTable.Columns, (sqc, rc) => (sqc: sqc, rc: rc)).ToList();
                ResultColumn[] entityGroupKey = tuples.Extract(t => t.sqc.Usage == PredictorSubQueryColumnUsage.ParentKey).Select(a=>a.rc).ToArray();
                ResultColumn[] remainingKeys = tuples.Extract(t => t.sqc.Usage == PredictorSubQueryColumnUsage.SplitBy).Select(a => a.rc).ToArray();
                var valuesTuples = tuples;

                return resultTable.Rows.AgGroupToDictionary(
                    row => row.GetValues(entityGroupKey),
                    gr => gr.ToDictionaryEx(
                        row => row.GetValues(remainingKeys),
                        row => valuesTuples.ToDictionaryEx(t => t.sqc, t => row[t.rc]),
                        ObjectArrayComparer.Instance
                    )
                );
            });

            var mainKeys = rt.Columns.Where(rc => !(rc.Column.Token is AggregateToken)).ToArray();

            var result = rt.Rows.Select(row => new PredictDictionary(ctx.Predictor, options, row.TryEntity)
            {
                MainQueryValues = ctx.Predictor.MainQuery.Columns.Select((c, i) => KeyValuePair.Create(c, row[i])).ToDictionaryEx(),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = subQueryResults.TryGetC(sq)?.TryGetC(row.GetValues(mainKeys)) ?? 
                    new Dictionary<object?[], Dictionary<PredictorSubQueryColumnEmbedded, object?>>(ObjectArrayComparer.Instance)
                })
            }).ToList();

            return result;
        }

        private static Filter Replace(Filter filter, Dictionary<string, string> tokenReplacements, QueryDescription qd)
        {
            if (filter is FilterGroup fg)
                return new FilterGroup(fg.GroupOperation, Replace(fg.Token!, tokenReplacements, qd), fg.Filters.Select(f => Replace(f, tokenReplacements, qd)).ToList());

            if (filter is FilterCondition fc)
                return new FilterCondition(Replace(fc.Token, tokenReplacements, qd), fc.Operation, fc.Value);

            throw new UnexpectedValueException(filter);
        }

        private static QueryToken Replace(QueryToken token, Dictionary<string, string> tokenReplacements, QueryDescription qd)
        {
            var tokenFullKey = token.FullKey();
            var bestKey = tokenReplacements.Keys.OrderByDescending(a => a.Length)
                .Where(k => k == tokenFullKey || tokenFullKey.StartsWith(k + "."))
                .FirstEx(() => "Impossible to use token '" + tokenFullKey + "' in SubQuery");


            var newToken = bestKey == tokenFullKey ? tokenReplacements.GetOrThrow(bestKey) :
                tokenReplacements.GetOrThrow(bestKey) + tokenFullKey.After(bestKey);

            return QueryUtils.Parse(newToken, qd, SubTokensOptions.CanAggregate | SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement);

        }

        public static Dictionary<ResultRow, PredictDictionary> ToPredictDictionaries(this PredictorTrainingContext ctx, PredictionOptions? options = null)
        {   
            var subQueryResults = ctx.Predictor.SubQueries.ToDictionaryEx(sq => sq, sqe =>
            {
                var resultTable = ctx.SubQueries[sqe].ResultTable;

                var tuples = sqe.Columns.Zip(resultTable.Columns, (sqc, rc) => (sqc: sqc, rc: rc)).ToList();
                ResultColumn[] parentKeys = tuples.Extract(t => t.sqc.Usage == PredictorSubQueryColumnUsage.ParentKey).Select(a => a.rc).ToArray();
                ResultColumn[] remainingKeys = tuples.Extract(t => t.sqc.Usage == PredictorSubQueryColumnUsage.SplitBy).Select(a => a.rc).ToArray();
                var valuesTuples = tuples;

                return resultTable.Rows.AgGroupToDictionary(
                    row => row.GetValues(parentKeys),
                    gr => gr.ToDictionaryEx(
                        row => row.GetValues(remainingKeys),
                        row => valuesTuples.ToDictionaryEx(t => t.sqc, t => row[t.rc]),
                        ObjectArrayComparer.Instance
                    )
                );
            });

            var result = ctx.MainQuery.ResultTable.Rows.ToDictionaryEx(row => row, row => new PredictDictionary(ctx.Predictor, options, row.TryEntity)
            {
                MainQueryValues = ctx.Predictor.MainQuery.Columns.Select((c, i) => KeyValuePair.Create(c, row[i])).ToDictionaryEx(),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = (subQueryResults.TryGetC(sq)?.TryGetC(ctx.MainQuery.GetParentKey(row))) ?? new Dictionary<object?[], Dictionary<PredictorSubQueryColumnEmbedded, object?>>(ObjectArrayComparer.Instance)
                })
            });

            return result;
        }

        public static PredictDictionary PredictBasic(this PredictDictionary input)
        {
            var pctx = GetPredictContext(input.Predictor.ToLite());

            return pctx.Algorithm.Predict(pctx, input);
        }
    }
}
