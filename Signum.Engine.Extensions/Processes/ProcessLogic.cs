using Signum.Entities.Processes;
using Signum.Engine.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities.Reflection;
using Signum.Entities.Authorization;

namespace Signum.Engine.Processes;

public static class ProcessLogic
{
    public static bool JustMyProcesses = true;

    public static Func<ProcessEntity, IDisposable?>? ApplySession;

    [AutoExpressionField]
    public static IQueryable<ProcessEntity> Processes(this ProcessAlgorithmSymbol p) => 
        As.Expression(() => Database.Query<ProcessEntity>().Where(a => a.Algorithm.Is(p)));

    [AutoExpressionField]
    public static ProcessEntity? LastProcess(this ProcessAlgorithmSymbol p) => 
        As.Expression(() => p.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault());

    [AutoExpressionField]
    public static IQueryable<ProcessExceptionLineEntity> ExceptionLines(this ProcessEntity p) => 
        As.Expression(() => Database.Query<ProcessExceptionLineEntity>().Where(a => a.Process.Is(p)));

    [AutoExpressionField]
    public static IQueryable<ProcessExceptionLineEntity> ExceptionLines(this IProcessLineDataEntity pl) => 
        As.Expression(() => Database.Query<ProcessExceptionLineEntity>().Where(a => a.Line.Is(pl)));

    [AutoExpressionField]
    public static ExceptionEntity? Exception(this IProcessLineDataEntity pl, ProcessEntity p) =>
        As.Expression(() => p.ExceptionLines().SingleOrDefault(el => el.Line.Is(pl))!.Exception.Entity);

    [AutoExpressionField]
    public static IQueryable<ProcessEntity> Processes(this IProcessDataEntity e) =>
        As.Expression(() => Database.Query<ProcessEntity>().Where(a => a.Data == e));

    [AutoExpressionField]
    public static ProcessEntity? LastProcess(this IProcessDataEntity e) => 
        As.Expression(() => e.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault());

    static Dictionary<ProcessAlgorithmSymbol, IProcessAlgorithm> registeredProcesses = new Dictionary<ProcessAlgorithmSymbol, IProcessAlgorithm>();

