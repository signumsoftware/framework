using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Files;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Processes;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.Processes;
using Signum.Entities.Reflection;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
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
        static Expression<Func<PredictorEntity, IQueryable<PredictSimpleResultEntity>>> SimpleResultsExpression =
           e => Database.Query<PredictSimpleResultEntity>().Where(a => a.Predictor.RefersTo(e));
        [ExpressionField]
        public static IQueryable<PredictSimpleResultEntity> SimpleResults(this PredictorEntity e)
        {
            return SimpleResultsExpression.Evaluate(e);
        }

        static Expression<Func<PredictorEntity, IQueryable<PredictorCodificationEntity>>> CodificationsExpression =
        e => Database.Query<PredictorCodificationEntity>().Where(a => a.Predictor.RefersTo(e));
        [ExpressionField]
        public static IQueryable<PredictorCodificationEntity> Codifications(this PredictorEntity e)
        {
            return CodificationsExpression.Evaluate(e);
        }
        
        static Expression<Func<PredictorEntity, IQueryable<PredictorEpochProgressEntity>>> ProgressesExpression =
        e => Database.Query<PredictorEpochProgressEntity>().Where(a => a.Predictor.RefersTo(e));
        [ExpressionField]
        public static IQueryable<PredictorEpochProgressEntity> EpochProgresses(this PredictorEntity e)
        {
            return ProgressesExpression.Evaluate(e);
        }

        public static Dictionary<PredictorAlgorithmSymbol, IPredictorAlgorithm> Algorithms = new Dictionary<PredictorAlgorithmSymbol, IPredictorAlgorithm>();
        public static void RegisterAlgorithm(PredictorAlgorithmSymbol symbol, IPredictorAlgorithm algorithm)
        {
            Algorithms.Add(symbol, algorithm);
        }

        public static Dictionary<PredictorResultSaverSymbol, IPredictorResultSaver> ResultSavers = new Dictionary<PredictorResultSaverSymbol, IPredictorResultSaver>();
        public static void RegisterResultSaver(PredictorResultSaverSymbol symbol, IPredictorResultSaver algorithm)
        {
            ResultSavers.Add(symbol, algorithm);
        }

        public static ConcurrentDictionary<Lite<PredictorEntity>, PredictorTrainingState> Trainings = new ConcurrentDictionary<Lite<PredictorEntity>, PredictorTrainingState>();


        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, IFileTypeAlgorithm predictorFileAlgorithm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PredictorEntity>()
                    .WithVirtualMList(p => p.SubQueries, mc => mc.Predictor)
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.MainQuery.Query,
                        e.Algorithm,
                        e.State,
                        e.TrainingException,
                    });

                PredictorGraph.Register();

                sb.Include<PredictorSubQueryEntity>()
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Query,
                        e.Predictor
                    });

                sb.Include<PredictorCodificationEntity>()
                    .WithUniqueIndex(pc => new { pc.Predictor, pc.Index, pc.Usage })
                    .WithExpressionFrom(dqm, (PredictorEntity e) => e.Codifications())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Predictor,
                        e.Index,
                        e.Usage,
                        e.OriginalColumnIndex,
                        e.SubQueryIndex,
                        e.SplitKey0,
                        e.SplitKey1,
                        e.SplitKey2,
                        e.IsValue,
                        e.Mean,
                        e.StdDev,
                    });

                sb.Include<PredictorEpochProgressEntity>()
                    .WithExpressionFrom(dqm, (PredictorEntity e) => e.EpochProgresses())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Predictor,
                        e.Id,
                        e.Epoch,
                        e.Ellapsed,
                        e.LossTraining,
                        e.LossValidation,
                        e.EvaluationTraining,
                        e.EvaluationValidation,
                    });

                FileTypeLogic.Register(PredictorFileType.PredictorFile, predictorFileAlgorithm);

                SymbolLogic<PredictorAlgorithmSymbol>.Start(sb, dqm, () => Algorithms.Keys);
                SymbolLogic<PredictorResultSaverSymbol>.Start(sb, dqm, () => ResultSavers.Keys);

                sb.Schema.EntityEvents<PredictorEntity>().Retrieved += PredictorEntity_Retrieved;
                sb.Schema.EntityEvents<PredictorSubQueryEntity>().Retrieved += PredictorMultiColumnEntity_Retrieved;

                Validator.PropertyValidator((PredictorColumnEmbedded c) => c.Encoding).StaticPropertyValidation += Column_StaticPropertyValidation;
                Validator.PropertyValidator((PredictorSubQueryColumnEmbedded c) => c.Token).StaticPropertyValidation += GroupKey_StaticPropertyValidation;

                sb.Include<PredictSimpleResultEntity>()
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Predictor,
                        e.Target,
                        e.Type,
                        e.PredictedValue,
                        e.PredictedCategory,
                    });
                RegisterResultSaver(PredictorSimpleResultSaver.OneOutput, new PredictorSimpleSaver());
                sb.Schema.EntityEvents<PredictorEntity>().PreUnsafeDelete += query => Database.Query<PredictSimpleResultEntity>().Where(a => query.Contains(a.Predictor.Entity)).UnsafeDelete();

                sb.Schema.WhenIncluded<ProcessEntity>(() =>
                {
                    sb.Schema.Settings.AssertImplementedBy((ProcessEntity p) => p.Data, typeof(AutoconfigureNeuralNetworkEntity));
                    ProcessLogic.Register(PredictorProcessAlgorithm.AutoconfigureNeuralNetwork, new AutoconfigureNeuralNetworkAlgorithm());

                    new Graph<ProcessEntity>.ConstructFrom<PredictorEntity>(PredictorOperation.AutoconfigureNetwork)
                    {
                        CanConstruct = p => p.AlgorithmSettings is NeuralNetworkSettingsEntity ? null : ValidationMessage._0ShouldBeOfType1.NiceToString(p.NicePropertyName(_ => _.AlgorithmSettings), typeof(NeuralNetworkSettingsEntity).NiceName()),
                        Construct = (p, _) =>
                        {
                            return ProcessLogic.Create(PredictorProcessAlgorithm.AutoconfigureNeuralNetwork, new AutoconfigureNeuralNetworkEntity
                            {
                                InitialPredictor = p.ToLite()
                            });
                        }
                    }.Register();
                });
            }
        }

        static string GroupKey_StaticPropertyValidation(PredictorSubQueryColumnEmbedded column, PropertyInfo pi)
        {
            var sq = (PredictorSubQueryEntity)column.GetParentEntity();
            var p = (PredictorEntity)sq.GetParentEntity();
            if(column.Token != null && column.Usage == PredictorSubQueryColumnUsage.ParentKey)
            {
                Implementations mainQueryImplementations = DynamicQueryManager.Current.GetEntityImplementations(p.MainQuery.Query.ToQueryName());
                var token = column.Token.Token;
                if (!Compatible(token.GetImplementations(), mainQueryImplementations))
                    return PredictorMessage.ParentKeyOf0ShouldBeOfType1.NiceToString(sq, mainQueryImplementations, token.GetImplementations()?.ToString() ?? token.NiceTypeName);
            }

            return null;
        }

        public static bool Compatible(Implementations? firstGroupKey, Implementations mainQuery)
        {
            if (firstGroupKey == null)
                return false;

            if (firstGroupKey.Value.IsByAll ||
                mainQuery.IsByAll)
                return false;

            if (firstGroupKey.Value.Types.Count() != 1 ||
                mainQuery.Types.Count() != 1)
                return false;

            return firstGroupKey.Value.Types.SingleEx().Equals(mainQuery.Types.SingleEx());
        }

        static string Column_StaticPropertyValidation(PredictorColumnEmbedded column, PropertyInfo pi)
        {
            var mq = (PredictorMainQueryEmbedded)column.GetParentEntity();
            var p = (PredictorEntity)mq.GetParentEntity();
            if (p.Algorithm == null)
                return null;

            var algorithm = Algorithms.GetOrThrow(p.Algorithm);
            return algorithm.ValidateEncodingProperty(p, null, column.Encoding, column.Usage, column.Token);
        }

        static string Column_StaticPropertyValidation(PredictorSubQueryColumnEmbedded column, PropertyInfo pi)
        {
            var sq = (PredictorSubQueryEntity)column.GetParentEntity();
            var p = (PredictorEntity)sq.GetParentEntity();
            if (p.Algorithm == null || column.Usage == PredictorSubQueryColumnUsage.ParentKey || column.Usage == PredictorSubQueryColumnUsage.SplitBy)
                return null;
            var algorithm = Algorithms.GetOrThrow(p.Algorithm);
            var usage = column.Usage == PredictorSubQueryColumnUsage.Input ? PredictorColumnUsage.Input : PredictorColumnUsage.Output;
            return algorithm.ValidateEncodingProperty(p, sq, column.Encoding.Value, usage, column.Token);
        }

        public static void TrainSync(this PredictorEntity p, bool autoReset = true, Action<string, decimal?> onReportProgres = null, CancellationToken? cancellationToken = null)
        {
            if(autoReset)
            {
                if (p.State == PredictorState.Trained || p.State == PredictorState.Error)
                    p.Execute(PredictorOperation.Untrain);
                else if(p.State == PredictorState.Training)
                    p.Execute(PredictorOperation.CancelTraining);
            }

            p.User = UserHolder.Current.ToLite();
            p.State = PredictorState.Training;
            p.Save();
            
            var ctx = new PredictorTrainingContext(p, cancellationToken ?? new CancellationTokenSource().Token);
            var lastWithProgress = false;

            if (onReportProgres != null)
                ctx.OnReportProgres += onReportProgres;
            else
                ctx.OnReportProgres += (message, progress) =>
                {
                    if (progress == null)
                    {
                        if (lastWithProgress)
                            Console.WriteLine();
                        Console.WriteLine(message);
                    }
                    else
                    {
                        SafeConsole.WriteSameLine($"{progress:P} - {message}");
                        lastWithProgress = true;
                    }
                };
            DoTraining(ctx);
        }

        static void StartTrainingAsync(PredictorEntity p)
        {
            var cancellationSource = new CancellationTokenSource();

            var ctx = new PredictorTrainingContext(p, cancellationSource.Token);

            var state = new PredictorTrainingState
            {
                CancellationTokenSource = cancellationSource,
                Context = ctx
            };

            if (!Trainings.TryAdd(p.ToLite(), state))
                throw new InvalidOperationException(PredictorMessage._0IsAlreadyBeingTrained.NiceToString(p));

            using (ExecutionContext.SuppressFlow())
            {
                Task.Run(() =>
                {
                    var user = ExecutionMode.Global().Using(_ => p.User.Retrieve());
                    using (UserHolder.UserSession(user))
                    {
                        try
                        {
                            DoTraining(ctx);
                        }
                        finally
                        {
                            Trainings.TryRemove(p.ToLite(), out var _);
                        }
                    }
                });
            }
        }

        static void DoTraining(PredictorTrainingContext ctx)
        {
            try
            {
                if (ctx.Predictor.ResultSaver != null)
                {
                    var saver = ResultSavers.GetOrThrow(ctx.Predictor.ResultSaver);
                    saver.AssertValid(ctx.Predictor);
                }

                PredictorLogicQuery.RetrieveData(ctx);
                PredictorCodificationLogic.CreatePredictorCodifications(ctx);
                var algorithm = Algorithms.GetOrThrow(ctx.Predictor.Algorithm);
                algorithm.Train(ctx);

                if(ctx.Predictor.ResultSaver != null)
                {
                    var saver = ResultSavers.GetOrThrow(ctx.Predictor.ResultSaver);
                    saver.SavePredictions(ctx);
                }

                ctx.Predictor.State = PredictorState.Trained;
                using (OperationLogic.AllowSave<PredictorEntity>())
                    ctx.Predictor.Save();
            }
            catch (OperationCanceledException e)
            {
                var p = ctx.Predictor.ToLite().RetrieveAndForget();
                CleanTrained(p);
                p.State = PredictorState.Draft;
                using (OperationLogic.AllowSave<PredictorEntity>())
                    p.Save();
            }
            catch (Exception ex)
            {
                ex.Data["entity"] = ctx.Predictor;
                var e = ex.LogException();
                var p = ctx.Predictor.ToLite().RetrieveAndForget();
                p.State = PredictorState.Error;
                p.TrainingException = e.ToLite();
                using (OperationLogic.AllowSave<PredictorEntity>())
                    p.Save();
            }
        }

        static void CleanTrained(PredictorEntity e)
        {
            PredictorPredictLogic.TrainedPredictorCache.Remove(e.ToLite());
            e.TrainingException = null;
            foreach (var fp in e.Files)
            {
                fp.DeleteFileOnCommit();
            }
            e.ClassificationTraining = null;
            e.ClassificationValidation = null;
            e.RegressionTraining = null;
            e.RegressionValidation = null;
            e.Files.Clear();
            e.Codifications().UnsafeDelete();
            e.EpochProgresses().UnsafeDelete();
        }

       

        public static byte[] GetTsvMetadata(this PredictorEntity predictor)
        {
            return null;
            //var ctx = new PredictorTrainingContext(predictor, CancellationToken.None);
            //PredictorLogicQuery.RetrieveData(ctx);
            //return Tsv.ToTsvBytes(ctx.AllRows.Rows.Take(1).ToArray());
        }

        public static byte[] GetTsv(this PredictorEntity predictor)
        {
            return null;
            //var ctx = new PredictorTrainingContext(predictor, CancellationToken.None);
            //PredictorLogicQuery.RetrieveData(ctx);
            //return Tsv.ToTsvBytes(ctx.AllRows.Rows);
        }

        public static byte[] GetCsv(this PredictorEntity predictor)
        {
            return null;
            //var ctx = new PredictorTrainingContext(predictor, CancellationToken.None);
            //PredictorLogicQuery.RetrieveData(ctx);
            //return Csv.ToCsvBytes(ctx.AllRows.Rows);
        }

        static void PredictorEntity_Retrieved(PredictorEntity predictor)
        {
            object queryName = QueryLogic.ToQueryName(predictor.MainQuery.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);
            predictor.MainQuery.ParseData(description);
        }

        static void PredictorMultiColumnEntity_Retrieved(PredictorSubQueryEntity mc)
        {
            object queryName = QueryLogic.ToQueryName(mc.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            mc.ParseData(description);
        }


        public class PredictorGraph : Graph<PredictorEntity, PredictorState>
        {
            public static void Register()
            {
                GetState = f => f.State;

                new Execute(PredictorOperation.Save)
                {
                    FromStates = { PredictorState.Draft, PredictorState.Error, PredictorState.Trained },
                    ToStates = { PredictorState.Draft, PredictorState.Error, PredictorState.Trained },
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
                    Execute = (p, _) =>
                    {
                        p.User = UserHolder.Current.ToLite();
                        p.State = PredictorState.Training;
                        p.Save();

                        StartTrainingAsync(p);
                    },
                }.Register();

                new Execute(PredictorOperation.Untrain)
                {
                    FromStates = { PredictorState.Trained, PredictorState.Error },
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
                    ToStates = { PredictorState.Training, PredictorState.Draft },
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) =>
                    {
                        if (Trainings.TryGetValue(e.ToLite(), out var state))
                        {
                            state.CancellationTokenSource.Cancel();
                        }
                        else
                        {
                            CleanTrained(e);
                            e.State = PredictorState.Draft;
                            e.Save();
                        }
                    },
                }.Register();

                new Execute(PredictorOperation.StopTraining)
                {
                    FromStates = { PredictorState.Training },
                    ToStates = { PredictorState.Training },
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) =>
                    {
                        if (Trainings.TryGetValue(e.ToLite(), out var state))
                        {
                            state.Context.StopTraining = true;
                        }

                        if (GraphExplorer.IsGraphModified(e))
                            throw new InvalidOperationException();
                    },
                }.Register();


                new Delete(PredictorOperation.Delete)
                {
                    FromStates = { PredictorState.Draft, PredictorState.Trained },
                    Delete = (e, _) =>
                    {
                        foreach (var fp in e.Files)
                        {
                            fp.DeleteFileOnCommit();
                        }
                        e.EpochProgresses().UnsafeDelete();
                        e.Codifications().UnsafeDelete();
                        e.Delete();
                    },
                }.Register();

                new Graph<PredictorEntity>.ConstructFrom<PredictorEntity>(PredictorOperation.Clone)
                {
                    Construct = (e, _) => new PredictorEntity
                    {
                        Name = e.Name.HasText() ? (e.Name + " (2)") : "",
                        State = PredictorState.Draft,
                        Algorithm = e.Algorithm,
                        ResultSaver = e.ResultSaver,
                        MainQuery = e.MainQuery.Clone(),
                        SubQueries = e.SubQueries.Select(a => a.Clone()).ToMList(),
                        AlgorithmSettings = e.AlgorithmSettings?.Clone(),
                        Settings = e.Settings?.Clone(),
                    },
                }.Register();
            }
        }
    }
}


