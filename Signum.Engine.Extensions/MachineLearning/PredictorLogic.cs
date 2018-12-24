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
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Signum.Engine.MachineLearning
{
    public class PredictorTrainingState
    {
        internal CancellationTokenSource CancellationTokenSource;
        public PredictorTrainingContext Context; 
    }

    public class PublicationSettings
    {
        public object QueryName;
        public Func<PredictorEntity, Entity> OnPublicate;
    }

    public static class PredictorLogic
    {
        static Expression<Func<PredictorEntity, IQueryable<PredictSimpleResultEntity>>> SimpleResultsExpression =
           e => Database.Query<PredictSimpleResultEntity>().Where(a => a.Predictor.Is(e));
        [ExpressionField]
        public static IQueryable<PredictSimpleResultEntity> SimpleResults(this PredictorEntity e)
        {
            return SimpleResultsExpression.Evaluate(e);
        }

        static Expression<Func<PredictorEntity, IQueryable<PredictorCodificationEntity>>> CodificationsExpression =
        e => Database.Query<PredictorCodificationEntity>().Where(a => a.Predictor.Is(e));
        [ExpressionField]
        public static IQueryable<PredictorCodificationEntity> Codifications(this PredictorEntity e)
        {
            return CodificationsExpression.Evaluate(e);
        }
        
        static Expression<Func<PredictorEntity, IQueryable<PredictorEpochProgressEntity>>> ProgressesExpression =
        e => Database.Query<PredictorEpochProgressEntity>().Where(a => a.Predictor.Is(e));
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

        public static Dictionary<PredictorPublicationSymbol, PublicationSettings> Publications = new Dictionary<PredictorPublicationSymbol, PublicationSettings>();
        public static void RegisterPublication(PredictorPublicationSymbol publication, PublicationSettings settings)
        {
            Publications.Add(publication, settings);
        }

        public static ConcurrentDictionary<Lite<PredictorEntity>, PredictorTrainingState> Trainings = new ConcurrentDictionary<Lite<PredictorEntity>, PredictorTrainingState>();


        public static PredictorTrainingContext GetTrainingContext(Lite<PredictorEntity> lite)
        {
            return Trainings.TryGetC(lite)?.Context;
        }

        public static void Start(SchemaBuilder sb, Func<IFileTypeAlgorithm> predictorFileAlgorithm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Settings.AssertIgnored((PredictorEntity p) => p.MainQuery.Filters.Single().Pinned, "use PredictorLogic", "by calling PredictorLogic.IgnorePinned in Starter.OverrideAttributes");
                sb.Settings.AssertIgnored((PredictorSubQueryEntity p) => p.Filters.Single().Pinned, "use PredictorLogic", "by calling PredictorLogic.IgnorePinned in Starter.OverrideAttributes");

                sb.Include<PredictorEntity>()
                    .WithVirtualMList(p => p.SubQueries, mc => mc.Predictor)
                    .WithQuery(() => e => new
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
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Query,
                        e.Predictor
                    });

                sb.Include<PredictorCodificationEntity>()
                    .WithUniqueIndex(pc => new { pc.Predictor, pc.Index, pc.Usage })
                    .WithExpressionFrom((PredictorEntity e) => e.Codifications())
                    .WithQuery(() => e => new
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
                        e.Min,
                        e.Max,
                        e.Average,
                        e.StdDev,
                    });

                sb.Include<PredictorEpochProgressEntity>()
                    .WithExpressionFrom((PredictorEntity e) => e.EpochProgresses())
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Predictor,
                        e.Id,
                        e.Epoch,
                        e.Ellapsed,
                        e.LossTraining,
                        e.EvaluationTraining,
                        e.LossValidation,
                        e.EvaluationValidation,
                    });

                FileTypeLogic.Register(PredictorFileType.PredictorFile, predictorFileAlgorithm());

                SymbolLogic<PredictorAlgorithmSymbol>.Start(sb, () => Algorithms.Keys);
                SymbolLogic<PredictorColumnEncodingSymbol>.Start(sb, () => Algorithms.Values.SelectMany(a => a.GetRegisteredEncodingSymbols()).Distinct());
                SymbolLogic<PredictorResultSaverSymbol>.Start(sb, () => ResultSavers.Keys);
                SymbolLogic<PredictorPublicationSymbol>.Start(sb, () => Publications.Keys);

                sb.Schema.EntityEvents<PredictorEntity>().Retrieved += PredictorEntity_Retrieved;
                sb.Schema.EntityEvents<PredictorSubQueryEntity>().Retrieved += PredictorMultiColumnEntity_Retrieved;

                Validator.PropertyValidator((PredictorColumnEmbedded c) => c.Encoding).StaticPropertyValidation += Column_StaticPropertyValidation;
                Validator.PropertyValidator((PredictorSubQueryColumnEmbedded c) => c.Token).StaticPropertyValidation += GroupKey_StaticPropertyValidation;
                Validator.PropertyValidator((PredictorSubQueryEntity c) => c.Columns).StaticPropertyValidation += SubQueryColumns_StaticPropertyValidation;

                sb.Include<PredictSimpleResultEntity>()
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Predictor,
                        e.Target,
                        e.Type,
                        e.OriginalValue,
                        e.PredictedValue,
                        e.OriginalCategory,
                        e.PredictedCategory,
                    });

                RegisterResultSaver(PredictorSimpleResultSaver.StatisticsOnly, new PredictorSimpleSaver { SaveAllResults = false });
                RegisterResultSaver(PredictorSimpleResultSaver.Full, new PredictorSimpleSaver { SaveAllResults = true });

                sb.Schema.EntityEvents<PredictorEntity>().PreUnsafeDelete += query =>
                {
                    Database.Query<PredictSimpleResultEntity>().Where(a => query.Contains(a.Predictor.Entity)).UnsafeDelete();
                    return null;
                };

                sb.Schema.WhenIncluded<ProcessEntity>(() =>
                {
                    sb.Schema.Settings.AssertImplementedBy((ProcessEntity p) => p.Data, typeof(PredictorEntity));
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

        public static void IgnorePinned(SchemaBuilder sb)
        {
            sb.Settings.FieldAttributes((PredictorEntity p) => p.MainQuery.Filters.Single().Pinned).Add(new IgnoreAttribute());
            sb.Settings.FieldAttributes((PredictorSubQueryEntity p) => p.Filters.Single().Pinned).Add(new IgnoreAttribute());
        }

        static string SubQueryColumns_StaticPropertyValidation(PredictorSubQueryEntity sq, PropertyInfo pi)
        {
            var p = (PredictorEntity)sq.GetParentEntity();
            var tokens = GetParentKeys(p.MainQuery);
            
            var current = sq.Columns.Where(a => a.Usage == PredictorSubQueryColumnUsage.ParentKey);

            if (tokens.Count != current.Count())
                return PredictorMessage.ThereShouldBe0ColumnsWith12Currently3.NiceToString(
                    tokens.Count,
                    ReflectionTools.GetPropertyInfo((PredictorSubQueryColumnEmbedded c) => c.Usage),
                    PredictorSubQueryColumnUsage.ParentKey.NiceToString(),
                    current.Count());

            return null;
        }

        static string GroupKey_StaticPropertyValidation(PredictorSubQueryColumnEmbedded column, PropertyInfo pi)
        {
            var sq = (PredictorSubQueryEntity)column.GetParentEntity();
            var p = (PredictorEntity)sq.GetParentEntity();
            if (column.Token != null && column.Usage == PredictorSubQueryColumnUsage.ParentKey)
            {
                var index = sq.Columns.Where(a => a.Usage == PredictorSubQueryColumnUsage.ParentKey).IndexOf(column);
                var tokens = GetParentKeys(p.MainQuery);
                var token = tokens.ElementAtOrDefault(index);

                if (token == null)
                    return null;

                if (!Compatible(token, column.Token.Token))
                    return PredictorMessage.TheTypeOf01DoesNotMatch23.NiceToString(column.Token.Token, column.Token.Token.NiceTypeName, token, token.NiceTypeName);
            }

            return null;
        }

        internal static List<QueryToken> GetParentKeys(PredictorMainQueryEmbedded mainQuery)
        {
            if (mainQuery.GroupResults)
                return mainQuery.Columns.Select(a => a.Token.Token).Where(t => !(t is AggregateToken)).ToList();

            var qd = QueryLogic.Queries.QueryDescription(mainQuery.Query.ToQueryName());
            return new List<QueryToken> { QueryUtils.Parse("Entity", qd, 0) };
        }

        public static bool Compatible(QueryToken subQuery, QueryToken mainQuery)
        {
            if (subQuery.Type == mainQuery.Type)
                return true;

            var subQueryImp = subQuery.GetImplementations();
            var mainQueryImp = mainQuery.GetImplementations();

            if (subQueryImp == null || mainQueryImp == null)
                return false;

            if (subQueryImp.Value.IsByAll || mainQueryImp.Value.IsByAll)
                return true;

            if (subQueryImp.Value.Types.Intersect(mainQueryImp.Value.Types).Any())
                return true;

            return false;
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

        static string SubQueryColumn_StaticPropertyValidation(PredictorSubQueryColumnEmbedded column, PropertyInfo pi)
        {
            var sq = (PredictorSubQueryEntity)column.GetParentEntity();
            var p = (PredictorEntity)sq.GetParentEntity();
            if (p.Algorithm == null || column.Usage == PredictorSubQueryColumnUsage.ParentKey || column.Usage == PredictorSubQueryColumnUsage.SplitBy)
                return null;

            var algorithm = Algorithms.GetOrThrow(p.Algorithm);
            var usage = column.Usage == PredictorSubQueryColumnUsage.Input ? PredictorColumnUsage.Input : PredictorColumnUsage.Output;
            return algorithm.ValidateEncodingProperty(p, sq, column.Encoding, usage, column.Token);
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
            using (HeavyProfiler.Log("DoTraining"))
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
                    using (HeavyProfiler.Log("Train"))
                        algorithm.Train(ctx);

                    if (ctx.Predictor.ResultSaver != null)
                    {
                        using (HeavyProfiler.Log("ResultSaver"))
                        {
                            var saver = ResultSavers.GetOrThrow(ctx.Predictor.ResultSaver);
                            saver.SavePredictions(ctx);
                        }
                    }

                    ctx.Predictor.State = PredictorState.Trained;
                    using (OperationLogic.AllowSave<PredictorEntity>())
                        ctx.Predictor.Save();
                }
                catch (OperationCanceledException)
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

        public static PredictorEntity ParseData(this PredictorEntity predictor)
        {
            predictor.MainQuery.ParseData();
            predictor.SubQueries.ForEach(sq => sq.ParseData());
            return predictor;
        }

        public static void ParseData(this PredictorMainQueryEmbedded mainQuery)
        {
            QueryDescription description = QueryLogic.Queries.QueryDescription(mainQuery.Query.ToQueryName());
            mainQuery.ParseData(description);
        }

        static void PredictorEntity_Retrieved(PredictorEntity predictor)
        {
            predictor.MainQuery.ParseData();
        }

        public static void ParseData(this PredictorSubQueryEntity subQuery)
        {
            QueryDescription description = QueryLogic.Queries.QueryDescription(subQuery.Query.ToQueryName());

            subQuery.ParseData(description);
        }

        static void PredictorMultiColumnEntity_Retrieved(PredictorSubQueryEntity subQuery)
        {
            subQuery.ParseData();
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
                    CanBeNew = true,
                    CanBeModified = true,
                    Execute = (e, _) => { },
                }.Register();

                new Execute(PredictorOperation.Train)
                {
                    FromStates = { PredictorState.Draft },
                    ToStates = { PredictorState.Training },
                    CanBeNew = true,
                    CanBeModified = true,
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


                new Execute(PredictorOperation.Publish)
                {
                    CanExecute = p => PredictorLogic.Publications.Values.Any(a => object.Equals(a.QueryName, p.MainQuery.Query.ToQueryName())) ? null : PredictorMessage.NoPublicationsForQuery0Registered.NiceToString(p.MainQuery),
                    FromStates = { PredictorState.Trained },
                    ToStates = { PredictorState.Trained },
                    Execute = (e, arg) =>
                    {
                        var publication = arg.GetArg<PredictorPublicationSymbol>();

                        Database.Query<PredictorEntity>()
                        .Where(a => a.Publication == publication)
                        .UnsafeUpdate()
                        .Set(a => a.Publication, a => null)
                        .Execute();

                        e.Publication = publication;
                        e.Save();
                    },
                }.Register();

                new Graph<Entity>.ConstructFrom<PredictorEntity>(PredictorOperation.AfterPublishProcess)
                {
                    CanConstruct = p => 
                    p.State != PredictorState.Trained ? ValidationMessage._0Or1ShouldBeSet.NiceToString(ReflectionTools.GetPropertyInfo(() => p.State),  p.State.NiceToString()) :                    p.Publication == null ? ValidationMessage._0IsNotSet.NiceToString(ReflectionTools.GetPropertyInfo(() => p.Publication)) :
                    Publications.GetOrThrow(p.Publication).OnPublicate == null ? PredictorMessage.NoPublicationsProcessRegisteredFor0.NiceToString(p.Publication) :
                    null,
                    Construct = (p, _) => Publications.GetOrThrow(p.Publication).OnPublicate(p)
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


