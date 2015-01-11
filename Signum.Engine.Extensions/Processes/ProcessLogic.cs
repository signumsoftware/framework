using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Processes;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;
using Signum.Utilities;
using System.Threading;
using Signum.Utilities.DataStructures;
using System.Diagnostics;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities.Scheduler;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Entities.Authorization;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Signum.Engine.Scheduler;
using System.Linq.Expressions;
using System.IO;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Cache;

namespace Signum.Engine.Processes
{
    public static class ProcessLogic
    {
        public static bool JustMyProcesses = true;

        public static Func<ProcessEntity, IDisposable> ApplySession;

        static Expression<Func<ProcessAlgorithmSymbol, IQueryable<ProcessEntity>>> ProcessesFromAlgorithmExpression =
            p => Database.Query<ProcessEntity>().Where(a => a.Algorithm == p);
        [ExpressionField("ProcessesFromAlgorithmExpression")]
        public static IQueryable<ProcessEntity> Processes(this ProcessAlgorithmSymbol p)
        {
            return ProcessesFromAlgorithmExpression.Evaluate(p);
        }

        static Expression<Func<ProcessAlgorithmSymbol, ProcessEntity>> LastProcessFromAlgorithmExpression =
            p => p.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault();
          [ExpressionField("LastProcessFromAlgorithmExpression")]
        public static ProcessEntity LastProcess(this ProcessAlgorithmSymbol p)
        {
            return LastProcessFromAlgorithmExpression.Evaluate(p);
        }

        static Expression<Func<ProcessEntity, IQueryable<ProcessExceptionLineEntity>>> ExceptionLinesProcessExpression =
            p => Database.Query<ProcessExceptionLineEntity>().Where(a => a.Process.RefersTo(p));
        [ExpressionField("ExceptionLinesProcessExpression")]
        public static IQueryable<ProcessExceptionLineEntity> ExceptionLines(this ProcessEntity p)
        {
            return ExceptionLinesProcessExpression.Evaluate(p);
        }


        static Expression<Func<IProcessLineDataEntity, IQueryable<ProcessExceptionLineEntity>>> ExceptionLinesLineExpression =
            p => Database.Query<ProcessExceptionLineEntity>().Where(a => a.Line.RefersTo(p));
        [ExpressionField("ExceptionLinesLineExpression")]
        public static IQueryable<ProcessExceptionLineEntity> ExceptionLines(this IProcessLineDataEntity pl)
        {
            return ExceptionLinesLineExpression.Evaluate(pl);
        }

        static Expression<Func<IProcessLineDataEntity, ProcessEntity, ExceptionEntity>> ExceptionExpression =
            (pl, p) => p.ExceptionLines().SingleOrDefault(el => el.Line.RefersTo(pl)).Exception.Entity;
        public static ExceptionEntity Exception(this IProcessLineDataEntity pl, ProcessEntity p)
        {
            return ExceptionExpression.Evaluate(pl, p);
        }


        static Expression<Func<IProcessDataEntity, IQueryable<ProcessEntity>>> ProcessesFromDataExpression =
            e => Database.Query<ProcessEntity>().Where(a => a.Data == e);
        [ExpressionField("ProcessesFromDataExpression")]
        public static IQueryable<ProcessEntity> Processes(this IProcessDataEntity e)
        {
            return ProcessesFromDataExpression.Evaluate(e);
        }

        static Expression<Func<IProcessDataEntity, ProcessEntity>> LastProcessFromDataExpression =
          e => e.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault();
        [ExpressionField("LastProcessFromDataExpression")]
        public static ProcessEntity LastProcess(this IProcessDataEntity e)
        {
            return LastProcessFromDataExpression.Evaluate(e);
        }

        static Dictionary<ProcessAlgorithmSymbol, IProcessAlgorithm> registeredProcesses = new Dictionary<ProcessAlgorithmSymbol, IProcessAlgorithm>();

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => ProcessLogic.Start(null, null, false)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool userProcessSession)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ProcessAlgorithmSymbol>();
                sb.Include<ProcessEntity>();
                sb.Include<ProcessExceptionLineEntity>();

