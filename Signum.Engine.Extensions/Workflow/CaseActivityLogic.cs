using Signum.Entities.Workflow;
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
using Signum.Entities.Dynamic;
using Signum.Engine.Basics;
using Signum.Engine.Scheduler;
using Signum.Engine.Processes;
using Signum.Entities.Processes;
using Signum.Engine.Alerts;

namespace Signum.Engine.Workflow
{
    public static class CaseActivityLogic
    {
        static Expression<Func<ICaseMainEntity, IQueryable<CaseActivityEntity>>> MainEntityCaseActivitiesExpression =
            e => Database.Query<CaseActivityEntity>().Where(a => a.Case.MainEntity == e);
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> CaseActivities(this ICaseMainEntity e)
        {
            return MainEntityCaseActivitiesExpression.Evaluate(e);
        }

        static Expression<Func<ICaseMainEntity, CaseActivityEntity>> LastCaseActivityExpression =
            e => e.CaseActivities().OrderByDescending(a => a.StartDate).FirstOrDefault();
        [ExpressionField]
        public static CaseActivityEntity LastCaseActivity(this ICaseMainEntity e)
        {
            return LastCaseActivityExpression.Evaluate(e);
        }

        static Expression<Func<ICaseMainEntity, IQueryable<CaseEntity>>> MainEntityCasesExpression =
            e => e.CaseActivities().Select(a => a.Case);
        [ExpressionField]
        public static IQueryable<CaseEntity> Cases(this ICaseMainEntity e)
        {
            return MainEntityCasesExpression.Evaluate(e);
        }

        static Expression<Func<ICaseMainEntity, bool>> MainEntityCurrentUserHasNotificationExpression =
            e => e.CaseActivities().SelectMany(a => a.Notifications()).Any(a => a.User.Is(UserEntity.Current));
        [ExpressionField]
        public static bool CurrentUserHasNotification(this ICaseMainEntity e)
        {
            return MainEntityCurrentUserHasNotificationExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowEntity, IQueryable<CaseEntity>>> CasesExpression =
            w => Database.Query<CaseEntity>().Where(a => a.Workflow == w);
        [ExpressionField]
        public static IQueryable<CaseEntity> Cases(this WorkflowEntity e)
        {
            return CasesExpression.Evaluate(e);
        }

        static Expression<Func<CaseActivityEntity, bool>> CurrentUserHasNotificationExpression =
            ca => ca.Notifications().Any(cn => cn.User.Is(UserEntity.Current) &&
                                                (cn.State == CaseNotificationState.New ||
                                                 cn.State == CaseNotificationState.Opened ||
                                                 cn.State == CaseNotificationState.InProgress));
        [ExpressionField]
        public static bool CurrentUserHasNotification(this CaseActivityEntity e)
        {
            return CurrentUserHasNotificationExpression.Evaluate(e);
        }

        static Expression<Func<CaseActivityEntity, IQueryable<CaseActivityEntity>>> NextActivitiesExpression =
            ca => Database.Query<CaseActivityEntity>().Where(a => a.Previous.Is(ca));
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
  
        
        static Expression<Func<IWorkflowNodeEntity, IQueryable<CaseActivityEntity>>> CaseActivitiesExpression =
            e => Database.Query<CaseActivityEntity>().Where(a => a.WorkflowActivity == e);
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> CaseActivities(this IWorkflowNodeEntity e)
        {
            return CaseActivitiesExpression.Evaluate(e);
        }

        static Expression<Func<WorkflowActivityEntity, double?>> AverageDurationActivityExpression =
        wa => wa.CaseActivities().Average(a => a.Duration);
        [ExpressionField]
        public static double? AverageDuration(this WorkflowActivityEntity wa)
        {
            return AverageDurationActivityExpression.Evaluate(wa);
        }

        static Expression<Func<WorkflowEventEntity, double?>> AverageDurationEventExpression =
            we => we.CaseActivities().Average(a => a.Duration);
        [ExpressionField]
        public static double? AverageDuration(this WorkflowEventEntity we)
        {
            return AverageDurationEventExpression.Evaluate(we);
        }

        static Expression<Func<CaseEntity, IQueryable<CaseActivityEntity>>> CaseActivitiesFromCaseExpression =
            e => Database.Query<CaseActivityEntity>().Where(a => a.Case == e);
        [ExpressionField]
        public static IQueryable<CaseActivityEntity> CaseActivities(this CaseEntity e)
        {
            return CaseActivitiesFromCaseExpression.Evaluate(e);
        }


        static Expression<Func<CaseEntity, IQueryable<CaseTagEntity>>> TagsExpression =
        e => Database.Query<CaseTagEntity>().Where(a => a.Case.Is(e));
        [ExpressionField]
        public static IQueryable<CaseTagEntity> Tags(this CaseEntity e)
        {
            return TagsExpression.Evaluate(e);
        }

        static Expression<Func<CaseActivityEntity, IQueryable<CaseNotificationEntity>>> NotificationsExpression =
            e => Database.Query<CaseNotificationEntity>().Where(a => a.CaseActivity.Is(e));
        [ExpressionField]
        public static IQueryable<CaseNotificationEntity> Notifications(this CaseActivityEntity e)
        {
            return NotificationsExpression.Evaluate(e);
        }


        static Expression<Func<CaseActivityEntity, IQueryable<CaseActivityExecutedTimerEntity>>> ExecutedTimersExpression =
        e => Database.Query<CaseActivityExecutedTimerEntity>().Where(a => a.CaseActivity.Is(e));
        [ExpressionField]
        public static IQueryable<CaseActivityExecutedTimerEntity> ExecutedTimers(this CaseActivityEntity e)
        {
            return ExecutedTimersExpression.Evaluate(e);
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<CaseEntity>()
                    .WithExpressionFrom((WorkflowEntity w) => w.Cases())
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Description,
                        e.Workflow,
                        e.MainEntity,
                    });