    public static void AssertStarted(SchemaBuilder sb)
    {
        sb.AssertDefined(ReflectionTools.GetMethodInfo(() => ProcessLogic.Start(null!)));
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<ProcessAlgorithmSymbol>()
                .WithQuery(() => pa => new
                {
                    Entity = pa,
                    pa.Id,
                    pa.Key
                });

            sb.Include<ProcessEntity>()
                .WithQuery(() => p => new
                {
                    Entity = p,
                    p.Id,
                    p.Algorithm,
                    p.Data,
                    p.State,
                    p.MachineName,
                    p.ApplicationName,
                    p.CreationDate,
                    p.PlannedDate,
                    p.CancelationDate,
                    p.QueuedDate,
                    p.ExecutionStart,
                    p.ExecutionEnd,
                    p.SuspendDate,
                    p.ExceptionDate,
                });

            sb.Include<ProcessExceptionLineEntity>()
                .WithQuery(() => p => new
                {
                    Entity = p,
                    p.Process,
                    p.Line,
                    ((PackageLineEntity)p.Line!.Entity).Target,
                    ((PackageLineEntity)p.Line!.Entity).Result,
                    p.ElementInfo,
                    p.Exception,
                });

            PermissionAuthLogic.RegisterPermissions(ProcessPermission.ViewProcessPanel);

            SymbolLogic<ProcessAlgorithmSymbol>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

            OperationLogic.AssertStarted(sb);

            ProcessGraph.Register();

            QueryLogic.Expressions.Register((ProcessAlgorithmSymbol p) => p.Processes(), () => typeof(ProcessEntity).NicePluralName());
            QueryLogic.Expressions.Register((ProcessAlgorithmSymbol p) => p.LastProcess(), ProcessMessage.LastProcess);

            QueryLogic.Expressions.Register((IProcessDataEntity p) => p.Processes(), () => typeof(ProcessEntity).NicePluralName());
            QueryLogic.Expressions.Register((IProcessDataEntity p) => p.LastProcess(), ProcessMessage.LastProcess);

            QueryLogic.Expressions.Register((ProcessEntity p) => p.ExceptionLines(), ProcessMessage.ExceptionLines);
            QueryLogic.Expressions.Register((IProcessLineDataEntity p) => p.ExceptionLines(), ProcessMessage.ExceptionLines);

            PropertyAuthLogic.SetMaxAutomaticUpgrade(PropertyRoute.Construct((ProcessEntity p) => p.User), PropertyAllowed.Read);

            ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
        }
    }

    public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEmbedded parameters, StringBuilder sb, CancellationToken token)
    {
        void Remove(ProcessState processState, DateTime dateLimit, bool withExceptions)
        {
            var query = Database.Query<ProcessEntity>().Where(p => p.State == processState && p.CreationDate < dateLimit);

            if (withExceptions)
                query = query.Where(p => p.Exception != null);

            query.SelectMany(a => a.ExceptionLines()).UnsafeDeleteChunksLog(parameters, sb, token);
            query.Where(a => !a.ExceptionLines().Any()).UnsafeDeleteChunksLog(parameters, sb, token);
        }

        var dateLimit = parameters.GetDateLimitDelete(typeof(ProcessEntity).ToTypeEntity());
        if (dateLimit != null)
        {
            Remove(ProcessState.Canceled, dateLimit.Value, withExceptions: false);
            Remove(ProcessState.Finished, dateLimit.Value, withExceptions: false);
            Remove(ProcessState.Error, dateLimit.Value, withExceptions: false);
        }

        dateLimit = parameters.GetDateLimitDeleteWithExceptions(typeof(ProcessEntity).ToTypeEntity());
        if (dateLimit != null)
        {
            Remove(ProcessState.Canceled, dateLimit.Value, withExceptions: true);
            Remove(ProcessState.Finished, dateLimit.Value, withExceptions: true);
            Remove(ProcessState.Error, dateLimit.Value, withExceptions: true);
        }
    }

    public static IDisposable? OnApplySession(ProcessEntity process)
    {
        return Disposable.Combine(ApplySession, f => f(process));
    }


    public static void Register(ProcessAlgorithmSymbol processAlgorithm, Action<ExecutingProcess> action) =>
        Register(processAlgorithm, new ActionProcessAlgorithm(action));

    public static void Register(ProcessAlgorithmSymbol processAlgorithm, IProcessAlgorithm algorithm)
    {
        if (processAlgorithm == null)
            throw AutoInitAttribute.ArgumentNullException(typeof(ProcessAlgorithmSymbol), nameof(processAlgorithm));

        if (algorithm == null)
            throw new ArgumentNullException(nameof(algorithm));

        registeredProcesses.Add(processAlgorithm, algorithm);
    }

    public class ProcessGraph : Graph<ProcessEntity, ProcessState>
    {
        public static void Register()
        {
            GetState = e => e.State;

            new Execute(ProcessOperation.Save)
            {
                FromStates = { ProcessState.Created },
                ToStates = { ProcessState.Created },
                CanBeNew = true,
                CanBeModified = true,
                Execute = (p, args) =>
                {
                    p.Save();
                }
            }.Register();

         
            new Execute(ProcessOperation.Execute)
            {
                FromStates = { ProcessState.Created, ProcessState.Planned, ProcessState.Canceled, ProcessState.Suspended },
                ToStates = { ProcessState.Queued },
                Execute = (p, _) =>
                {
                    p.MachineName = JustMyProcesses ? Schema.Current.MachineName : ProcessEntity.None;
                    p.ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName : ProcessEntity.None;

                    p.SetAsQueued();

                    Transaction.PostRealCommit -= ProcessRunner.WakeupExecuteInThisMachine;
                    Transaction.PostRealCommit += ProcessRunner.WakeupExecuteInThisMachine;
                }
            }.Register();

            new Execute(ProcessOperation.Suspend)
            {
                FromStates = { ProcessState.Executing },
                ToStates = { ProcessState.Suspending },
                Execute = (p, _) =>
                {
                    p.State = ProcessState.Suspending;
                    p.SuspendDate = Clock.Now;
                }
            }.Register();

            new Execute(ProcessOperation.Cancel)
            {
                FromStates = { ProcessState.Planned, ProcessState.Created, ProcessState.Suspended, ProcessState.Queued, ProcessState.Executing, ProcessState.Suspending },
                ToStates = { ProcessState.Canceled },
                Execute = (p, _) =>
                {
                    p.State = ProcessState.Canceled;
                    p.CancelationDate = Clock.Now;
                }
            }.Register();

            new Execute(ProcessOperation.Plan)
            {
                FromStates = { ProcessState.Created, ProcessState.Canceled, ProcessState.Planned, ProcessState.Suspended },
                ToStates = { ProcessState.Planned },
                Execute = (p, args) =>
                {
                    p.MachineName = JustMyProcesses ? Schema.Current.MachineName : ProcessEntity.None;
                    p.ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName : ProcessEntity.None;

                    p.State = ProcessState.Planned;
                    p.PlannedDate = args.GetArg<DateTime>();
                }
            }.Register();

            new ConstructFrom<ProcessEntity>(ProcessOperation.Retry)
            {
                CanConstruct = p => p.State.InState(ProcessState.Error, ProcessState.Canceled, ProcessState.Finished, ProcessState.Suspended),
                ToStates = { ProcessState.Created },
                Construct = (p, _) => p.Algorithm.Create(p.Data, p)
            }.Register();
        }
    }

    public static ProcessEntity Create(this ProcessAlgorithmSymbol process, IProcessDataEntity? processData, Entity? copyMixinsFrom = null)
    {
        using (OperationLogic.AllowSave<ProcessEntity>())
        {
            var result = new ProcessEntity(process)
            {
                State = ProcessState.Created,
                Data = processData,
                MachineName = JustMyProcesses ? Schema.Current.MachineName : ProcessEntity.None,
                ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName : ProcessEntity.None,
                User = UserHolder.Current.User,
            };

            if (copyMixinsFrom != null)
                process.CopyMixinsFrom(copyMixinsFrom);

            return result.Save();
        }
    }

    public static void ExecuteTest(this ProcessEntity p, bool writeToConsole = false)
    {
        p.QueuedDate = Clock.Now;
        var ep = new ExecutingProcess(GetProcessAlgorithm(p.Algorithm), p)
        {
            WriteToConsole = writeToConsole
        };

        ep.TakeForThisMachine();
        ep.Execute();
    }

    public static IProcessAlgorithm GetProcessAlgorithm(ProcessAlgorithmSymbol processAlgorithm)
    {
        return registeredProcesses.GetOrThrow(processAlgorithm, "The process algorithm {0} is not registered");
    }

    public static void ForEachLine<T>(this ExecutingProcess executingProcess, IQueryable<T> remainingLines, Action<T> action, int groupsOf = 100)
        where T : Entity, IProcessLineDataEntity, new()
    {
        var remainingNotExceptionsLines = remainingLines.Where(li => li.Exception(executingProcess.CurrentProcess) == null);

        var totalCount = remainingNotExceptionsLines.Count();
        int j = 0;
        executingProcess.ProgressChanged(0, totalCount);
        while (true)
        {
            List<T> lines = remainingNotExceptionsLines.Take(groupsOf).ToList();
            if (lines.IsEmpty())
                return;

            for (int i = 0; i < lines.Count; i++)
            {
                executingProcess.CancellationToken.ThrowIfCancellationRequested();

                T pl = lines[i];

                using (HeavyProfiler.Log("ProcessLine", () => pl.ToString()))
                {
                    try
                    {
                        Transaction.ForceNew().EndUsing(tr =>
                        {
                            action(pl);
                            tr.Commit();
                        });
                    }
                    catch (Exception e)
                    {
                        if (Transaction.InTestTransaction)
                            throw;

                        var exLog = e.LogException();

                        Transaction.ForceNew().EndUsing(tr =>
                        {
                            new ProcessExceptionLineEntity
                            {
                                Exception = exLog.ToLite(),
                                Line = pl.ToLite(),
                                Process = executingProcess.CurrentProcess.ToLite()
                            }.Save();

                            tr.Commit();
                        });
                    }

                    j++;
                    executingProcess.ProgressChanged(j, totalCount);
                }
            }
        }
    }

    public static void ForEach<T>(this ExecutingProcess executingProcess, List<T> collection,
        Func<T, string> elementInfo, Action<T> action, string? status = null)
    {
        if (executingProcess == null)
        {
            collection.ProgressForeach(elementInfo, action);
        }
        else
        {
            executingProcess.ForEachNonTransactional(collection, elementInfo, item => {
                using (var tr = Transaction.ForceNew())
                {
                    action(item);
                    tr.Commit();
                }
            }, status);
        }
    }


    public static void ForEachNonTransactional<T>(this ExecutingProcess executingProcess, List<T> collection,
        Func<T, string> elementInfo, Action<T> action, string? status = null)
    {
        if (executingProcess == null)
        {
            collection.ProgressForeach(elementInfo, action, transactional: false);
        }
        else
        {

            var totalCount = collection.Count;
            int j = 0;
            foreach (var item in collection)
            {
                executingProcess.CancellationToken.ThrowIfCancellationRequested();
                using (HeavyProfiler.Log("ProgressForeach", () => elementInfo(item)))
                {
                    try
                    {
                        action(item);
                    }
                    catch (Exception e)
                    {
                        if (Transaction.InTestTransaction)
                            throw;

                        var exLog = e.LogException();

                        Transaction.ForceNew().EndUsing(tr =>
                        {
                            new ProcessExceptionLineEntity
                            {
                                Exception = exLog.ToLite(),
                                ElementInfo = elementInfo(item),
                                Process = executingProcess.CurrentProcess.ToLite()
                            }.Save();

                            tr.Commit();
                        });
                    }

                    executingProcess.ProgressChanged(j++, totalCount, status);
                }
            }

        }
    }

    public static void Synchronize<K, N, O>(this ExecutingProcess ep,
        Dictionary<K, N> newDictionary,
        Dictionary<K, O> oldDictionary,
        Action<K, N> createNew,
        Action<K, O> removeOld,
        Action<K, N, O> merge)
        where K : notnull
    {
        HashSet<K> keys = new HashSet<K>();
        keys.UnionWith(oldDictionary.Keys);
        keys.UnionWith(newDictionary.Keys);

        foreach (var key in keys)
        {
            var oldExists = oldDictionary.TryGetValue(key, out var oldVal);
            var newExists = newDictionary.TryGetValue(key, out var newVal);

            if (!oldExists)
            {
                createNew?.Invoke(key, newVal!);
            }
            else if (!newExists)
            {
                removeOld?.Invoke(key, oldVal!);
            }
            else
            {
                merge?.Invoke(key, newVal!, oldVal!);
            }
        }
    }

    public static void WriteLineColor(this ExecutingProcess? ep, ConsoleColor color, string? str)
    {
        if (ep != null)
        {
            ep.WriteMessage(str);
        }
        else
        {
            if (!Console.IsOutputRedirected)
            {
                SafeConsole.WriteLineColor(color, str);
            }
        }
    }


    public static void SynchronizeProgressForeach<K, N, O>(this ExecutingProcess ep,
        Dictionary<K, N> newDictionary,
        Dictionary<K, O> oldDictionary,
        Action<K, N> createNew,
        Action<K, O> removeOld,
        Action<K, N, O> merge,
        string? status = null)
        where O : class
        where N : class
        where K : notnull
    {

        if (ep == null)
        {
            ep.WriteLineColor(ConsoleColor.Green, status);
            Synchronizer.SynchronizeProgressForeach(newDictionary, oldDictionary, createNew, removeOld, merge);
        }
        else
        {
            HashSet<K> keys = new HashSet<K>();
            keys.UnionWith(oldDictionary.Keys);
            keys.UnionWith(newDictionary.Keys);
            ep.ForEach(keys.ToList(), key => key.ToString()!, key =>
            {
                var oldVal = oldDictionary.TryGetC(key);
                var newVal = newDictionary.TryGetC(key);

                if (oldVal == null)
                {
                    createNew?.Invoke(key, newVal!);
                }
                else if (newVal == null)
                {
                    removeOld?.Invoke(key, oldVal);
                }
                else
                {
                    merge?.Invoke(key, newVal, oldVal);
                }
            }, status);
        }
    }


    public static void SynchronizeProgressForeachNonTransactional<K, N, O>(this ExecutingProcess ep,
        Dictionary<K, N> newDictionary,
        Dictionary<K, O> oldDictionary,
        Action<K, N> createNew,
        Action<K, O> removeOld,
        Action<K, N, O> merge)
        where O : class
        where N : class
        where K : notnull
    {

        if (ep == null)
            Synchronizer.SynchronizeProgressForeach(newDictionary, oldDictionary, createNew, removeOld, merge, transactional: false);
        else
        {
            HashSet<K> keys = new HashSet<K>();
            keys.UnionWith(oldDictionary.Keys);
            keys.UnionWith(newDictionary.Keys);
            ep.ForEachNonTransactional(keys.ToList(), key => key.ToString()!, key =>
            {
                var oldVal = oldDictionary.TryGetC(key);
                var newVal = newDictionary.TryGetC(key);

                if (oldVal == null)
                {
                    createNew?.Invoke(key, newVal!);
                }
                else if (newVal == null)
                {
                    removeOld?.Invoke(key, oldVal);
                }
                else
                {
                    merge?.Invoke(key, newVal, oldVal);
                }
            });
        }
    }
}



public interface IProcessAlgorithm
{
    void Execute(ExecutingProcess executingProcess);
}

public class ActionProcessAlgorithm : IProcessAlgorithm
{
    Action<ExecutingProcess> Action;

    public ActionProcessAlgorithm(Action<ExecutingProcess> action)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
    }
    
    public void Execute(ExecutingProcess executingProcess) => Action(executingProcess);
}