                PermissionAuthLogic.RegisterPermissions(ProcessPermission.ViewProcessPanel);

                SymbolLogic<ProcessAlgorithmSymbol>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

                OperationLogic.AssertStarted(sb);

                ProcessGraph.Register();

                dqm.RegisterQuery(typeof(ProcessAlgorithmSymbol), () =>
                             from pa in Database.Query<ProcessAlgorithmSymbol>()
                             select new
                             {
                                 Entity = pa,
                                 pa.Id,
                                 pa.Key
                             });

                dqm.RegisterQuery(typeof(ProcessEntity), ()=>
                             from p in Database.Query<ProcessEntity>()
                              select new
                              {
                                  Entity = p,
                                  p.Id,
                                  Resume = p.ToString(),
                                  Process = p.Algorithm,
                                  State = p.State,
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

                dqm.RegisterQuery(typeof(ProcessExceptionLineEntity), () =>
                             from p in Database.Query<ProcessExceptionLineEntity>()
                             select new
                             {
                                 Entity = p,
                                 p.Line,
                                 p.Process,
                                 p.Exception,
                             });

                dqm.RegisterExpression((ProcessAlgorithmSymbol p) => p.Processes(), () => typeof(ProcessEntity).NicePluralName());
                dqm.RegisterExpression((ProcessAlgorithmSymbol p) => p.LastProcess(), () => ProcessMessage.LastProcess.NiceToString());

                dqm.RegisterExpression((IProcessDataEntity p) => p.Processes(), () => typeof(ProcessEntity).NicePluralName());
                dqm.RegisterExpression((IProcessDataEntity p) => p.LastProcess(), () => ProcessMessage.LastProcess.NiceToString());

                dqm.RegisterExpression((IProcessLineDataEntity p) => p.ExceptionLines(), () => ProcessMessage.ExceptionLines.NiceToString());

                if (userProcessSession)
                {
                    PropertyAuthLogic.AvoidAutomaticUpgradeCollection.Add(PropertyRoute.Construct((ProcessEntity p) => p.Mixin<UserProcessSessionMixin>().User));
                    MixinDeclarations.AssertDeclared(typeof(ProcessEntity), typeof(UserProcessSessionMixin));
                    ApplySession += process =>
                    {
                        var user = process.Mixin<UserProcessSessionMixin>().User;

                        if (user != null)
                            using (ExecutionMode.Global())
                                UserHolder.Current = user.Retrieve();

                        return null;
                    };
                }

                ExceptionLogic.DeleteLogs += ExceptionLogic_DeleteLogs;
            }
        }

        public static void ExceptionLogic_DeleteLogs(DeleteLogParametersEntity parameters)
        {
            Remove(ProcessState.Canceled, parameters);
            Remove(ProcessState.Finished, parameters);
            Remove(ProcessState.Error, parameters);
        }

        private static void Remove(ProcessState processState, DeleteLogParametersEntity parameters)
        {
            var query = Database.Query<ProcessEntity>().Where(p => p.State == ProcessState.Canceled && p.CreationDate < parameters.DateLimit);

            query.SelectMany(a => a.ExceptionLines()).UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);

