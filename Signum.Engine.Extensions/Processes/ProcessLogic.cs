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

        public static Polymorphic<Action<IProcessSessionDN>> ApplySession = new Polymorphic<Action<IProcessSessionDN>>();

        public static Func<IProcessSessionDN> CreateDefaultProcessSession;

        static Expression<Func<ProcessAlgorithmDN, IQueryable<ProcessDN>>> ProcessesFromAlgorithmExpression =
            p => Database.Query<ProcessDN>().Where(a => a.Algorithm == p);
        [ExpressionField("ProcessesFromAlgorithmExpression")]
        public static IQueryable<ProcessDN> Processes(this ProcessAlgorithmDN p)
        {
            return ProcessesFromAlgorithmExpression.Evaluate(p);
        }

        static Expression<Func<ProcessAlgorithmDN, ProcessDN>> LastProcessFromAlgorithmExpression =
            p => p.Processes().OrderByDescending(a => a.ExecutionStart).FirstOrDefault();
          [ExpressionField("LastProcessFromAlgorithmExpression")]
        public static ProcessDN LastProcess(this ProcessAlgorithmDN p)
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

        static Dictionary<Enum, IProcessAlgorithm> registeredProcesses = new Dictionary<Enum, IProcessAlgorithm>();

        public static void AssertStarted(SchemaBuilder sb)
        {
            sb.AssertDefined(ReflectionTools.GetMethodInfo(() => ProcessLogic.Start(null, null, false)));
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm, bool userProcessSession)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ProcessAlgorithmDN>();
                sb.Include<ProcessDN>();
                sb.Include<ProcessExceptionLineDN>();

                PermissionAuthLogic.RegisterPermissions(ProcessPermission.ViewProcessPanel);

                MultiEnumLogic<ProcessAlgorithmDN>.Start(sb, () => registeredProcesses.Keys.ToHashSet());

                OperationLogic.AssertStarted(sb);
                AuthLogic.AssertStarted(sb);
                CacheLogic.AssertStarted(sb); 

                ProcessGraph.Register();

                dqm.RegisterQuery(typeof(ProcessAlgorithmDN), () =>
                             from pa in Database.Query<ProcessAlgorithmDN>()
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

                dqm.RegisterExpression((ProcessAlgorithmDN p) => p.Processes());
                dqm.RegisterExpression((ProcessAlgorithmDN p) => p.LastProcess());

                dqm.RegisterExpression((IProcessDataDN p) => p.Processes());
                dqm.RegisterExpression((IProcessDataDN p) => p.LastProcess());

                dqm.RegisterExpression((IProcessLineDataDN p) => p.ExceptionLines());

                if (userProcessSession)
                {
                    sb.Settings.AssertImplementedBy((ProcessDN p) => p.Session, typeof(UserProcessSessionDN));
                    ApplySession.Register((UserProcessSessionDN ups) =>
                    {
                        if (ups.User != null)
                            UserDN.Current = ups.User.Retrieve();
                    });

                    CreateDefaultProcessSession = UserProcessSessionDN.CreateCurrent;
                }
                else
                    CreateDefaultProcessSession = () => null;
            }
        }

        public static void Register(Enum processKey, IProcessAlgorithm logic)
        {
            if (processKey == null)
                throw new ArgumentNullException("processKey");

            if (logic == null)
                throw new ArgumentNullException("logic");

            registeredProcesses.Add(processKey, logic);
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
                        if (JustMyProcesses)
                            p.MachineName = Environment.MachineName;
                        else
                            p.MachineName = ProcessDN.None;

                        p.State = ProcessState.Planned;
                        p.PlannedDate = (DateTime)args[0];
                        
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
                        if (JustMyProcesses)
                            p.MachineName = Environment.MachineName;
                        else
                            p.MachineName = ProcessDN.None;

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
                    Construct = (p, _) => p.Algorithm.Create(p.Data, p.Session)
                }.Register();
            }
        }


        public static ProcessDN Create(Enum processKey, IProcessDataDN processData, IProcessSessionDN session = null)
        {
            return MultiEnumLogic<ProcessAlgorithmDN>.ToEntity(processKey).Create(processData, session);
        }

        public static ProcessDN Create(this ProcessAlgorithmDN process, IProcessDataDN processData, IProcessSessionDN session = null)
        {
            if (session == null)
            {
                session = ProcessLogic.CreateDefaultProcessSession();
            }

            using (OperationLogic.AllowSave<ProcessDN>())
                return new ProcessDN(process)
                {
                    State = ProcessState.Created,
                    Data = processData,
                    Session = session,
                    MachineName = Environment.MachineName,
                }.Save();
        }

        public static void ExecuteTest(this ProcessDN p)
        {
            p.QueuedDate = TimeZoneManager.Now;
            var ep = new ExecutingProcess(
                GetProcessAlgorithm(MultiEnumLogic<ProcessAlgorithmDN>.ToEnum(p.Algorithm.Key)),
                p
            );

            ep.Execute();
        }

        public static IProcessAlgorithm GetProcessAlgorithm(Enum processKey)
        {
            return registeredProcesses.GetOrThrow(processKey, "The process {0} is not registered");
        }

        public static void ForEachLine<T>(this ExecutingProcess executingProcess, IQueryable<T> remainingLines, Action<T> action, int groupsOf = 100)
            where T : IdentifiableEntity, IProcessLineDataDN, new()
        {
            var ramainingNotExceptionsLines = remainingLines.Where(li => li.Exception(executingProcess.CurrentExecution) == null);

            var totalCount = ramainingNotExceptionsLines.Count();
            int j = 0; 
            while (true)
            {
                List<T> lines = ramainingNotExceptionsLines.Take(groupsOf).ToList();
                if (lines.IsEmpty())
                    return;

                for (int i = 0; i < lines.Count; i++)
                {
                    executingProcess.CancellationToken.ThrowIfCancellationRequested();

                    T pl = lines[i];

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

    public interface IProcessAlgorithm
    {
        void Execute(ExecutingProcess executingProcess);
    }
}
