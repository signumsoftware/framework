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
using Signum.Engine.Exceptions;
using System.IO;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Cache;

namespace Signum.Engine.Processes
{
    public static class ProcessLogic
    {
        public static bool JustMyProcesses = true;

        public static Func<ProcessDN, IDisposable> ApplySession;

        static Expression<Func<ProcessAlgorithmSymbol, IQueryable<ProcessDN>>> ProcessesFromAlgorithmExpression =
            p => Database.Query<ProcessDN>().Where(a => a.Algorithm == p);
        [ExpressionField("ProcessesFromAlgorithmExpression")]
        public static IQueryable<ProcessDN> Processes(this ProcessAlgorithmSymbol p)
        {
            return ProcessesFromAlgorithmExpression.Evaluate(p);
        }

        static Expression<Func<ProcessAlgorithmSymbol, ProcessDN>> LastProcessFromAlgorithmExpression =
            p => p.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault();
          [ExpressionField("LastProcessFromAlgorithmExpression")]
        public static ProcessDN LastProcess(this ProcessAlgorithmSymbol p)
        {
            return LastProcessFromAlgorithmExpression.Evaluate(p);
        }

        static Expression<Func<ProcessDN, IQueryable<ProcessExceptionLineDN>>> ExceptionLinesProcessExpression =
            p => Database.Query<ProcessExceptionLineDN>().Where(a => a.Process.RefersTo(p));
        [ExpressionField("ExceptionLinesProcessExpression")]
        public static IQueryable<ProcessExceptionLineDN> ExceptionLines(this ProcessDN p)
        {
            return ExceptionLinesProcessExpression.Evaluate(p);
        }


        static Expression<Func<IProcessLineDataDN, IQueryable<ProcessExceptionLineDN>>> ExceptionLinesLineExpression =
            p => Database.Query<ProcessExceptionLineDN>().Where(a => a.Line.RefersTo(p));
        [ExpressionField("ExceptionLinesLineExpression")]
        public static IQueryable<ProcessExceptionLineDN> ExceptionLines(this IProcessLineDataDN pl)
        {
            return ExceptionLinesLineExpression.Evaluate(pl);
        }

        static Expression<Func<IProcessLineDataDN, ProcessDN, ExceptionDN>> ExceptionExpression =
            (pl, p) => p.ExceptionLines().SingleOrDefault(el => el.Line.RefersTo(pl)).Exception.Entity;
        public static ExceptionDN Exception(this IProcessLineDataDN pl, ProcessDN p)
        {
            return ExceptionExpression.Evaluate(pl, p);
        }


        static Expression<Func<IProcessDataDN, IQueryable<ProcessDN>>> ProcessesFromDataExpression =
            e => Database.Query<ProcessDN>().Where(a => a.Data == e);
        [ExpressionField("ProcessesFromDataExpression")]
        public static IQueryable<ProcessDN> Processes(this IProcessDataDN e)
        {
            return ProcessesFromDataExpression.Evaluate(e);
        }

        static Expression<Func<IProcessDataDN, ProcessDN>> LastProcessFromDataExpression =
          e => e.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault();
        [ExpressionField("LastProcessFromDataExpression")]
        public static ProcessDN LastProcess(this IProcessDataDN e)
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
                sb.Include<ProcessDN>();
                sb.Include<ProcessExceptionLineDN>();

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

                dqm.RegisterQuery(typeof(ProcessDN), ()=>
                             from p in Database.Query<ProcessDN>()
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

                dqm.RegisterQuery(typeof(ProcessExceptionLineDN), () =>
                             from p in Database.Query<ProcessExceptionLineDN>()
                             select new
                             {
                                 Entity = p,
                                 p.Line,
                                 p.Process,
                                 p.Exception,
                             });

                dqm.RegisterExpression((ProcessAlgorithmSymbol p) => p.Processes(), () => typeof(ProcessDN).NicePluralName());
                dqm.RegisterExpression((ProcessAlgorithmSymbol p) => p.LastProcess(), () => ProcessMessage.LastProcess.NiceToString());

                dqm.RegisterExpression((IProcessDataDN p) => p.Processes(), () => typeof(ProcessDN).NicePluralName());
                dqm.RegisterExpression((IProcessDataDN p) => p.LastProcess(), () => ProcessMessage.LastProcess.NiceToString());

                dqm.RegisterExpression((IProcessLineDataDN p) => p.ExceptionLines(), () => ProcessMessage.ExceptionLines.NiceToString());

                if (userProcessSession)
                {
                    PropertyAuthLogic.AvoidAutomaticUpgradeCollection.Add(PropertyRoute.Construct((ProcessDN p) => p.Mixin<UserProcessSessionMixin>().User));
                    MixinDeclarations.AssertDeclared(typeof(ProcessDN), typeof(UserProcessSessionMixin));
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

        public static void ExceptionLogic_DeleteLogs(DateTime limit)
        {
            Remove(ProcessState.Canceled, limit);
            Remove(ProcessState.Finished, limit);
            Remove(ProcessState.Error, limit);
        }

        private static void Remove(ProcessState processState, DateTime limit)
        {
            var query = Database.Query<ProcessDN>().Where(p => p.State == ProcessState.Canceled && p.CreationDate < limit);

            query.SelectMany(a=>a.ExceptionLines()).UnsafeDelete();

            query.UnsafeDelete();
        }

        public static IDisposable OnApplySession(ProcessDN process)
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


        public class ProcessGraph : Graph<ProcessDN, ProcessState>
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
                        p.MachineName = JustMyProcesses ? Environment.MachineName : ProcessDN.None;
                        p.ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName : ProcessDN.None;
                      
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
                        p.MachineName = JustMyProcesses ? Environment.MachineName : ProcessDN.None;
                        p.ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName: ProcessDN.None;

                        p.SetAsQueued();
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

                new ConstructFrom<ProcessDN>(ProcessOperation.Retry)
                {
                    CanConstruct = p => p.State.InState(ProcessState.Error, ProcessState.Canceled, ProcessState.Finished, ProcessState.Suspended),
                    ToState = ProcessState.Created,
                    Construct = (p, _) => p.Algorithm.Create(p.Data, p)
                }.Register();
            }
        }

        public static ProcessDN Create(this ProcessAlgorithmSymbol process, IProcessDataDN processData, IdentifiableEntity copyMixinsFrom = null)
        {
            using (OperationLogic.AllowSave<ProcessDN>())
            {
                var result = new ProcessDN(process)
                {
                    State = ProcessState.Created,
                    Data = processData,
                    MachineName = JustMyProcesses ? Environment.MachineName : ProcessDN.None,
                    ApplicationName = JustMyProcesses ? Schema.Current.ApplicationName : ProcessDN.None,
                };
                
                if(copyMixinsFrom != null)
                    process.CopyMixinsFrom(copyMixinsFrom);

                return result.Save();
            }
        }

        public static void ExecuteTest(this ProcessDN p)
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
            where T : IdentifiableEntity, IProcessLineDataDN, new()
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
                            using (Transaction tr = Transaction.ForceNew())
                            {
                                action(pl);
                                tr.Commit();
                            }
                        }
                        catch (Exception e)
                        {
                            if (Transaction.InTestTransaction)
                                throw;

                            var exLog = e.LogException();

                            using (Transaction tr = Transaction.ForceNew())
                            {
                                new ProcessExceptionLineDN
                                {
                                    Exception = exLog.ToLite(),
                                    Line = pl.ToLite(),
                                    Process = executingProcess.CurrentExecution.ToLite()
                                }.Save();

                                tr.Commit();
                            }
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
