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

        public static PredictorPredictContext GetPredictContext(Lite<PredictorEntity> predictor)
        {
            return TrainedPredictorCache.GetOrCreate(predictor, () =>
            {
                using (ExecutionMode.Global())
                using (var t = Transaction.ForceNew())
                {
                    var p = predictor.Retrieve();
                    if (p.State != PredictorState.Trained)
                        throw new InvalidOperationException($"Predictor '{p.Name}' not trained");

                    var codifications = p.RetrievePredictorCodifications();
                    var ppc = new PredictorPredictContext(p,  PredictorLogic.Algorithms.GetOrThrow(p.Algorithm), codifications);
                    ppc.Algorithm.LoadModel(ppc);
                    return t.Commit(ppc);
                }
            });
        }
        
        public static PredictDictionary FromEntity(Lite<PredictorEntity> predictor, Lite<Entity> entity)
        {
            return FromEntities(predictor, new List<Lite<Entity>> { entity }).SingleEx().Value;
        }

        public static Dictionary<Lite<Entity>, PredictDictionary> FromEntities(Lite<PredictorEntity> predictor, List<Lite<Entity>> entities)
        {
            var ctx = GetPredictContext(predictor);

            var qd = DynamicQueryManager.Current.QueryDescription(ctx.Predictor.MainQuery.Query.ToQueryName());

            var entityToken = QueryUtils.Parse("Entity", qd, SubTokensOptions.CanElement);

            var qr = new QueryRequest
            {
                QueryName = qd.QueryName,

                Filters = new List<Filter> {  new Filter(entityToken, FilterOperation.IsIn, entities) },

                Columns = ctx.Predictor.MainQuery.Columns.Select(c => new Column(c.Token.Token, null)).ToList(),

                Pagination = new Pagination.All(),
                Orders = Enumerable.Empty<Order>().ToList(),
            };

            var rt = DynamicQueryManager.Current.ExecuteQuery(qr);

            var subQueryResults = ctx.Predictor.SubQueries.ToDictionaryEx(sq => sq, sqe =>
            {
                var parentKey = sqe.Columns.SingleEx(a=>a.Usage == PredictorSubQueryColumnUsage.ParentKey).Token.Token;

                var mainFilter = new Filter(parentKey, FilterOperation.IsIn, entities);

                var additionalFilters = sqe.Filters.Select(f => PredictorLogicQuery.ToFilter(f)).ToList();

                var allColumns = sqe.Columns.Select(c => new Column(c.Token.Token, null)).ToList();

                var qgr = new QueryRequest
                {
                    QueryName = sqe.Query.ToQueryName(),
                    GroupResults = true,
                    Filters = new[] { mainFilter }.Concat(additionalFilters).ToList(),
                    Columns = allColumns,
                    Orders = new List<Order>(),
                    Pagination = new Pagination.All(),
                };

                var groupResult = DynamicQueryManager.Current.ExecuteQuery(qgr);

                var tuples = sqe.Columns.Zip(groupResult.Columns, (sqc, rc) => (sqc, rc)).ToList();

                var entityGroupKey = tuples.Extract(t => t.sqc.Usage == PredictorSubQueryColumnUsage.ParentKey).SingleEx().rc;
                var remainingKeys = tuples.Extract(t => t.sqc.Usage == PredictorSubQueryColumnUsage.SplitBy).Select(a => a.rc).ToArray();
                var values = tuples.Select(a => a.rc).ToList(); 

                return groupResult.Rows.AgGroupToDictionary(row => (Lite<Entity>)row[entityGroupKey], gr =>
                    gr.ToDictionaryEx(
                        row => row.GetValues(remainingKeys),
                        row => sqe.Columns.Select((ac, i)=>KVP.Create(ac, row[values[i]])).ToDictionaryEx(),
                        ObjectArrayComparer.Instance));

            });

            var result = rt.Rows.ToDictionaryEx(row => row.Entity, row => new PredictDictionary(ctx.Predictor)
            {
                MainQueryValues = ctx.Predictor.MainQuery.Columns.Select((c, i) => KVP.Create(c, row[i])).ToDictionaryEx(),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = (subQueryResults.TryGetC(sq)?.TryGetC(row.Entity))
                })
            });

            return result;
        }

        public static PredictDictionary PredictBasic(this Lite<PredictorEntity> predictor, PredictDictionary input)
        {
            var pctx = GetPredictContext(predictor);

            return pctx.Algorithm.Predict(pctx, input);
        }

        public static PredictDictionary PredictEntity(this Lite<PredictorEntity> predictor, PredictDictionary input)
        {
            var pctx = GetPredictContext(predictor);

            return pctx.Algorithm.Predict(pctx, input);
        }

        public static PredictDictionary PredictModel(this Lite<PredictorEntity> predictor, PredictDictionary input)
        {
            var pctx = GetPredictContext(predictor);

            return pctx.Algorithm.Predict(pctx, input);
        }
    }
}
