using Signum.Entities.Workflow;
using Signum.Engine;
using Signum.Engine.Dynamic;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.Dynamic;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Engine.Scheduler;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Engine.Alerts;
using Signum.Engine.Authorization;

namespace Signum.Engine.Workflow
{
    public static class CaseActivityLogic
    {
        static Expression<Func<WorkflowEntity, IQueryable<CaseEntity>>> CasesExpression =
            w => Database.Query<CaseEntity>().Where(a => a.Workflow == w);
        [ExpressionField]
        public static IQueryable<CaseEntity> Cases(this WorkflowEntity e)
        {
            return CasesExpression.Evaluate(e);
        }


        static Expression<Func<CaseActivityEntity, IQueryable<CaseActivityEntity>>> NextActivitiesExpression =
            ca => Database.Query<CaseActivityEntity>().Where(a => a.Previous.RefersTo(ca));
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> NextActivities(this CaseActivityEntity e)
        {
            return NextActivitiesExpression.Evaluate(e);
        }

        static Expression<Func<CaseEntity, CaseActivityEntity>> DecompositionSurrogateActivityExpression =
            childCase => childCase.CaseActivities().OrderBy(ca => ca.StartDate).Select(a => a.Previous.Entity).First();
        [ExpressionField]
        public static CaseActivityEntity DecompositionSurrogateActivity(this CaseEntity childCase)
        {
            return DecompositionSurrogateActivityExpression.Evaluate(childCase);
        }



        static Expression<Func<CaseEntity, IQueryable<CaseEntity>>> SubCasesExpression =
        p => Database.Query<CaseEntity>().Where(c => c.ParentCase.Is(p));
        [ExpressionField]
        public static IQueryable<CaseEntity> SubCases(this CaseEntity p)
        {
            return SubCasesExpression.Evaluate(p);
        }


        static Expression<Func<CaseActivityEntity, bool>> IsFreshNewExpression =
        ca => (ca.State == CaseActivityState.PendingNext || ca.State == CaseActivityState.PendingDecision) && 
                ca.Notifications().All(n => n.State == CaseNotificationState.New);
        [ExpressionField]
        public static bool IsFreshNew(this CaseActivityEntity entity)
        {
            return IsFreshNewExpression.Evaluate(entity);
        }
  

        static Expression<Func<WorkflowActivityEntity, IQueryable<CaseActivityEntity>>> CaseActivitiesFromWorkflowActivityExpression =
            e => Database.Query<CaseActivityEntity>().Where(a => a.WorkflowActivity == e);
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> CaseActivities(this WorkflowActivityEntity e)
        {
            return CaseActivitiesFromWorkflowActivityExpression.Evaluate(e);
        }


        static Expression<Func<WorkflowActivityEntity, double?>> AverageDurationExpression =
        wa => wa.CaseActivities().Average(a => a.Duration);
        [ExpressionField]
        public static double? AverageDuration(this WorkflowActivityEntity wa)
        {
            return AverageDurationExpression.Evaluate(wa);
        }

        static Expression<Func<CaseEntity, IQueryable<CaseActivityEntity>>> CaseActivitiesFromCaseExpression =
            e => Database.Query<CaseActivityEntity>().Where(a => a.Case == e);
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> CaseActivities(this CaseEntity e)
        {
            return CaseActivitiesFromCaseExpression.Evaluate(e);
        }


        static Expression<Func<CaseEntity, IQueryable<CaseTagEntity>>> TagsExpression =
        e => Database.Query<CaseTagEntity>().Where(a => a.Case.RefersTo(e));
        [ExpressionField]
        public static IQueryable<CaseTagEntity> Tags(this CaseEntity e)
        {
            return TagsExpression.Evaluate(e);
        }

        static Expression<Func<CaseActivityEntity, IQueryable<CaseNotificationEntity>>> NotificationsExpression =
            e => Database.Query<CaseNotificationEntity>().Where(a => a.CaseActivity.RefersTo(e));
        [ExpressionField]
        public static IQueryable<CaseNotificationEntity> Notifications(this CaseActivityEntity e)
        {
            return NotificationsExpression.Evaluate(e);
        }


        static Expression<Func<CaseActivityEntity, IQueryable<CaseActivityExecutedTimerEntity>>> ExecutedTimersExpression =
        e => Database.Query<CaseActivityExecutedTimerEntity>().Where(a => a.CaseActivity.RefersTo(e));
        [ExpressionField]
        public static IQueryable<CaseActivityExecutedTimerEntity> ExecutedTimers(this CaseActivityEntity e)
        {
            return ExecutedTimersExpression.Evaluate(e);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CaseEntity>()
                    .WithExpressionFrom(dqm, (WorkflowEntity w) => w.Cases())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Description,
                        e.Workflow,
                        e.MainEntity,
                    });

