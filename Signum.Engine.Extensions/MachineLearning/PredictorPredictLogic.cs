using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public static PredictorPredictContext GetPredictContext(this Lite<PredictorEntity> predictor)
        {
            lock (TrainedPredictorCache)
                return TrainedPredictorCache.GetOrCreate(predictor, () =>
                {
                    using (ExecutionMode.Global())
                    using (var t = Transaction.ForceNew())
                    {
                        var p = predictor.Retrieve();
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

        public static PredictDictionary GetInputsFromEntity(this PredictorPredictContext ctx, Lite<Entity> entity)
        {
            var qd = DynamicQueryManager.Current.QueryDescription(ctx.Predictor.MainQuery.Query.ToQueryName());

            var entityToken = QueryUtils.Parse("Entity", qd, 0);

            return ctx.FromFilters(new List<Filter> { new Filter(entityToken, FilterOperation.EqualTo, entity) }).SingleEx();
        }

        public static PredictDictionary GetInputsFromParentKeys(this PredictorPredictContext ctx, Dictionary<QueryToken, object> parentKeyValues)
        {
            if (!ctx.Predictor.MainQuery.GroupResults)
            {
                var kvp = parentKeyValues.SingleEx();

                if (kvp.Key.FullKey() != "Entity")
                    throw new InvalidOperationException("only Entity expected");

                var filters = new List<Filter> { new Filter(kvp.Key, FilterOperation.EqualTo, kvp.Value) };

                return ctx.FromFilters(filters).SingleEx(); ;
            }
            else
            {
                var filters = ctx.Predictor.MainQuery.Columns
                    .Select(a => a.Token.Token)
                    .Where(t => !(t is AggregateToken))
                    .Select(t => new Filter(t, FilterOperation.EqualTo, parentKeyValues.GetOrThrow(t)))
                    .ToList();

                return ctx.FromFilters(filters).SingleEx();
            }
        }

        public static PredictDictionary GetInputsEmpty(this PredictorPredictContext ctx)
        {
            var result = new PredictDictionary(ctx.Predictor)
            {
                MainQueryValues = ctx.Predictor.MainQuery.Columns.Select((c, i) => KVP.Create(c, (object)null)).ToDictionaryEx(),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = new Dictionary<object[], Dictionary<PredictorSubQueryColumnEmbedded, object>>(ObjectArrayComparer.Instance)
                })
            };

            return result;
        }

        public static List<PredictDictionary> FromFilters(this PredictorPredictContext ctx, List<Filter> filters)
        {
            var qd = DynamicQueryManager.Current.QueryDescription(ctx.Predictor.MainQuery.Query.ToQueryName());
            
            var qr = new QueryRequest
            {
                QueryName = qd.QueryName,

                GroupResults = ctx.Predictor.MainQuery.GroupResults,

                Filters = filters,

                Columns = ctx.Predictor.MainQuery.Columns.Select(c => new Column(c.Token.Token, null)).ToList(),

                Pagination = new Pagination.All(),
                Orders = Enumerable.Empty<Order>().ToList(),
            };

            var rt = DynamicQueryManager.Current.ExecuteQuery(qr);

            var subQueryResults = ctx.Predictor.SubQueries.ToDictionaryEx(sq => sq, sqe =>
            {
                List<QueryToken> parentKeys = sqe.Columns.Where(a => a.Usage == PredictorSubQueryColumnUsage.ParentKey).Select(a => a.Token.Token).ToList();

                Filter[] mainFilters = filters.ZipStrict(parentKeys, (f, pk) => new Filter(pk, f.Operation, f.Value)).ToArray();

                List<Filter> additionalFilters = sqe.Filters.Select(f => PredictorLogicQuery.ToFilter(f)).ToList();

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

                ResultTable resultTable = DynamicQueryManager.Current.ExecuteQuery(qgr);

                var tuples = sqe.Columns.Zip(resultTable.Columns, (sqc, rc) => (sqc, rc)).ToList();
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

            var result = rt.Rows.Select(row => new PredictDictionary(ctx.Predictor)
            {
                Entity = row.TryEntity,
                MainQueryValues = ctx.Predictor.MainQuery.Columns.Select((c, i) => KVP.Create(c, row[i])).ToDictionaryEx(),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = subQueryResults.TryGetC(sq)?.TryGetC(row.GetValues(mainKeys)) ?? 
                    new Dictionary<object[], Dictionary<PredictorSubQueryColumnEmbedded, object>>(ObjectArrayComparer.Instance)
                })
            }).ToList();

            return result;
        }

        public static Dictionary<ResultRow, PredictDictionary> ToPredictDictionaries(this PredictorTrainingContext ctx)
        {   
            var subQueryResults = ctx.Predictor.SubQueries.ToDictionaryEx(sq => sq, sqe =>
            {
                var resultTable = ctx.SubQueries[sqe].ResultTable;

                var tuples = sqe.Columns.Zip(resultTable.Columns, (sqc, rc) => (sqc, rc)).ToList();
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

            var result = ctx.MainQuery.ResultTable.Rows.ToDictionaryEx(row => row, row => new PredictDictionary(ctx.Predictor)
            {
                MainQueryValues = ctx.Predictor.MainQuery.Columns.Select((c, i) => KVP.Create(c, row[i])).ToDictionaryEx(),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = (subQueryResults.TryGetC(sq)?.TryGetC(ctx.MainQuery.GetParentKey(row))) ?? new Dictionary<object[], Dictionary<PredictorSubQueryColumnEmbedded, object>>(ObjectArrayComparer.Instance)
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