                sb.Include<CaseTagTypeEntity>()
                    .WithSave(CaseTagTypeOperation.Save)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Color
                    });


                sb.Include<CaseTagEntity>()
                    .WithExpressionFrom((CaseEntity ce) => ce.Tags())
                    .WithQuery(() => e => new
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
                    .WithExpressionFrom((WorkflowActivityEntity c) => c.CaseActivities())
                    .WithExpressionFrom((CaseEntity c) => c.CaseActivities())
                    .WithExpressionFrom((CaseActivityEntity c) => c.NextActivities())
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.WorkflowActivity,
                        e.StartDate,
                        e.DoneDate,
                        e.DoneBy,
                        e.Previous,
                        e.Case,
                    });


                sb.Include<CaseActivityExecutedTimerEntity>()
                    .WithExpressionFrom((CaseActivityEntity ca) => ca.ExecutedTimers())
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.CreationDate,
                        e.CaseActivity,
                        e.BoundaryEvent,
                    });

                QueryLogic.Expressions.Register((WorkflowActivityEntity a) => a.AverageDuration(), () => WorkflowActivityMessage.AverageDuration.NiceToString());

                SimpleTaskLogic.Register(CaseActivityTask.Timeout, (ScheduledTaskContext ctx) =>
                {
                    var boundaryCandidates =
                    (from ca in Database.Query<CaseActivityEntity>()
                     where ca.State == CaseActivityState.PendingDecision || ca.State == CaseActivityState.PendingNext
                     from we in ((WorkflowActivityEntity)ca.WorkflowActivity).BoundaryTimers
                     where we.Type == WorkflowEventType.BoundaryInterruptingTimer ? true :
                     we.Type == WorkflowEventType.BoundaryForkTimer ? !ca.ExecutedTimers().Any(t => t.BoundaryEvent.Is(we)) :
                     false
                     select new { Activity = ca, Event = we }).ToList();


                    var intermediateCandidates =
                    (from ca in Database.Query<CaseActivityEntity>()
                     where ca.State == CaseActivityState.PendingDecision || ca.State == CaseActivityState.PendingNext
                     let we = ((WorkflowEventEntity)ca.WorkflowActivity)
                     where we.Type == WorkflowEventType.IntermediateTimer
                     select new { Activity = ca, Event = we }).ToList();

                    var candidates = boundaryCandidates.Concat(intermediateCandidates).Distinct().ToList();
                    var conditions = candidates.Select(a => a.Event.Timer.Condition).Distinct().ToList();

                    var now = TimeZoneManager.Now;
                    var activities = conditions.SelectMany(cond =>
                    {
                        if (cond == null)
                            return candidates.Where(a => a.Event.Timer.Duration != null && a.Event.Timer.Duration.Add(a.Activity.StartDate) < now).Select(a => a.Activity.ToLite()).ToList();

                        var condEval = cond.RetrieveFromCache().Eval.Algorithm;

                        return candidates.Where(a => a.Event.Timer.Condition.Is(cond) && condEval.EvaluateUntyped(a.Activity, now)).Select(a => a.Activity.ToLite()).ToList();
                    }).Distinct().ToList();

                    if (!activities.Any())
                        return null;

                    var pkg = new PackageEntity().CreateLines(activities);

                    return ProcessLogic.Create(CaseActivityProcessAlgorithm.Timeout, pkg).Execute(ProcessOperation.Execute).ToLite();
                });
                ProcessLogic.Register(CaseActivityProcessAlgorithm.Timeout, new PackageExecuteAlgorithm<CaseActivityEntity>(CaseActivityOperation.Timer));

                QueryLogic.Expressions.Register((CaseEntity c) => c.DecompositionSurrogateActivity());
                QueryLogic.Expressions.Register((CaseActivityEntity ca) => ca.CurrentUserHasNotification(), () => CaseActivityMessage.CurrentUserHasNotification.NiceToString());
                QueryLogic.Expressions.Register((ICaseMainEntity a) => a.CaseActivities(), () => typeof(CaseActivityEntity).NicePluralName());
                QueryLogic.Expressions.Register((ICaseMainEntity a) => a.Cases(), () => typeof(CaseEntity).NicePluralName());
                QueryLogic.Expressions.Register((ICaseMainEntity a) => a.LastCaseActivity(), () => CaseActivityMessage.LastCaseActivity.NiceToString());
                QueryLogic.Expressions.Register((ICaseMainEntity a) => a.CurrentUserHasNotification(), () => CaseActivityMessage.CurrentUserHasNotification.NiceToString());

                sb.Include<CaseNotificationEntity>()
                    .WithExpressionFrom((CaseActivityEntity c) => c.Notifications())
                    .WithQuery(() => e => new
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


                QueryLogic.Queries.Register(CaseActivityQuery.Inbox, () => DynamicQueryCore.Auto(
                        from cn in Database.Query<CaseNotificationEntity>()
                        where cn.User == UserEntity.Current.ToLite()
                        let ca = cn.CaseActivity.Entity
                        let previous = ca.Previous.Entity
                        select new
                        {
                            Entity = cn.CaseActivity,
                            ca.StartDate,
                            Workflow = ca.Case.Workflow.ToLite(),
                            Activity = new ActivityWithRemarks
                            {
                                workflowActivity = ((WorkflowActivityEntity)ca.WorkflowActivity).ToLite(),
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
            public CaseEntity Case;
            public CaseActivityEntity CaseActivity;
            public List<WorkflowActivityEntity> ToActivities = new List<WorkflowActivityEntity>();
            public List<WorkflowEventEntity> ToIntermediateEvents = new List<WorkflowEventEntity>();
            public bool IsFinished { get; set; }
            public List<WorkflowConnectionEntity> Connections = new List<WorkflowConnectionEntity>();

            public void ExecuteConnection(WorkflowConnectionEntity connection)
            {
                var wctx = new WorkflowTransitionContext(Case, CaseActivity, connection);

                WorkflowLogic.OnTransition?.Invoke(Case.MainEntity, wctx);

                if (connection.Action != null)
                {
                    var alg = connection.Action.RetrieveFromCache().Eval.Algorithm;
                    alg.ExecuteUntyped(Case.MainEntity, wctx);
                };
                
                this.Connections.Add(connection);
            }
        }

        static bool Applicable(this WorkflowConnectionEntity wc, WorkflowExecuteStepContext ctx)
        {
            var doneType = 
                wc.Type == ConnectionType.Approve ? DoneType.Approve :
                wc.Type == ConnectionType.Decline ? DoneType.Decline : 
                (DoneType?)null;

            if (doneType != null && doneType != ctx.CaseActivity?.DoneType)
                return false;

            if (wc.Condition != null)
            {
                var alg = wc.Condition.RetrieveFromCache().Eval.Algorithm;
                var result = alg.EvaluateUntyped(ctx.Case.MainEntity, new WorkflowTransitionContext(ctx.Case, ctx.CaseActivity, wc));


                return result;
            }
            
            return true;
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

        public static void InsertCaseActivityNotifications(CaseActivityEntity caseActivity)
        {
            if (caseActivity.WorkflowActivity is WorkflowActivityEntity wa && 
                (wa.Type == WorkflowActivityType.Task ||  
                wa.Type == WorkflowActivityType.Decision))
            {
                using (ExecutionMode.Global())
                {
                    var lane = caseActivity.WorkflowActivity.Lane;
                    var actors = lane.Actors.ToList();
                    if (lane.ActorsEval != null)
                        actors = lane.ActorsEval.Algorithm.GetActors(caseActivity.Case.MainEntity, new WorkflowTransitionContext(caseActivity.Case, caseActivity, null)).EmptyIfNull().ToList();

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

                    if (!notifications.Any())
                        throw new ApplicationException(CaseActivityMessage.NoActorsFoundToInsertCaseActivityNotifications.NiceToString());

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
                        if (w.HasExpired())
                            throw new InvalidOperationException(WorkflowMessage.Workflow0HasExpiredOn1.NiceToString(w, w.ExpirationDate.Value.ToString()));

                        var mainEntity = args.TryGetArgC<ICaseMainEntity>() ?? CaseActivityLogic.Options.GetOrThrow(w.MainEntityType.ToType()).Constructor();

                        var @case = new CaseEntity
                        {
                            ParentCase = args.TryGetArgC<CaseEntity>(),
                            Workflow = w,
                            Description = w.Name,
                            MainEntity = mainEntity,
                        };

                        var start = w.WorkflowEvents().Single(a => a.Type == WorkflowEventType.Start);
                        var connection = start.NextConnectionsFromCache(ConnectionType.Normal).SingleEx();
                        var next = (WorkflowActivityEntity)connection.To;
                        var ca = new CaseActivityEntity
                        {
                            WorkflowActivity = next,
                            OriginalWorkflowActivityName = next.Name,
                            Case = @case,
                        };

                        new WorkflowExecuteStepContext
                        {
                            Case = @case,
                            CaseActivity = ca,
                        }.ExecuteConnection(connection);
                       
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
                        ExecuteInitialStep(@case, start, start.NextConnectionsFromCache(ConnectionType.Normal).SingleEx());

                        return @case;
                    }
                }.Register();

                new Execute(CaseActivityOperation.Register)
                {
                    CanExecute = ca => !(ca.WorkflowActivity is WorkflowActivityEntity) ? CaseActivityMessage.NoWorkflowActivity.NiceToString() : null,
                    FromStates = {  CaseActivityState.New },
                    ToStates = {  CaseActivityState.PendingNext, CaseActivityState.PendingDecision},
                    CanBeNew = true,
                    CanBeModified = true,
                    Execute = (ca, _) =>
                    {
                        SaveEntity(ca.Case.MainEntity);
                        var now = TimeZoneManager.Now;
                        var c = ca.Case;
                        c.StartDate = now;
                        c.Description = ca.Case.MainEntity.ToString().Trim().Etc(100);
                        c.Save();

                        var prevConn = ca.WorkflowActivity.PreviousConnectionsFromCache().SingleEx(a => a.From is WorkflowEventEntity && ((WorkflowEventEntity)a.From).Type == WorkflowEventType.Start);

                        new WorkflowExecuteStepContext
                        {
                            Case = ca.Case,
                            CaseActivity = ca,
                        }.ExecuteConnection(prevConn);

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
                    !ca.CurrentUserHasNotification() ? CaseActivityMessage.NoNewOrOpenedOrInProgressNotificationsFound.NiceToString() : null,
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
                    CanExecute = ca => !(ca.WorkflowActivity is WorkflowActivityEntity) ? CaseActivityMessage.NoWorkflowActivity.NiceToString() : 
                    !ca.CurrentUserHasNotification() ? CaseActivityMessage.NoNewOrOpenedOrInProgressNotificationsFound.NiceToString() : null,
                    FromStates = {  CaseActivityState.PendingDecision },
                    ToStates = {  CaseActivityState.Done },
                    CanBeModified = true,
                    Execute = (ca, _) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, DoneType.Approve, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Decline)
                {
                    CanExecute = ca => !(ca.WorkflowActivity is WorkflowActivityEntity) ? CaseActivityMessage.NoWorkflowActivity.NiceToString() :
                    !ca.CurrentUserHasNotification() ? CaseActivityMessage.NoNewOrOpenedOrInProgressNotificationsFound.NiceToString() : null,
                    FromStates = { CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done },
                    CanBeModified = true,
                    Execute = (ca, _) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, DoneType.Decline, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Next)
                {
                    CanExecute = ca => !(ca.WorkflowActivity is WorkflowActivityEntity) ? CaseActivityMessage.NoWorkflowActivity.NiceToString() :
                    !ca.CurrentUserHasNotification() ? CaseActivityMessage.NoNewOrOpenedOrInProgressNotificationsFound.NiceToString() : null,
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.Done },
                    CanBeModified = true,
                    Execute = (ca, args) =>
                    {
                        CheckRequiresOpen(ca);
                        ExecuteStep(ca, DoneType.Next, null);
                    },
                }.Register();

            

                new Execute(CaseActivityOperation.Jump)
                {
                    CanExecute = ca => !(ca.WorkflowActivity is WorkflowActivityEntity) ? CaseActivityMessage.NoWorkflowActivity.NiceToString() : 
                    ca.WorkflowActivity.NextConnectionsFromCache(ConnectionType.Jump).IsEmpty() ? CaseActivityMessage.Activity0HasNoJumps.NiceToString(ca.WorkflowActivity) :
                    !ca.CurrentUserHasNotification() ? CaseActivityMessage.NoNewOrOpenedOrInProgressNotificationsFound.NiceToString() : null,
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done },
                    CanBeModified = true,
                    Execute = (ca, args) =>
                    {
                        CheckRequiresOpen(ca);
                        var to = args.GetArg<Lite<IWorkflowNodeEntity>>();
                        var jump = ca.WorkflowActivity.NextConnectionsFromCache(ConnectionType.Jump).SingleEx(c => to.Is(c.To));
                        ExecuteStep(ca, DoneType.Jump, jump);
                    },
                }.Register();

                new Execute(CaseActivityOperation.Timer)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.Done, CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    CanExecute = ca => (ca.WorkflowActivity is WorkflowEventEntity we && we.Type.IsTimer() || 
                    ca.WorkflowActivity is WorkflowActivityEntity wa && wa.BoundaryTimers.Any()) ?  null : CaseActivityMessage.Activity0HasNoTimers.NiceToString(ca.WorkflowActivity),
                    Execute = (ca, _) =>
                    {
                        var now = TimeZoneManager.Now;

                        var alreadyExecuted = ca.ExecutedTimers().Select(a => a.BoundaryEvent).ToHashSet();

                        var candidateEvents = ca.WorkflowActivity is WorkflowEventEntity @event ? new WorkflowEventEntity[] { @event } :
                        ((WorkflowActivityEntity)ca.WorkflowActivity).BoundaryTimers.ToArray();

                        var timer = candidateEvents.Where(e => e.Type == WorkflowEventType.BoundaryInterruptingTimer || !alreadyExecuted.Contains(e.ToLite())).FirstOrDefault(t =>
                        {
                            if (t.Timer.Duration != null)
                                return t.Timer.Duration.Add(ca.StartDate) < now;

                            return t.Timer.Condition.RetrieveFromCache().Eval.Algorithm.EvaluateUntyped(ca, now);
                        });
                        
                        if (timer == null)
                            throw new InvalidOperationException(WorkflowActivityMessage.NoActiveTimerFound.NiceToString());

                        switch (timer.Type)
                        {
                            case WorkflowEventType.BoundaryForkTimer:
                                ExecuteTimerFork(ca, timer);
                                break;
                            case WorkflowEventType.BoundaryInterruptingTimer:
                            case WorkflowEventType.IntermediateTimer:
                                ExecuteStep(ca, DoneType.Timeout, timer.NextConnectionsFromCache(ConnectionType.Normal).SingleEx());
                                break;
                            default:
                                throw new InvalidOperationException("Unexpected Timer Type " + timer.Type);
                        }
                    },
                }.Register();

                new Execute(CaseActivityOperation.MarkAsUnread)
                {
                    FromStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    ToStates = { CaseActivityState.PendingNext, CaseActivityState.PendingDecision },
                    CanExecute = c=> c.Notifications().Any(a=>a.User.Is(UserEntity.Current) && (a.State == CaseNotificationState.InProgress || a.State == CaseNotificationState.Opened)) ? null :
                        CaseActivityMessage.NoOpenedOrInProgressNotificationsFound.NiceToString(),
                    Execute = (ca, args) =>
                    {
                        ca.Notifications()
                        .Where(cn => cn.User.Is(UserEntity.Current) && (cn.State == CaseNotificationState.InProgress || cn.State == CaseNotificationState.Opened))
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
                    CanExecute = s => s.WorkflowActivity is WorkflowActivityEntity wa && wa.Type == WorkflowActivityType.Script ? null : CaseActivityMessage.OnlyForScriptWorkflowActivities.NiceToString(),
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.Done },
                    Execute = (ca, args) =>
                    {
                        var script = ((WorkflowActivityEntity)ca.WorkflowActivity).Script.Script.RetrieveFromCache();
                        script.Eval.Algorithm.ExecuteUntyped(ca.Case.MainEntity, new WorkflowScriptContext
                        {
                            CaseActivity = ca,
                            RetryCount = ca.ScriptExecution.RetryCount,
                        });
                        ExecuteStep(ca, DoneType.ScriptSuccess, null);
                    },
                }.Register();

                new Execute(CaseActivityOperation.ScriptScheduleRetry)
                {
                    CanExecute = s => s.WorkflowActivity is WorkflowActivityEntity wa && wa.Type == WorkflowActivityType.Script ? null : CaseActivityMessage.OnlyForScriptWorkflowActivities.NiceToString(),
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.PendingNext },
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
                    CanExecute = s => s.WorkflowActivity is WorkflowActivityEntity wa && wa.Type == WorkflowActivityType.Script ? null : CaseActivityMessage.OnlyForScriptWorkflowActivities.NiceToString(),
                    FromStates = { CaseActivityState.PendingNext },
                    ToStates = { CaseActivityState.Done },
                    Execute = (ca, args) =>
                    {
                        ExecuteStep(ca, DoneType.ScriptFailure, ca.WorkflowActivity.NextConnectionsFromCache(ConnectionType.ScriptException).SingleEx());
                    },
                }.Register();
            }

            private static void CheckRequiresOpen(CaseActivityEntity ca)
            {
                if (((WorkflowActivityEntity)ca.WorkflowActivity).RequiresOpen)
                {
                    if (!ca.Notifications().Any(cn => cn.User == UserEntity.Current.ToLite() && cn.State != CaseNotificationState.New))
                        throw new ApplicationException(CaseActivityMessage.TheActivity0RequiresToBeOpened.NiceToString(ca.WorkflowActivity));
                }
            }


            private static void ExecuteStep(CaseActivityEntity ca, DoneType doneType, WorkflowConnectionEntity firstConnection)
            {
                using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = ca, Connection = firstConnection }))
                {
                    SaveEntity(ca.Case.MainEntity);
                }

                ca.DoneBy = UserEntity.Current.ToLite();
                ca.DoneDate = TimeZoneManager.Now;
                ca.DoneType = doneType;
                ca.Case.Description = ca.Case.MainEntity.ToString().Trim().Etc(100);
                ca.Save();

                ca.Notifications()
                   .UnsafeUpdate()
                   .Set(a => a.State, a => a.User == UserEntity.Current.ToLite() ? CaseNotificationState.Done : CaseNotificationState.DoneByOther)
                   .Execute();

                var ctx = new WorkflowExecuteStepContext
                {
                    Case = ca.Case,
                    CaseActivity = ca,
                };

                if (firstConnection != null)
                {
                    if (firstConnection.Condition != null)
                    {
                        var jumpCtx = new WorkflowTransitionContext(ca.Case, ca, firstConnection);
                        var alg = firstConnection.Condition.RetrieveFromCache().Eval.Algorithm;
                        var result = alg.EvaluateUntyped(ca.Case.MainEntity, jumpCtx);
                        if (!result)
                            throw new ApplicationException(WorkflowMessage.JumpTo0FailedBecause1.NiceToString(firstConnection.To, firstConnection.Condition));
                    }

                    ctx.ExecuteConnection(firstConnection);
                    if (!FindNext(firstConnection.To, ctx))
                        return;
                }
                else
                {
                    var connection = ca.WorkflowActivity.NextConnectionsFromCache(ConnectionType.Normal).SingleEx();
                    if (!FindNext(connection, ctx))
                        return;
                }

                FinishStep(ca.Case, ctx, ca);
            }

            private static void FinishStep(CaseEntity @case, WorkflowExecuteStepContext ctx, CaseActivityEntity ca)
            {
                
                @case.Description = @case.MainEntity.ToString().Trim().Etc(100);

                if (ctx.IsFinished)
                {
                    if (ctx.ToActivities.Any() || ctx.ToIntermediateEvents.Any())
                        throw new InvalidOperationException("ToActivities and ToIntermediateEvents should be empty when finishing");

                    if (@case.CaseActivities().Any(a => a.State == CaseActivityState.PendingNext || a.State == CaseActivityState.PendingDecision))
                        return;

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

                foreach (var twa in ctx.ToActivities)
                {
                    if (twa.Type == WorkflowActivityType.DecompositionWorkflow || twa.Type == WorkflowActivityType.CallWorkflow)
                    {
                        var lastConn = ctx.Connections.OfType<WorkflowConnectionEntity>().Single(a => a.To.Is(twa));

                        Decompose(@case, ca, twa, lastConn);
                    }
                    else
                    {
                        var nca = InsertNewCaseActivity(@case, twa, ca);
                        InsertCaseActivityNotifications(nca);
                    }
                }

                foreach (var twe in ctx.ToIntermediateEvents)
                {
                    InsertNewCaseActivity(@case, twe, ca);
                }
            }

            private static void ExecuteTimerFork(CaseActivityEntity ca, WorkflowEventEntity boundaryEvent)
            {
                var connection = boundaryEvent.NextConnectionsFromCache(ConnectionType.Normal).SingleEx();

                var @case = ca.Case;
                var ctx = new WorkflowExecuteStepContext
                {
                    Case = @case,
                    CaseActivity = ca,
                };

                ctx.ExecuteConnection(connection);
                if (!FindNext(connection.To, ctx))
                    return;

                CreateNextActivities(@case, ctx, ca);

                new CaseActivityExecutedTimerEntity
                {
                    BoundaryEvent = boundaryEvent.ToLite(),
                    CaseActivity = ca.ToLite(),
                }.Save();
            }

            private static void ExecuteInitialStep(CaseEntity  @case, WorkflowEventEntity @event, WorkflowConnectionEntity transition)
            {

                SaveEntity(@case.MainEntity);
                
                @case.Description = @case.MainEntity.ToString().Trim().Etc(100);
                @case.Save();
                
                var ctx = new WorkflowExecuteStepContext
                {
                    Case = @case
                };
             
                if (transition.Condition != null)
                {
                    var jumpCtx = new WorkflowTransitionContext(@case, null, transition);
                    var alg = transition.Condition.RetrieveFromCache().Eval.Algorithm;
                    var result = alg.EvaluateUntyped(@case.MainEntity, jumpCtx);
                    if (!result)
                        throw new ApplicationException(WorkflowMessage.JumpTo0FailedBecause1.NiceToString(transition, transition.Condition));
                }

                ctx.ExecuteConnection(transition);
                if (!FindNext(transition, ctx))
                    return;

                FinishStep(@case, ctx, null);
            }

            static CaseActivityEntity InsertNewCaseActivity(CaseEntity @case, IWorkflowNodeEntity workflowActivity, CaseActivityEntity previous)
            {
                return new CaseActivityEntity
                {
                    StartDate = previous?.DoneDate ?? TimeZoneManager.Now,
                    Previous = previous?.ToLite(),
                    WorkflowActivity = workflowActivity,
                    OriginalWorkflowActivityName = workflowActivity.Name,
                    Case = @case,
                    ScriptExecution = workflowActivity is WorkflowActivityEntity w && w.Type == WorkflowActivityType.Script ? new ScriptExecutionEmbedded
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
                    ExecuteStep(decompositionCaseActivity, DoneType.Recompose, null);
                }
            }

            private static void Decompose(CaseEntity @case, CaseActivityEntity previous, WorkflowActivityEntity decActivity, WorkflowConnectionEntity conn)
            {
                var surrogate = InsertNewCaseActivity(@case, decActivity, previous);
                var subEntities = decActivity.SubWorkflow.SubEntitiesEval.Algorithm.GetSubEntities(@case.MainEntity, new WorkflowTransitionContext(@case, previous, conn));
                if (decActivity.Type == WorkflowActivityType.CallWorkflow && subEntities.Count > 1)
                    throw new InvalidOperationException("More than one entity generated using CallWorkflow. Use DecompositionWorkflow instead.");

                if (subEntities.IsEmpty())
                    ExecuteStep(surrogate, DoneType.Recompose, null);
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
                ctx.ExecuteConnection(connection);
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
                    else if(ne.Type == WorkflowEventType.IntermediateTimer)
                    {
                        ctx.ToIntermediateEvents.Add(ne);
                        return true;
                    }

                    throw new NotImplementedException($"Unexpected {nameof(WorkflowEventType)} {ne.Type}");
                }
                else if (next is WorkflowActivityEntity na)
                {
                    ctx.ToActivities.Add(na);
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
                                var firstConnection = gateway.NextConnectionsFromCache(null)
                                    .Where(a => a.Type == ConnectionType.Approve || a.Type == ConnectionType.Decline || a.Type == ConnectionType.Normal)
                                    .GroupBy(c => c.Order)
                                    .OrderBy(gr => gr.Key)
                                    .Select(gr => gr.SingleOrDefaultEx(c => c.Applicable(ctx)))
                                    .NotNull()
                                    .FirstEx();

                                return FindNext(firstConnection, ctx);
                            }
                            else //if (gateway.Direction == WorkflowGatewayDirection.Join)
                            {
                                var singleConnection = gateway.NextConnectionsFromCache(ConnectionType.Normal).SingleEx();
                                return FindNext(singleConnection, ctx);
                            }

                        case WorkflowGatewayType.Parallel:
                        case WorkflowGatewayType.Inclusive:
                            if (gateway.Direction == WorkflowGatewayDirection.Split)
                            {
                                var applicable = gateway.NextConnectionsFromCache(null)
                                     .Where(a => 
                                     a.Type == ConnectionType.Approve || 
                                     a.Type == ConnectionType.Decline || 
                                     a.Type == ConnectionType.Normal && (gateway.Type == WorkflowGatewayType.Parallel || a.Condition != null))
                                     .Where(c =>
                                     {
                                         var app = c.Applicable(ctx);
                                         if (!app && gateway.Type == WorkflowGatewayType.Parallel)
                                             throw new InvalidOperationException($"Conditions not allowed in {WorkflowGatewayType.Parallel} {WorkflowGatewayDirection.Split}!");
                                         return app;
                                     }).ToList();

                                if (applicable.IsEmpty())
                                {
                                    if (gateway.Type == WorkflowGatewayType.Parallel)
                                        throw new InvalidOperationException(WorkflowValidationMessage.ParallelSplit0ShouldHaveAtLeastOneConnection.NiceToString(gateway));
                                    else
                                    {
                                        var fallback = gateway.NextConnectionsFromCache(null).SingleOrDefaultEx(a => a.Condition == null && a.Type == ConnectionType.Normal);
                                        if (fallback == null)
                                            throw new InvalidOperationException(WorkflowValidationMessage.InclusiveGateway0ShouldHaveOneConnectionWithoutCondition.NiceToString(gateway));

                                        return FindNext(fallback, ctx);
                                    }
                                }
                                else
                                {
                                    foreach (var con in applicable)
                                    {
                                        FindNext(con, ctx);
                                    }
                                    return true;
                                }
                            }
                            else //if (gateway.Direction == WorkflowGatewayDirection.Join)
                            {
                                if (!AllTrackCompleted(0, gateway, ctx))
                                    return false;

                                var singleConnection = gateway.NextConnectionsFromCache(ConnectionType.Normal).SingleEx();
                                return FindNext(singleConnection, ctx);
                            }
                        default:
                            throw new InvalidOperationException();
                    }
                }
            }

            static bool? IsCaseActivityCompleted(int depth, IWorkflowNodeEntity node, WorkflowExecuteStepContext ctx)
            {
                if (node.Is(ctx.CaseActivity.WorkflowActivity))
                    return true;

                // Parallel gateways always have CaseActivity but for Inclusive gateways maybe not be created because of conditions
                var last = ctx.Case.CaseActivities().Where(a => a.WorkflowActivity.Is(node)).OrderBy(a => a.StartDate).LastOrDefault();
                if (last != null)
                    return (last.DoneDate.HasValue);
                else
                    return null;
            }

            private static bool AllTrackCompleted(int depth, IWorkflowNodeEntity node, WorkflowExecuteStepContext ctx)
            {
                if (node is WorkflowActivityEntity || node is WorkflowEventEntity we && we.Type == WorkflowEventType.IntermediateTimer)
                {
                    var completed = IsCaseActivityCompleted(depth, node, ctx);

                    if (completed.HasValue)
                        return completed.Value;

                    var previous = node.PreviousConnectionsFromCache().Select(a => a.From).ToList();
                    return previous.Any(wn => AllTrackCompleted(depth, wn, ctx));
                }
                if (node is WorkflowEventEntity e && (e.Type == WorkflowEventType.BoundaryForkTimer || e.Type == WorkflowEventType.BoundaryInterruptingTimer))
                {
                    var parentActivity = WorkflowLogic.GetWorkflowNodeGraph(e.Lane.Pool.Workflow.ToLite()).Activities.GetOrThrow(e.BoundaryOf);

                    var completed = IsCaseActivityCompleted(depth, parentActivity, ctx);

                    if (completed.HasValue)
                        return completed.Value;
                    
                    var previous = parentActivity.PreviousConnectionsFromCache().Select(a => a.From).ToList();
                    if (parentActivity.BoundaryTimers.Any(a => a.Type == WorkflowEventType.BoundaryForkTimer))
                    {
                        if (depth <= 1)
                            return true;

                        return previous.Any(wn => AllTrackCompleted(depth - 1, wn, ctx));
                    }
                    else
                        return previous.Any(wn => AllTrackCompleted(depth, wn, ctx));

                }
                else if (node is WorkflowGatewayEntity g)
                {
                    if (g.Direction == WorkflowGatewayDirection.Split)
                    {
                        var from = g.PreviousConnectionsFromCache().Select(a => a.From).SingleEx();

                        switch (g.Type)
                        {
                            case WorkflowGatewayType.Exclusive:
                                return AllTrackCompleted(depth, from, ctx);
                            case WorkflowGatewayType.Inclusive:
                            case WorkflowGatewayType.Parallel:
                                if (depth <= 1)
                                    return true;

                                return AllTrackCompleted(depth - 1, from, ctx);
                            default:
                                throw new UnexpectedValueException(g.Type);
                        }
                    }
                    else if (g.Direction == WorkflowGatewayDirection.Join)
                    {
                        var previous = g.PreviousConnectionsFromCache().Select(a => a.From).ToList();

                        switch (g.Type)
                        {
                            case WorkflowGatewayType.Exclusive:
                                return previous.Any(wn => AllTrackCompleted(depth, wn, ctx));
                            case WorkflowGatewayType.Inclusive:
                            case WorkflowGatewayType.Parallel:
                                return previous.All(wn => AllTrackCompleted(depth + 1, wn, ctx));
                            default:
                                throw new UnexpectedValueException(g.Type);

                        }


                    }
                    else throw new UnexpectedValueException(g.Direction);
                }
                throw new UnexpectedValueException(node);
            }
        }
    }
}