                sb.Include<CaseTagTypeEntity>()
                    .WithSave(CaseTagTypeOperation.Save)
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Color
                    });


                sb.Include<CaseTagEntity>()
                    .WithExpressionFrom(dqm, (CaseEntity ce) => ce.Tags())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.CreationDate,
                        e.Case,
                        e.TagType,
                        e.CreatedBy,
                    });

                new Graph<CaseEntity>.Execute(CaseOperation.SetTags)
                {
                    Execute = (e, args) => 
                    {
                        var current = e.Tags().ToList();

                        var model = args.GetArg<CaseTagsModel>();

                        var toDelete = current.Where(ct => model.OldCaseTags.Contains(ct.TagType) && !model.CaseTags.Contains(ct.TagType)).ToList();

                        Database.DeleteList(toDelete);

                        model.CaseTags.Where(ctt => !current.Any(ct => ct.TagType.Is(ctt))).Select(ctt => new CaseTagEntity
                        {
                            Case = e.ToLite(),
                            TagType = ctt,
                            CreatedBy = UserHolder.Current.ToLite(),
                        }).SaveList();
                    },
                }.Register();

                sb.Include<CaseActivityEntity>()
                    .WithIndex(a => new { a.ScriptExecution.ProcessIdentifier }, a => a.DoneDate == null)
                    .WithIndex(a => new { a.ScriptExecution.NextExecution }, a => a.DoneDate == null)
                    .WithExpressionFrom(dqm, (WorkflowActivityEntity c) => c.CaseActivities())
                    .WithExpressionFrom(dqm, (CaseEntity c) => c.CaseActivities())
                    .WithExpressionFrom(dqm, (CaseActivityEntity c) => c.NextActivities())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.StartDate,
                        e.DoneDate,
                        e.DoneBy,
                        e.Previous,
                        e.Case,
                        e.WorkflowActivity,
                    });


                sb.Include<CaseActivityExecutedTimerEntity>()
                    .WithExpressionFrom(dqm, (CaseActivityEntity ca) => ca.ExecutedTimers())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.CreationDate,
                        e.BpmnElementId,
                        e.CaseActivity
                    });

                dqm.RegisterExpression((WorkflowActivityEntity a) => a.AverageDuration(), () => WorkflowActivityMessage.AverageDuration.NiceToString());

                SimpleTaskLogic.Register(CaseActivityTask.Timeout, (ScheduledTaskContext ctx) =>
                {
                    var candidates =
                    (from ca in Database.Query<CaseActivityEntity>()
                     where ca.State == CaseActivityState.PendingDecision || ca.State == CaseActivityState.PendingNext
                     from timer in ca.WorkflowActivity.Timers
                     where timer.Interrupting || !ca.ExecutedTimers().Any(t => t.BpmnElementId == timer.BpmnElementId)
                     select new { Activity = ca, Timer = timer });
                    
                    var conditions = candidates.Select(a => a.Timer.Condition).Distinct().ToList();

                    var now = TimeZoneManager.Now;
                    var activities = conditions.SelectMany(cond =>
                    {
                        if (cond == null)
                            return candidates.Where(a => a.Timer.Duration != null && a.Timer.Duration.Add(a.Activity.StartDate) < now).Select(a => a.Activity.ToLite()).ToList();

                        var condExpr = cond.RetrieveFromCache().Eval.Algorithm.GetTimerCondition();

                        return candidates.Where(a => a.Timer.Condition == cond).Select(a => condExpr.Evaluate(a.Activity, now) ? a.Activity.ToLite() : null).ToList().NotNull().ToList();
                    }).Distinct().ToList();

                    if (!activities.Any())
                        return null;

                    var pkg = new PackageEntity().CreateLines(activities);

                    return ProcessLogic.Create(CaseActivityProcessAlgorithm.Timeout, pkg).Execute(ProcessOperation.Execute).ToLite();
                });
                ProcessLogic.Register(CaseActivityProcessAlgorithm.Timeout, new PackageExecuteAlgorithm<CaseActivityEntity>(CaseActivityOperation.Timer));

                dqm.RegisterExpression((CaseEntity c) => c.DecompositionSurrogateActivity());

                sb.Include<CaseNotificationEntity>()
                    .WithExpressionFrom(dqm, (CaseActivityEntity c) => c.Notifications())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.CaseActivity.Entity.StartDate,
                        e.State,
                        e.CaseActivity,
                        e.User,
                    });


                new Graph<CaseNotificationEntity>.Execute(CaseNotificationOperation.SetRemarks)
                {
                    Execute = (e, args) =>
                    {
                        e.Remarks = args.GetArg<string>();
                    },
                }.Register();


                dqm.RegisterQuery(CaseActivityQuery.Inbox, () => DynamicQueryCore.Auto(
                        from cn in Database.Query<CaseNotificationEntity>()
                        where cn.User == UserEntity.Current.ToLite()
                        let ca = cn.CaseActivity.Entity
                        let previous = ca.Previous.Entity
                        select new
                        {
                            Entity = cn.CaseActivity,
                            ca.StartDate,
                            Activity = new ActivityWithRemarks
                            {
                                workflowActivity = ca.WorkflowActivity.ToLite(),
                                @case = ca.Case.ToLite(),
                                caseActivity = ca.ToLite(),
                                notification = cn.ToLite(),
                                remarks = cn.Remarks,
                                alerts = ca.MyActiveAlerts().Count(),
                                tags = ca.Case.Tags().Select(a => a.TagType).ToList(),
                            },
                            MainEntity = ca.Case.MainEntity.ToLite(ca.Case.ToString()),
                            Sender = previous.DoneBy,
                            SenderNote = previous.Note,
                            cn.State,
                            cn.Actor,
                        })
                        .ColumnDisplayName(a => a.Activity, () => InboxMessage.Activity.NiceToString())
                        .ColumnDisplayName(a => a.Sender, () => InboxMessage.Sender.NiceToString())
                        .ColumnDisplayName(a => a.SenderNote, () => InboxMessage.SenderNote.NiceToString())
                        );

                sb.Schema.WhenIncluded<DynamicTypeEntity>(() =>
                {
                    new Graph<DynamicTypeEntity>.Execute(CaseActivityOperation.FixCaseDescriptions)
                    {
                        Execute = (e, _) =>
                        {
                            var type = TypeLogic.GetType(e.TypeName);
                            giFixCaseDescriptions.GetInvoker(type)();
                        },
                    }.Register();
                });

                CaseActivityGraph.Register();
            }
        }

        public static CaseActivityEntity CreateCaseActivity(this WorkflowEntity workflow, ICaseMainEntity mainEntity)
        {
            var caseActivity = workflow.ConstructFrom(CaseActivityOperation.CreateCaseActivityFromWorkflow, mainEntity);
            return caseActivity.Execute(CaseActivityOperation.Register);
        }

        static readonly GenericInvoker<Action> giFixCaseDescriptions = new GenericInvoker<Action>(() => FixCaseDescriptions<Entity>());
        public static void FixCaseDescriptions<T>() where T : Entity
        {
            Database.Query<CaseEntity>()
                          .Where(a => a.MainEntity.GetType() == typeof(T))
                          .UnsafeUpdate()
                          .Set(a => a.Description, a => ((T)a.MainEntity).ToString())
                          .Execute();
        }

        public static Dictionary<Type, WorkflowOptions> Options = new Dictionary<Type, WorkflowOptions>(); 

        public class WorkflowOptions
        {
            public Func<ICaseMainEntity> Constructor;
            public Action<ICaseMainEntity> SaveEntity;
        }
        
        public static FluentInclude<T> WithWorkflow<T>(this FluentInclude<T> fi, Func<T> constructor, Action<T> save)
            where T: Entity, ICaseMainEntity
        {
            fi.SchemaBuilder.Schema.EntityEvents<T>().Saved += (e, args)=>
            {
                if (AvoidNotifyInProgressVariable.Value == true)
                    return;

                e.NotifyInProgress();
            };

            Options[typeof(T)] = new WorkflowOptions
            {
                Constructor = constructor,
                SaveEntity = e => save((T)e)
            };

            return fi; 
        }

        static IDisposable AvoidNotifyInProgress()
        {
            var old = AvoidNotifyInProgressVariable.Value;
            AvoidNotifyInProgressVariable.Value = true;
            return new Disposable(() => AvoidNotifyInProgressVariable.Value = old);
        }
        
        static ThreadVariable<bool> AvoidNotifyInProgressVariable = Statics.ThreadVariable<bool>("avoidNotifyInProgress");

        public static int NotifyInProgress(this ICaseMainEntity mainEntity)
        {
            return Database.Query<CaseNotificationEntity>()
                .Where(n => n.CaseActivity.Entity.Case.MainEntity == mainEntity && n.CaseActivity.Entity.DoneDate == null)
                .Where(n => n.User == UserEntity.Current.ToLite() && (n.State == CaseNotificationState.New || n.State == CaseNotificationState.Opened))
                .UnsafeUpdate()
                .Set(n => n.State, n => CaseNotificationState.InProgress)
                .Execute();
        }

        public class WorkflowExecuteStepContext
        {
            public DecisionResult? DecisionResult;
            public CaseEntity Case;
            public CaseActivityEntity CaseActivity;
            public List<WorkflowActivityEntity> ToActivities = new List<WorkflowActivityEntity>();
            public bool IsFinished { get; set; }
            public List<IWorkflowTransition> Connections = new List<IWorkflowTransition>();
        }

        static bool Applicable(this WorkflowConnectionEntity wc, WorkflowExecuteStepContext ctx)
        {
            if (wc.DecisonResult != null && wc.DecisonResult != ctx.DecisionResult)
                return false;

            if (wc.Condition != null)
            {
                var alg = wc.Condition.RetrieveFromCache().Eval.Algorithm;
                var result = alg.EvaluateUntyped(ctx.Case.MainEntity, new WorkflowTransitionContext(ctx.Case, ctx.CaseActivity, wc, ctx.DecisionResult));


                return result;
            }
            
            return true;
        }

        static void WorkflowAction(ICaseMainEntity me, WorkflowTransitionContext ctx)
        {
            WorkflowLogic.OnTransition?.Invoke(me, ctx);

            if (ctx.Connection.Action != null)
            {
                var alg = ctx.Connection.Action.RetrieveFromCache().Eval.Algorithm;
                alg.ExecuteUntyped(me, ctx);
            };
        }

        static void SaveEntity(ICaseMainEntity mainEntity)
        {
            var options = CaseActivityLogic.Options.GetOrThrow(mainEntity.GetType());
            using (AvoidNotifyInProgress())
                options.SaveEntity(mainEntity);
        }

        public static CaseActivityEntity RetrieveForViewing(Lite<CaseActivityEntity> lite)
        {
            var ca = lite.Retrieve();

            if (ca.DoneBy == null)
                ca.Notifications()
                  .Where(n => n.User == UserEntity.Current.ToLite() && n.State == CaseNotificationState.New)
                  .UnsafeUpdate()
                  .Set(a => a.State, a => CaseNotificationState.Opened)
                  .Execute();

            return ca;
        }

        static void InsertCaseActivityNotifications(CaseActivityEntity caseActivity)
        {
            if (caseActivity.WorkflowActivity.Type == WorkflowActivityType.Task || 
                caseActivity.WorkflowActivity.Type == WorkflowActivityType.Decision)
            {
                using (ExecutionMode.Global())
                {
                    var lane = caseActivity.WorkflowActivity.Lane;
                    var actors = lane.Actors.ToList();
                    if (lane.ActorsEval != null)
                        actors = lane.ActorsEval.Algorithm.GetActors(caseActivity.Case.MainEntity, new WorkflowTransitionContext(caseActivity.Case, caseActivity, null, null)).EmptyIfNull().ToList();

                    var notifications = actors.Distinct().SelectMany(a =>
                    Database.Query<UserEntity>()
                    .Where(u => WorkflowLogic.IsUserActorConstant.Evaluate(u, a))
                    .Select(u => new CaseNotificationEntity
                    {
                        CaseActivity = caseActivity.ToLite(),
                        Actor = a,
                        State = CaseNotificationState.New,
                        User = u.ToLite()
                    })).ToList();

                    notifications.BulkInsert();
                }
            }
        }

      

        class CaseActivityGraph : Graph<CaseActivityEntity, CaseActivityState>
        {
            public static void Register()
            {
                GetState = ca => ca.State;
                new ConstructFrom<WorkflowEntity>(CaseActivityOperation.CreateCaseActivityFromWorkflow)
                {
                    ToStates = { CaseActivityState.New},
                    Construct = (w, args) =>
                    {
                        var mainEntity = args.TryGetArgC<ICaseMainEntity>() ?? CaseActivityLogic.Options.GetOrThrow(w.MainEntityType.ToType()).Constructor();

                        var @case = new CaseEntity
                        {
                            ParentCase = args.TryGetArgC<CaseEntity>(),
                            Workflow = w,
                            Description = w.Name,
                            MainEntity = mainEntity,
                        };

                        var start = w.WorkflowEvents().Single(a => a.Type == WorkflowEventType.Start);
                        var connection = start.NextConnectionsFromCache().SingleEx();
                        var next = (WorkflowActivityEntity)connection.To;
                        var ca = new CaseActivityEntity
                        {
                            WorkflowActivity = next,
                            OriginalWorkflowActivityName = next.Name,
                            Case = @case,
                        };

                        WorkflowAction(@case.MainEntity, new WorkflowTransitionContext(@case, ca, connection, null));
                       
                        return ca;
                    }
                }.Register();

                new Graph<CaseEntity>.ConstructFrom<WorkflowEventTaskEntity>(CaseActivityOperation.CreateCaseFromWorkflowEventTask)
                {
                    Construct = (wet, args) =>
                    {
                        var mainEntity = args.TryGetArgC<ICaseMainEntity>();
                        var w = wet.GetWorkflow();
                        var @case = new CaseEntity
                        {
                            Workflow = w,
                            Description = w.Name,
                            MainEntity = mainEntity,
                        };

                        var start = wet.Event.Retrieve();
                        ExecuteInitialStep(@case, start, start.NextConnectionsFromCache().SingleEx());

                        return @case;
                    }
                }.Register();

                new Execute(CaseActivityOperation.Register)
                {
                    FromStates = {  CaseActivityState.New },
                    ToStates = {  CaseActivityState.PendingNext, CaseActivityState.PendingDecision},
                    AllowsNew = true,
                    Lite = false,
                    Execute = (ca, _) =>
                    {
                        SaveEntity(ca.Case.MainEntity);
                        var now = TimeZoneManager.Now;
                        var c = ca.Case;
                        c.StartDate = now;
                        c.Description = ca.Case.MainEntity.ToString().Trim();
                        c.Save();

                        var prev = ca.WorkflowActivity.PreviousConnectionsFromCache().SingleEx(a => a.From is WorkflowEventEntity && ((WorkflowEventEntity)a.From).Type == WorkflowEventType.Start);
                        WorkflowAction(ca.Case.MainEntity, new WorkflowTransitionContext(ca.Case, ca, prev, null));

                        ca.StartDate = now;
                        ca.Save();

                        InsertCaseActivityNotifications(ca);
                    }
                }.Register();

                new Delete(CaseActivityOperation.Delete)
                {
                    FromStates = { CaseActivityState.PendingDecision, CaseActivityState.PendingNext },
                    CanDelete = ca => ca.Case.ParentCase != null ? CaseActivityMessage.CaseIsADecompositionOf0.NiceToString(ca.Case.ParentCase) :
                    ca.Case.CaseActivities().Any(a => a != ca) ? CaseActivityMessage.CaseContainsOtherActivities.NiceToString() : 
                    null,
                    Delete = (ca, _) =>
                    {
                        var c = ca.Case;
                        ca.Notifications().UnsafeDelete();
                        ca.Delete();
                        c.Delete();
                        c.MainEntity.Delete();
                    },
                }.Register();

                new Execute(CaseActivityOperation.Approve)
                {
                    FromStates = {  CaseActivityState.PendingDecision },
                    ToStates = {  CaseActivityState.Done },
                    Lite = false,
                    Execute = (ca, _) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, DecisionResult.Approve, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Decline)
                {
                    FromStates = { CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done },
                    Lite = false,
                    Execute = (ca, _) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, DecisionResult.Decline, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Next)
                {
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.Done },
                    Lite = false,
                    Execute = (ca, args) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, null, null);
                    },
                }.Register();

            

                new Execute(CaseActivityOperation.Jump)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done },
                    CanExecute = a => a.WorkflowActivity.Jumps.Any() ? null : CaseActivityMessage.Activity0HasNoJumps.NiceToString(a.WorkflowActivity),
                    Lite = false,
                    Execute = (ca, args) =>
                    {
                        CheckRequiresOpen(ca);
                        var to = args.GetArg<Lite<IWorkflowNodeEntity>>();
                        WorkflowJumpEmbedded jump = ca.WorkflowActivity.Jumps.SingleEx(j => j.To.Is(to));
                        ExecuteStep(ca, null, jump);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Reject)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done },
                    CanExecute = ca => 
                        ca.WorkflowActivity.Reject == null ?  CaseActivityMessage.Activity0HasNoReject.NiceToString(ca.WorkflowActivity) : 
                        ca.Previous == null ? CaseActivityMessage.ThereIsNoPreviousActivity.NiceToString() :
                        null,
                    Lite = false,
                    Execute = (ca, _) =>
                    {
                        var pwa = ca.Previous.Retrieve().WorkflowActivity;
                        if (!pwa.Lane.Pool.Workflow.Is(ca.WorkflowActivity.Lane.Pool.Workflow))
                            throw new InvalidOperationException("Previous in different workflow");

                        ExecuteStep(ca, null, ca.WorkflowActivity.Reject);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Timer)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done, CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    CanExecute = ca => ca.WorkflowActivity.Timers.Count == 0 ? CaseActivityMessage.Activity0HasNoTimers.NiceToString(ca.WorkflowActivity) : null,
                    Execute = (ca, _) =>
                    {
                        var now = TimeZoneManager.Now;

                        var alreadyExecuted = ca.ExecutedTimers().Select(a => a.BpmnElementId).ToHashSet();

                        var timer = ca.WorkflowActivity.Timers.Where(a => a.Interrupting || !alreadyExecuted.Contains(a.BpmnElementId)).FirstOrDefault(t =>
                           {
                               if (t.Duration != null)
                                   return t.Duration.Add(ca.StartDate) < now;

                               return t.Condition.RetrieveFromCache().Eval.Algorithm.GetTimerCondition().Evaluate(ca, now);
                           });
                        
                        if (timer == null)
                            throw new InvalidOperationException(WorkflowActivityMessage.NoActiveTimerFound.NiceToString());


                        if (timer.Interrupting)
                            ExecuteStep(ca, null, timer);
                        else
                            ExecuteTimerFork(ca, timer);
                    },
                }.Register();

                new Execute(CaseActivityOperation.MarkAsUnread)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    CanExecute = c=> c.Notifications().Any(a=>a.User.RefersTo(UserEntity.Current) && (a.State == CaseNotificationState.InProgress || a.State == CaseNotificationState.Opened)) ? null :
                        CaseActivityMessage.NoOpenedOrInProgressNotificationsFound.NiceToString(),
                    Execute = (ca, args) =>
                    {
                        ca.Notifications()
                        .Where(cn => cn.User.RefersTo(UserEntity.Current) && (cn.State == CaseNotificationState.InProgress || cn.State == CaseNotificationState.Opened))
                        .UnsafeUpdate()
                        .Set(cn => cn.State, cn => CaseNotificationState.New)
                        .Execute();
                    },
                }.Register();

                new Execute(CaseActivityOperation.Undo)
                {
                    FromStates = { CaseActivityState.Done },
                    ToStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    CanExecute = ca =>
                    {
                        if (!ca.DoneBy.Is(UserEntity.Current.ToLite()))
                            return CaseActivityMessage.Only0CanUndoThisOperation.NiceToString(ca.DoneBy);

                        if (!ca.NextActivities().All(na => na.IsFreshNew()))
                            return CaseActivityMessage.NextActivityAlreadyInProgress.NiceToString();

                        if (ca.Case.ParentCase != null && !ca.Case.InDB().SelectMany(c=>c.DecompositionSurrogateActivity().NextActivities()).All(na =>na.IsFreshNew()))
                            return CaseActivityMessage.NextActivityOfDecompositionSurrogateAlreadyInProgress.NiceToString();

                        return null;
                    },
                    Execute = (ca, args) =>
                    {
                        ca.NextActivities().SelectMany(a => a.Notifications()).UnsafeDelete();
                        var cases = ca.NextActivities().Select(a => a.Case).ToList();
                        cases.Remove(ca.Case);
                        ca.NextActivities().UnsafeDelete();
                        //Decomposition
                        if (cases.Any())
                            Database.Query<CaseEntity>().Where(c => cases.Contains(c) && !c.CaseActivities().Any()).UnsafeDelete();

                        //Recomposition
                        if(ca.Case.ParentCase != null && ca.Case.FinishDate.HasValue)
                        {
                            var surrogate = ca.Case.DecompositionSurrogateActivity();
                            surrogate.NextActivities().SelectMany(a => a.Notifications()).UnsafeDelete();
                            surrogate.NextActivities().UnsafeDelete();

                            surrogate.DoneBy = null;
                            surrogate.DoneDate = null;
                            surrogate.DoneType = null;
                            surrogate.Case.FinishDate = null;
                            surrogate.Save();
                        }

                        ca.DoneBy = null;
                        ca.DoneDate = null;
                        ca.DoneType = null;
                        ca.Case.FinishDate = null;
                        ca.Notifications()
                           .UnsafeUpdate()
                           .Set(a => a.State, a => CaseNotificationState.New)
                           .Execute();
                    },
                }.Register();

                new Execute(CaseActivityOperation.ScriptExecute)
                {
                    CanExecute = s => s.WorkflowActivity.Type != WorkflowActivityType.Script ? CaseActivityMessage.OnlyForScriptWorkflowActivities.NiceToString() : null,
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.Done },
                    Lite = false,
                    Execute = (ca, args) =>
                    {
                        var script = ca.WorkflowActivity.Script.Script.RetrieveFromCache();
                        script.Eval.Algorithm.ExecuteUntyped(ca.Case.MainEntity, new WorkflowScriptContext
                        {
                            CaseActivity = ca,
                            RetryCount = ca.ScriptExecution.RetryCount,
                        });
                        ExecuteStep(ca, null, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.ScriptScheduleRetry)
                {
                    CanExecute = s => s.WorkflowActivity.Type != WorkflowActivityType.Script ? CaseActivityMessage.OnlyForScriptWorkflowActivities.NiceToString() : null,
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.PendingNext },
                    Lite = false,
                    Execute = (ca, args) =>
                    {
                        ca.ScriptExecution.RetryCount++;
                        ca.ScriptExecution.NextExecution = args.GetArg<DateTime>();
                        ca.ScriptExecution.ProcessIdentifier = null;
                        ca.Save();
                    },
                }.Register();

                new Execute(CaseActivityOperation.ScriptFailureJump)
                {
                    CanExecute = s => s.WorkflowActivity.Type != WorkflowActivityType.Script ? CaseActivityMessage.OnlyForScriptWorkflowActivities.NiceToString() : null,
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.Done },
                    Lite = false,
                    Execute = (ca, args) =>
                    {
                        ExecuteStep(ca, null, ca.WorkflowActivity.Script);
                    },
                }.Register();
            }

            private static void CheckRequiresOpen(CaseActivityEntity ca)
            {
                if (ca.WorkflowActivity.RequiresOpen)
                {
                    if (!ca.Notifications().Any(cn => cn.User == UserEntity.Current.ToLite() && cn.State != CaseNotificationState.New))
                        throw new ApplicationException(CaseActivityMessage.TheActivity0RequiresToBeOpened.NiceToString(ca.WorkflowActivity));
                }
            }

    
            private static void ExecuteStep(CaseActivityEntity ca, DecisionResult? decisionResult, IWorkflowTransition transition)
            {
                using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = ca, WorkflowActivity = ca.WorkflowActivity, DecisionResult = decisionResult, Transition = transition }))
                {
                    SaveEntity(ca.Case.MainEntity);

                    ca.DoneBy = UserEntity.Current.ToLite();
                    ca.DoneDate = TimeZoneManager.Now;
                    ca.DoneType = transition is WorkflowJumpEmbedded ? DoneType.Jump :
                                  transition is WorkflowRejectEmbedded ? DoneType.Rejected :
                                  transition is WorkflowTimerEmbedded ? DoneType.Timeout :
                                  transition is WorkflowScriptPartEmbedded ? DoneType.ScriptFailure :
                                  decisionResult == DecisionResult.Approve ? DoneType.Approve :
                                  decisionResult == DecisionResult.Decline ? DoneType.Decline :
                                  ca.WorkflowActivity.Type == WorkflowActivityType.Script ? DoneType.ScriptSuccess :
                                  DoneType.Next;
                    ca.Case.Description = ca.Case.MainEntity.ToString().Trim();
                    ca.Save();

                    ca.Notifications()
                       .UnsafeUpdate()
                       .Set(a => a.State, a => a.User == UserEntity.Current.ToLite() ? CaseNotificationState.Done : CaseNotificationState.DoneByOther)
                       .Execute();

                    var ctx = new WorkflowExecuteStepContext
                    {
                        Case = ca.Case,
                        CaseActivity = ca,
                        DecisionResult = decisionResult,
                    };

                    if (transition != null)
                    {
                        var to =
                            transition is WorkflowJumpEmbedded jump ? jump.To.Retrieve() :
                            transition is WorkflowTimerEmbedded timer ? timer.To.Retrieve() :
                            transition is WorkflowScriptPartEmbedded ? ((IWorkflowTransitionTo)transition).To.Retrieve() :
                            transition is WorkflowRejectEmbedded ? ca.Previous.Retrieve().WorkflowActivity :
                            throw new NotImplementedException();

                        if (transition.Condition != null)
                        {
                            var jumpCtx = new WorkflowTransitionContext(ca.Case, ca, transition, null);
                            var alg = transition.Condition.RetrieveFromCache().Eval.Algorithm;
                            var result = alg.EvaluateUntyped(ca.Case.MainEntity, jumpCtx);
                            if (!result)
                                throw new ApplicationException(WorkflowMessage.JumpTo0FailedBecause1.NiceToString(to, transition.Condition));
                        }

                        ctx.Connections.Add(transition);
                        if (!FindNext(to, ctx))
                            return;
                    }
                    else
                    {
                        var connection = ca.WorkflowActivity.NextConnectionsFromCache().SingleEx();
                        if (!FindNext(connection, ctx))
                            return;
                    }

                    FinishStep(ca.Case, ctx, ca);
                }
            }

            private static void FinishStep(CaseEntity @case, WorkflowExecuteStepContext ctx, CaseActivityEntity ca)
            {
                ctx.Connections.ForEach(wc => WorkflowAction(@case.MainEntity, new WorkflowTransitionContext(@case, ca, wc, ctx.DecisionResult)));

                @case.Description = @case.MainEntity.ToString().Trim();

                if (ctx.IsFinished)
                {
                    if (ctx.ToActivities.Any())
                        throw new InvalidOperationException("ToActivities should be empty when finishing");

                    @case.FinishDate = ca.DoneDate.Value;
                    @case.Save();

                    if (@case.ParentCase != null)
                        TryToRecompose(@case);
                }
                else
                {
                    CreateNextActivities(@case, ctx, ca);
                }
            }

            private static void CreateNextActivities(CaseEntity @case, WorkflowExecuteStepContext ctx, CaseActivityEntity ca)
            {
                @case.Save();

                foreach (var t2 in ctx.ToActivities)
                {
                    if (t2.Type == WorkflowActivityType.DecompositionWorkflow || t2.Type == WorkflowActivityType.CallWorkflow)
                    {
                        var lastConn =
                            (IWorkflowTransition)ctx.Connections.OfType<WorkflowJumpEmbedded>().SingleOrDefaultEx() ??
                            (IWorkflowTransition)ctx.Connections.OfType<WorkflowConnectionEntity>().Single(a => a.To.Is(t2));

                        Decompose(@case, ca, t2, lastConn);
                    }
                    else
                    {
                        var nca = InsertNewCaseActivity(@case, t2, ca);
                        InsertCaseActivityNotifications(nca);
                    }
                }
            }

            private static void ExecuteTimerFork(CaseActivityEntity ca, WorkflowTimerEmbedded timer)
            {
                using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = ca, WorkflowActivity = ca.WorkflowActivity, DecisionResult = null, Transition = timer }))
                {
                    var @case = ca.Case;
                    var ctx = new WorkflowExecuteStepContext
                    {
                        Case = @case,
                        CaseActivity = ca,
                        DecisionResult = null,
                    };

                    ctx.Connections.Add(timer);
                    if (!FindNext(timer.To.Retrieve(), ctx))
                        return;

                    ctx.Connections.ForEach(wc => WorkflowAction(@case.MainEntity, new WorkflowTransitionContext(@case, ca, wc, ctx.DecisionResult)));

                    CreateNextActivities(@case, ctx, ca);
                }
            }

            private static void ExecuteInitialStep(CaseEntity  @case, WorkflowEventEntity @event, WorkflowConnectionEntity transition)
            {

                SaveEntity(@case.MainEntity);
                
                @case.Description = @case.MainEntity.ToString().Trim();
                @case.Save();
                
                var ctx = new WorkflowExecuteStepContext
                {
                    Case = @case
                };
             
                if (transition.Condition != null)
                {
                    var jumpCtx = new WorkflowTransitionContext(@case, null, transition, null);
                    var alg = transition.Condition.RetrieveFromCache().Eval.Algorithm;
                    var result = alg.EvaluateUntyped(@case.MainEntity, jumpCtx);
                    if (!result)
                        throw new ApplicationException(WorkflowMessage.JumpTo0FailedBecause1.NiceToString(transition, transition.Condition));
                }

                ctx.Connections.Add(transition);
                if (!FindNext(transition, ctx))
                    return;

                FinishStep(@case, ctx, null);
            }

            static CaseActivityEntity InsertNewCaseActivity(CaseEntity @case, WorkflowActivityEntity workflowActivity, CaseActivityEntity previous)
            {
                return new CaseActivityEntity
                {
                    StartDate = previous?.DoneDate ?? TimeZoneManager.Now,
                    Previous = previous?.ToLite(),
                    WorkflowActivity = workflowActivity,
                    OriginalWorkflowActivityName = workflowActivity.Name,
                    Case = @case,
                    ScriptExecution = workflowActivity.Type == WorkflowActivityType.Script ? new ScriptExecutionEmbedded
                    {
                        NextExecution = TimeZoneManager.Now,
                        RetryCount = 0,
                    } : null
                }.Save();
            }

            private static void TryToRecompose(CaseEntity childCase)
            {
                if(Database.Query<CaseEntity>().Where(cc => cc.ParentCase.Is(childCase.ParentCase) && cc.Workflow == childCase.Workflow).All(a => a.FinishDate.HasValue))
                {
                    var decompositionCaseActivity = childCase.DecompositionSurrogateActivity();
                    if (decompositionCaseActivity.DoneDate != null)
                        throw new InvalidOperationException("The DecompositionCaseActivity is already finished");
                    
                    var lastActivities = Database.Query<CaseEntity>().Where(c => c.ParentCase.Is(childCase.ParentCase)).Select(c => c.CaseActivities().OrderByDescending(ca => ca.DoneDate).FirstOrDefault()).ToList();
                    decompositionCaseActivity.Note = lastActivities.NotNull().Where(ca => ca.Note.HasText()).ToString(a => $"{a.DoneBy}: {a.Note}", "\r\n");
                    ExecuteStep(decompositionCaseActivity, null, null);
                }
            }

            private static void Decompose(CaseEntity @case, CaseActivityEntity previous, WorkflowActivityEntity decActivity, IWorkflowTransition conn)
            {
                var surrogate = InsertNewCaseActivity(@case, decActivity, previous);
                var subEntities = decActivity.SubWorkflow.SubEntitiesEval.Algorithm.GetSubEntities(@case.MainEntity, new WorkflowTransitionContext(@case, previous, conn, null));
                if (decActivity.Type == WorkflowActivityType.CallWorkflow && subEntities.Count > 1)
                    throw new InvalidOperationException("More than one entity generated using CallWorkflow. Use DecompositionWorkflow instead.");

                if (subEntities.IsEmpty())
                    ExecuteStep(surrogate, null, null);
                else
                {
                    var subWorkflow = decActivity.SubWorkflow.Workflow;
                    foreach (var se in subEntities)
                    {
                        var caseActivity = subWorkflow.ConstructFrom(CaseActivityOperation.CreateCaseActivityFromWorkflow, se, @case);
                        caseActivity.Previous = surrogate.ToLite();
                        caseActivity.Execute(CaseActivityOperation.Register);
                    }
                }
            }

            private static bool FindNext(WorkflowConnectionEntity connection, WorkflowExecuteStepContext ctx)
            {
                ctx.Connections.Add(connection);
                return FindNext(connection.To, ctx);
            }

            private static bool FindNext(IWorkflowNodeEntity next, WorkflowExecuteStepContext ctx)
            {
                if (next is WorkflowEventEntity ne)
                {
                    if (ne.Type == WorkflowEventType.Finish)
                    {
                        ctx.IsFinished = true;
                        return true;
                    }

                    throw new NotImplementedException($"Unexpected {nameof(WorkflowEventType)} {ne.Type}");
                }
                else if (next is WorkflowActivityEntity)
                {
                    ctx.ToActivities.Add((WorkflowActivityEntity)next);
                    return true;
                }
                else
                {
                    var gateway = (WorkflowGatewayEntity)next;

                    switch (gateway.Type)
                    {
                        case WorkflowGatewayType.Exclusive:
                            if (gateway.Direction == WorkflowGatewayDirection.Split)
                            {
                                var firstConnection = gateway.NextConnectionsFromCache()
                                    .GroupBy(c => c.Order)
                                    .OrderBy(gr => gr.Key)
                                    .Select(gr => gr.SingleOrDefaultEx(c => c.Applicable(ctx)))
                                    .NotNull()
                                    .FirstEx();

                                return FindNext(firstConnection, ctx);
                            }
                            else //if (gateway.Direction == WorkflowGatewayDirection.Join)
                            {
                                var singleConnection = gateway.NextConnectionsFromCache().SingleEx();
                                return FindNext(singleConnection, ctx);
                            }

                        case WorkflowGatewayType.Parallel:
                        case WorkflowGatewayType.Inclusive:
                            if (gateway.Direction == WorkflowGatewayDirection.Split)
                            {
                                var applicable = gateway.NextConnectionsFromCache().ToList().Where(c =>
                                {
                                    var app = c.Applicable(ctx);
                                    if (!app && gateway.Type == WorkflowGatewayType.Parallel)
                                        throw new InvalidOperationException($"Conditions not allowed in {WorkflowGatewayType.Parallel} {WorkflowGatewayDirection.Split}!");
                                    return app;
                                }).ToList();

                                if (applicable.IsEmpty())
                                {
                                    var join = WorkflowLogic.GetWorkflowNodeGraph(gateway.Lane.Pool.Workflow.ToLite()).ParallelWorkflowPairs.GetOrThrow(gateway);
                                    return FindNext(join, ctx);
                                }
                                else
                                {
                                    foreach (var con in applicable)
                                        FindNext(con, ctx);
                                    return true;
                                }
                            }
                            else //if (gateway.Direction == WorkflowGatewayDirection.Join)
                            {
                                if (!FindPrevious(0, gateway, ctx))
                                    return false;

                                var singleConnection = gateway.NextConnectionsFromCache().SingleEx();
                                return FindNext(singleConnection, ctx);
                            }
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }
            
            private static bool FindPrevious(int depth, IWorkflowNodeEntity node, WorkflowExecuteStepContext ctx)
            {
                if (node is WorkflowEventEntity)
                {
                    throw new InvalidOperationException($"Unexpected {nameof(WorkflowEventEntity)} in {nameof(FindPrevious)}");
                }
                else if (node is WorkflowActivityEntity wa)
                {
                    if (wa.Is(ctx.CaseActivity.WorkflowActivity))
                        return true;

                    // Parallel gateways always have CaseActivity but for Inclusive gateways maybe not be created because of conditions
                    var last = ctx.Case.CaseActivities().Where(a => a.WorkflowActivity == wa).OrderBy(a => a.StartDate).LastOrDefault();
                    if (last != null)
                        return (last.DoneDate.HasValue);
                    else
                    {
                        // We should continue backtracking reaching an CaseActivity or gateways with split direction
                        var prevsConnections = node.PreviousConnectionsFromCache().Select(a => a.From).ToList();
                        return prevsConnections.All(wn => FindPrevious(depth, wn, ctx));
                    }
                }
                else if (node is WorkflowGatewayEntity g)
                {
                    if (g.Type != WorkflowGatewayType.Exclusive)
                    {
                        depth += (g.Direction == WorkflowGatewayDirection.Split ? -1 : 1);
                        if (depth == 0)
                            return true;
                    }

                    switch (g.Type)
                    {
                        case WorkflowGatewayType.Exclusive:
                            {
                                var prevsExclusive = g.PreviousConnectionsFromCache().Select(a => a.From).ToList();
                                return prevsExclusive.Any(wn => FindPrevious(depth, wn, ctx));
                            }
                        case WorkflowGatewayType.Parallel:
                        case WorkflowGatewayType.Inclusive:
                            {
                                var prevsParallelAndInclusive = g.PreviousConnectionsFromCache().Select(a => a.From).ToList();
                                return prevsParallelAndInclusive.All(wn => FindPrevious(depth, wn, ctx));
                            }
                        default:
                            throw new InvalidOperationException();
                    }
                }
                throw new InvalidOperationException();
            }
        }
    }
}
