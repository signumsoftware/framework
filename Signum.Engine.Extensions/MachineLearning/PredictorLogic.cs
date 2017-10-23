using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Files;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.MachineLearning
{
    public class PredictorTrainingState
    {
        public CancellationTokenSource CancellationTokenSource;
        public PredictorTrainingContext Context; 
    }

    public static class PredictorLogic
    {
        static Expression<Func<PredictorEntity, IQueryable<PredictorCodificationEntity>>> CodificationsExpression =
        e => Database.Query<PredictorCodificationEntity>().Where(a => a.Predictor.RefersTo(e));
        [ExpressionField]
        public static IQueryable<PredictorCodificationEntity> Codifications(this PredictorEntity e)
        {
            return CodificationsExpression.Evaluate(e);
        }

        public static Dictionary<PredictorAlgorithmSymbol, PredictorAlgorithm> Algorithms = new Dictionary<PredictorAlgorithmSymbol, PredictorAlgorithm>();
        public static void RegisterAlgorithm(PredictorAlgorithmSymbol symbol, PredictorAlgorithm algorithm)
        {
            Algorithms.Add(symbol, algorithm);
        }

        public static ConcurrentDictionary<Lite<PredictorEntity>, PredictorTrainingState> Trainings = new ConcurrentDictionary<Lite<PredictorEntity>, PredictorTrainingState>();

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PredictorEntity>()
                    .WithVirtualMList(p => p.MultiColumns, mc => mc.Predictor)
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Query,
                        InputCount = e.Filters.Count,
                        OutputCount = e.Filters.Count,
                    });

                PredictorGraph.Register();

                sb.Include<PredictorMultiColumnEntity>()
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Query,
                        e.Predictor
                    });

                sb.Include<PredictorCodificationEntity>()
                    .WithUniqueIndex(pc => new { pc.Predictor, pc.ColumnIndex })
                    .WithExpressionFrom(dqm, (PredictorEntity e) => e.Codifications())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Predictor,
                        e.ColumnIndex,
                        e.OriginalColumnIndex,
                        e.GroupKey0,
                        e.GroupKey1,
                        e.GroupKey2,
                        e.IsValue,
                    });

                SymbolLogic<PredictorAlgorithmSymbol>.Start(sb, dqm, () => Algorithms.Keys);

                sb.Schema.EntityEvents<PredictorEntity>().Retrieved += PredictorEntity_Retrieved;
                sb.Schema.EntityEvents<PredictorMultiColumnEntity>().Retrieved += PredictorMultiColumnEntity_Retrieved;
            }
        }

        public class PredictorGraph : Graph<PredictorEntity, PredictorState>
        {
            public static void Register()
            {
                GetState = f => f.State;

                new Execute(PredictorOperation.Save)
                {
                    FromStates = { PredictorState.Draft },
                    ToStates = { PredictorState.Draft },
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();
                
                new Execute(PredictorOperation.Train)
                {
                    FromStates = { PredictorState.Draft },
                    ToStates = { PredictorState.Training },
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => {
                        var state = Trainings.TryGetC(e.ToLite());

                        if (state != null)
                            throw new InvalidOperationException(PredictorMessage._0IsAlreadyBeingTrained.NiceToString(e));

                        var cancellationSource = new CancellationTokenSource();

                        var state = new PredictorTrainingState
                        {
                            CancellationTokenSource = cancellationSource,
                            Context = new PredictorTrainingContext(e, cancellationSource.Token)
                        };

                        Task.Run(() =>
                        {
                            var rows = PredictorLogicQuery.RetrieveData(e, out var columns);

                            var output =  



                            

                        });

                        Trainings.TryAdd(state);


                        state.CancellationTokenSource.Cancel();
                    },
                }.Register();

                new Execute(PredictorOperation.Untrain)
                {
                    FromStates = { PredictorState.Trained },
                    ToStates = { PredictorState.Draft },
                    Execute = (e, _) =>
                    {
                        CleanTrained(e);
                        e.State = PredictorState.Draft;
                    },
                }.Register();

                new Execute(PredictorOperation.CancelTraining)
                {
                    FromStates = { PredictorState.Training },
                    ToStates = { PredictorState.Draft },
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => {
                        
                    },
                }.Register();


                new Delete(PredictorOperation.Delete)
                {
                    FromStates = { PredictorState.Draft, PredictorState.Trained },
                    Delete = (e, _) =>
                    {
                        e.Codifications().UnsafeDelete();
                        e.Delete();
                    },
                }.Register();

                new ConstructFrom<PredictorEntity>(PredictorOperation.Clone)
                {
                    Construct = (e, _) => new PredictorEntity
                    {
                        Name = e.Name.HasText() ? (e.Name + " (2)") : "",
                        State = e.State,
                        Query = e.Query,
                        Algorithm = e.Algorithm,
                        AlgorithmSettings = e.AlgorithmSettings?.Clone(),
                        Settings = e.Settings?.Clone(),
                        Filters = e.Filters.Select(f => f.Clone()).ToMList(),
                        SimpleColumns = e.SimpleColumns.Select(a => a.Clone()).ToMList(),
                        MultiColumns = e.MultiColumns.Select(a => a.Clone()).ToMList()
                    },
                }.Register();
            }

            private static void CleanTrained(PredictorEntity e)
            {
                foreach (var fp in e.Files)
                {
                    fp.DeleteFileOnCommit();
                }
                e.Files.RemoveAll();
                e.Codifications().UnsafeDelete();
            }
        }

        public static void TrainPredictor(PredictorEntity predictor)
        {
            var design = PredictorLogicQuery.RetrieveData(predictor, out List<PredictorResultColumn> columnDescriptions);

            predictor.Codifications().UnsafeDelete();
            CreatePredictorCodifications(predictor, columnDescriptions);
        }

        private static void CreatePredictorCodifications(PredictorEntity predictor, List<PredictorResultColumn> columnDescriptions)
        {
            columnDescriptions.Select((a, i) =>
            {
                string GetGroupKey(int index)
                {
                    var token = a.MultiColumn?.Aggregates.ElementAtOrDefault(index)?.Token;
                    if (token == null)
                        return null;

                    var obj = a.Keys.ElementAtOrDefault(index);

                    if (obj == null)
                        return null;

                    return FilterValueConverter.ToString(obj, token.Token.Type, allowSmart: false);
                }

                return new PredictorCodificationEntity
                {
                    Predictor = predictor.ToLite(),
                    ColumnIndex = i,
                    OriginalMultiColumnIndex = a.MultiColumn == null ? (int?)null : predictor.MultiColumns.IndexOf(a.MultiColumn),
                    OriginalColumnIndex = a.MultiColumn == null ? predictor.SimpleColumns.IndexOf(a.PredictorColumn) : a.MultiColumn.Aggregates.IndexOf(a.PredictorColumn),
                    GroupKey0 = GetGroupKey(0),
                    GroupKey1 = GetGroupKey(1),
                    GroupKey2 = GetGroupKey(2),
                    IsValue = GetGroupKey(2),
                };
            }).BulkInsert();
        }

        public static byte[] GetTsvMetadata(this PredictorEntity predictor)
        {
            return new byte[0];
        }

        public static byte[] GetTsv(this PredictorEntity predictor)
        {
            List<PredictorResultColumn> columnDescriptions;
            return Tsv.ToTsvBytes(PredictorLogicQuery.RetrieveData(predictor, out columnDescriptions));
        }

        public static byte[] GetCsv(this PredictorEntity predictor)
        {
            List<PredictorResultColumn> columnDescriptions;
            return Csv.ToCsvBytes(PredictorLogicQuery.RetrieveData(predictor, out columnDescriptions));
        }

        static void PredictorEntity_Retrieved(PredictorEntity predictor)
        {
            object queryName = QueryLogic.ToQueryName(predictor.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            predictor.ParseData(description);
        }

        static void PredictorMultiColumnEntity_Retrieved(PredictorMultiColumnEntity mc)
        {
            object queryName = QueryLogic.ToQueryName(mc.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            mc.ParseData(description);
        }
    }


}


class MultiColumnQuery
{
    public PredictorMultiColumnEntity MultiColumn;
    public QueryGroupRequest QueryGroupRequest;
    public ResultTable ResultTable;
    public Dictionary<Lite<Entity>, Dictionary<object[], object[]>> GroupedValues;

    public ResultColumn[] Aggregates { get; internal set; }
}