            query.UnsafeDeleteChunks(parameters.ChunkSize, parameters.MaxChunks);
        }

        public static IDisposable OnApplySession(ProcessEntity process)
        {
            return Disposable.Combine(ApplySession, f => f(process));
        }

        public static void Register(ProcessAlgorithmSymbol processAlgorthm, IProcessAlgorithm logic)
        {
            if (processAlgorthm == null)
                throw new ArgumentNullException("processAlgorthmSymbol");

            if (logic == null)
                throw new ArgumentNullException("logic");

            registeredProcesses.Add(processAlgorthm, logic);
        }


        public class ProcessGraph : Graph<ProcessEntity, ProcessState>
        {
            public static void Register()
            {
                GetState = e => e.State;

                new Execute(ProcessOperation.Save)
                {
                    FromStates = { ProcessState.Created },
                    ToState = ProcessState.Created,
                    AllowsNew = true,
                    Lite = false,
                    Execute = (p, args) =>
                    {
                        p.Save();
                    }
                }.Register();

                new Execute(ProcessOperation.Plan)
                {
                    FromStates = { ProcessState.Created, ProcessState.Canceled, ProcessState.Planned, ProcessState.Suspended },
                    ToState = ProcessState.Planned,
                    Execute = (p, args) =>
                    {
                        p.MachineName = JustMyProcesses ? Environment.MachineName : ProcessEntity.None;
                        p.ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName : ProcessEntity.None;
                      
                        p.State = ProcessState.Planned;
                        p.PlannedDate = args.GetArg<DateTime>();
                    }
                }.Register();

                new Execute(ProcessOperation.Cancel)
                {
                    FromStates = { ProcessState.Planned, ProcessState.Created, ProcessState.Suspended, ProcessState.Queued },
                    ToState = ProcessState.Canceled,
                    Execute = (p, _) =>
                    {
                        p.State = ProcessState.Canceled;
                        p.CancelationDate = TimeZoneManager.Now;
                    }
                }.Register();

                new Execute(ProcessOperation.Execute)
                {
                    FromStates = { ProcessState.Created, ProcessState.Planned, ProcessState.Canceled, ProcessState.Suspended },
                    ToState = ProcessState.Queued,
                    Execute = (p, _) =>
                    {
                        p.MachineName = JustMyProcesses ? Environment.MachineName : ProcessEntity.None;
                        p.ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName: ProcessEntity.None;

                        p.SetAsQueued();

                        ProcessRunnerLogic.WakeUp("Execute in this machine", null);
                    }
                }.Register();

                new Execute(ProcessOperation.Suspend)
                {
                    FromStates = { ProcessState.Executing },
                    ToState = ProcessState.Suspending,
                    Execute = (p, _) =>
                    {
                        p.State = ProcessState.Suspending;
                        p.SuspendDate = TimeZoneManager.Now;
                    }
                }.Register();

                new ConstructFrom<ProcessEntity>(ProcessOperation.Retry)
                {
                    CanConstruct = p => p.State.InState(ProcessState.Error, ProcessState.Canceled, ProcessState.Finished, ProcessState.Suspended),
                    ToState = ProcessState.Created,
                    Construct = (p, _) => p.Algorithm.Create(p.Data, p)
                }.Register();
            }
        }

        public static ProcessEntity Create(this ProcessAlgorithmSymbol process, IProcessDataEntity processData, Entity copyMixinsFrom = null)
        {
            using (OperationLogic.AllowSave<ProcessEntity>())
            {
                var result = new ProcessEntity(process)
                {
                    State = ProcessState.Created,
                    Data = processData,
                    MachineName = JustMyProcesses ? Environment.MachineName : ProcessEntity.None,
                    ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName : ProcessEntity.None,
                };
                
                if(copyMixinsFrom != null)
                    process.CopyMixinsFrom(copyMixinsFrom);

                return result.Save();
            }
        }

        public static void ExecuteTest(this ProcessEntity p)
        {
            p.QueuedDate = TimeZoneManager.Now;
            var ep = new ExecutingProcess(
                GetProcessAlgorithm(p.Algorithm),
                p
            );

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
            var remainingNotExceptionsLines = remainingLines.Where(li => li.Exception(executingProcess.CurrentExecution) == null);

            var totalCount = remainingNotExceptionsLines.Count();
            int j = 0; 
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
                                    Process = executingProcess.CurrentExecution.ToLite()
                                }.Save();

                                tr.Commit();
                            });
                        }

                        executingProcess.ProgressChanged(j++, totalCount);
                    }
                }
            }
        }
    }

    public interface IProcessAlgorithm
    {
        void Execute(ExecutingProcess executingProcess);
    }
}
