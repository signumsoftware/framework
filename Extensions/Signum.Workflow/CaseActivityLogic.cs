using DocumentFormat.OpenXml.Spreadsheet;
using Signum.Alerts;
using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Mailing;
using Signum.Mailing.Package;
using Signum.Processes;
using Signum.Scheduler;
using Signum.SMS;
using Signum.Utilities.Reflection;

namespace Signum.Workflow;

public static class CaseActivityLogic
{
    [AutoExpressionField]
    public static IQueryable<CaseActivityEntity> CaseActivities(this ICaseMainEntity e) =>
        As.Expression(() => Database.Query<CaseActivityEntity>().Where(a => a.Case.MainEntity == e));

    [AutoExpressionField]
    public static CaseActivityEntity? LastCaseActivity(this ICaseMainEntity e) =>
        As.Expression(() => e.CaseActivities().OrderByDescending(a => a.StartDate).FirstOrDefault());

    [AutoExpressionField]
    public static IQueryable<CaseEntity> Cases(this ICaseMainEntity e) =>
        As.Expression(() => e.CaseActivities().Select(a => a.Case));

    [AutoExpressionField]
    public static bool CurrentUserHasNotification(this ICaseMainEntity e) =>
        As.Expression(() => e.CaseActivities().SelectMany(a => a.Notifications()).Any(cn => IsForMe.Evaluate(cn)));

    [AutoExpressionField]
    public static IQueryable<CaseEntity> Cases(this WorkflowEntity w) =>
        As.Expression(() => Database.Query<CaseEntity>().Where(a => a.Workflow.Is(w)));

    [AutoExpressionField]
    public static bool CurrentUserHasNotification(this CaseActivityEntity ca) =>
        As.Expression(() => ca.Notifications().Any(cn => IsForMe.Evaluate(cn) &&
                                             (cn.State == CaseNotificationState.New ||
                                              cn.State == CaseNotificationState.Opened ||
                                              cn.State == CaseNotificationState.InProgress)));

    [AutoExpressionField]
    public static IQueryable<CaseActivityEntity> NextActivities(this CaseActivityEntity ca) =>
        As.Expression(() => Database.Query<CaseActivityEntity>().Where(a => a.Previous.Is(ca)));

    [AutoExpressionField]
    public static CaseActivityEntity DecompositionSurrogateActivity(this CaseEntity childCase) =>
        As.Expression(() => childCase.CaseActivities().OrderBy(ca => ca.StartDate).Select(a => a.Previous!.Entity).First());

    [AutoExpressionField]
    public static IQueryable<CaseEntity> SubCases(this CaseEntity p) =>
        As.Expression(() => Database.Query<CaseEntity>().Where(c => c.ParentCase.Is(p)));

    [AutoExpressionField]
    public static bool IsFreshNew(this CaseActivityEntity ca) =>
        As.Expression(() => ca.State == CaseActivityState.Pending && ca.Notifications().All(n => n.State == CaseNotificationState.New));

    [AutoExpressionField]
    public static IQueryable<CaseActivityEntity> CaseActivities(this IWorkflowNodeEntity e) =>
        As.Expression(() => Database.Query<CaseActivityEntity>().Where(a => a.WorkflowActivity == e));

    [AutoExpressionField]
    public static double? AverageDuration(this WorkflowActivityEntity wa) =>
        As.Expression(() => wa.CaseActivities().Average(a => a.Duration));

    [AutoExpressionField]
    public static double? AverageDuration(this WorkflowEventEntity we) =>
        As.Expression(() => we.CaseActivities().Average(a => a.Duration));

    [AutoExpressionField]
    public static IQueryable<CaseActivityEntity> CaseActivities(this CaseEntity e) =>
        As.Expression(() => Database.Query<CaseActivityEntity>().Where(a => a.Case.Is(e)));


    [AutoExpressionField]
    public static IQueryable<CaseTagEntity> Tags(this CaseEntity e) =>
        As.Expression(() => Database.Query<CaseTagEntity>().Where(a => a.Case.Is(e)));

    [AutoExpressionField]
    public static IQueryable<CaseNotificationEntity> Notifications(this CaseActivityEntity e) =>
        As.Expression(() => Database.Query<CaseNotificationEntity>().Where(a => a.CaseActivity.Is(e)));

    [AutoExpressionField]
    public static IQueryable<CaseActivityExecutedTimerEntity> ExecutedTimers(this CaseActivityEntity e) =>
        As.Expression(() => Database.Query<CaseActivityExecutedTimerEntity>().Where(a => a.CaseActivity.Is(e)));

    [AutoExpressionField]
    public static CaseActivityExecutedTimerEntity? LastExecutedTimer(this CaseActivityEntity ca, Lite<WorkflowEventEntity> we) =>
        As.Expression(() => ca.ExecutedTimers().Where(a => a.BoundaryEvent.Is(we)).OrderByDescending(a => a.CreationDate).FirstOrDefault());

    public static Expression<Func<CaseNotificationEntity, bool>> IsForMe = n => n.User.Is(UserEntity.Current); // For Deputy scenarios
    public static void Start(SchemaBuilder sb)
    {
        if (sb.AlreadyDefined(MethodInfo.GetCurrentMethod()))
            return;

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
                    CreatedBy = UserHolder.Current.User,
                }).SaveList();
            },
        }.Register();


        new Graph<CaseEntity>.Execute(CaseOperation.Cancel)
        {
            CanExecute = c => c.FinishDate == null ? null : CaseActivityMessage.AlreadyFinished.NiceToString(),
            Execute = (c, args) =>
            {
                var avoidRecompose = args.TryGetArgS<bool>() ?? false;

                foreach (var sc in c.SubCases().Where(a => a.FinishDate == null))
                {
                    sc.Execute(CaseOperation.Cancel, true);
                }

                var currentActivities = c.CaseActivities().Where(a => a.DoneBy == null).ToList();

                foreach (var ca in currentActivities)
                {
                    ca.DoneBy = UserEntity.Current;
                    ca.DoneDate = Clock.Now;
                    ca.DoneType = DoneType.Jump;
                    ca.DoneDecision = CaseActivityMessage.CanceledCase.ToString();
                    ca.Save();

                    foreach (var notification in ca.Notifications().ToList())
                    {
                        notification.State = CaseNotificationState.DoneByOther;
                        notification.Save();
                    }
                }

                c.FinishDate = Clock.Now;
                CancelEntity(c.MainEntity, c);
                c.Save();

                if (c.ParentCase != null && !avoidRecompose)
                    CaseActivityGraph.TryToRecompose(c);
            }
        }.Register();

        new Graph<CaseEntity>.Delete(CaseOperation.Delete)
        {
            CanDelete = e => e.ParentCase == null ? null : CaseActivityMessage.CaseIsADecompositionOf0.NiceToString(e.ParentCase),
            Delete = (c, args) =>
            {
                DeleteCase(c);

                if (args.GetArg<bool>())
                    c.MainEntity.Delete();
            }

        }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();

        void DeleteCase(CaseEntity c)
        {
            foreach (var sc in c.SubCases())
            {
                DeleteCase(sc);
            }

            c.CaseActivities().SelectMany(ca => ca.Notifications()).UnsafeDelete();
            c.CaseActivities().UnsafeDelete();
            c.Delete();
        }

        IQueryable<CaseActivityEntity> RejectedCases(CaseEntity c)
        {
            return c.CaseActivities().Where(a => a.DoneBy != null && (a.DoneType == DoneType.Next || a.DoneType == DoneType.Recompose)).OrderByDescending(a => a.DoneDate).Take(1);
        }

        new Graph<CaseEntity>.Execute(CaseOperation.Reactivate)
        {
            CanExecute = c => c.FinishDate != null && CancelledCases(c).Any() ? null : CaseActivityMessage.NotCanceled.NiceToString(),
            Execute = (c, _) =>
            {
                ReactivateEntity(c.MainEntity, c);

                var currentActivities = CancelledCases(c).ToList();

                currentActivities = currentActivities.Concat(RejectedCases(c).ToList()).ToList();

                foreach (var ca in currentActivities)
                {
                    ca.DoneBy = null;
                    ca.DoneDate = null;
                    ca.DoneType = null;
                    ca.DoneDecision = null;
                    var connections = ca.WorkflowActivity.NextConnections().ToList();

                    ca.Save();

                    foreach (var notification in ca.Notifications().ToList())
                    {
                        notification.State = CaseNotificationState.New;
                        notification.Save();
                    }
                }

                c.FinishDate = null;
                c.Save();

                foreach (var sc in c.SubCases().Where(a => a.FinishDate != null))
                {
                    sc.Execute(CaseOperation.Reactivate, true);
                }
            }
        }.Register();


        sb.Include<CaseActivityEntity>()
            .WithIndex(a => new { a.ScriptExecution!.ProcessIdentifier }, a => a.DoneDate == null)
            .WithIndex(a => new { a.ScriptExecution!.NextExecution }, a => a.DoneDate == null)
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

        sb.Schema.EntityEvents<CaseActivityEntity>().Saved += (e, args) =>
        {
            if (args.WasNew && e.WorkflowActivity is WorkflowActivityEntity wa && wa.Type == WorkflowActivityType.Script)
                WorkflowScriptRunner.WakeupOnCommit();
        };

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

        QueryLogic.Expressions.Register((WorkflowActivityEntity a) => a.AverageDuration(), WorkflowActivityMessage.AverageDuration);

        SimpleTaskLogic.Register(CaseActivityTask.Timeout, (ScheduledTaskContext ctx) =>
        {
            var boundaryCandidates =
            (from ca in Database.Query<CaseActivityEntity>()
             where !ca.Workflow().HasExpired()
             where ca.State == CaseActivityState.Pending
             from we in ((WorkflowActivityEntity)ca.WorkflowActivity).BoundaryTimers
             where we.Type == WorkflowEventType.BoundaryInterruptingTimer ? true :
             we.Type == WorkflowEventType.BoundaryForkTimer ? (we.RunRepeatedly || !ca.ExecutedTimers().Any(t => t.BoundaryEvent.Is(we))) :
             false
             select new ActivityEvent(ca, we)).ToList();

            var intermediateCandidates =
            (from ca in Database.Query<CaseActivityEntity>()
             where !ca.Workflow().HasExpired()
             where ca.State == CaseActivityState.Pending
             let we = ((WorkflowEventEntity)ca.WorkflowActivity)
             where we.Type == WorkflowEventType.IntermediateTimer
             select new ActivityEvent(ca, we)).ToList();

            var candidates = boundaryCandidates.Concat(intermediateCandidates).Distinct().ToList();
            var conditions = candidates.Select(a => a.Event.Timer!.Condition).Distinct().ToList();
            var lastExecutions = boundaryCandidates
                .Where(a => a.Event.Type == WorkflowEventType.BoundaryForkTimer && a.Event.RunRepeatedly)
                .Select(a => new { ActivityEvent = a, LastExecution = a.Activity.LastExecutedTimer(a.Event.ToLite()) })
                .ToList();

            var now = Clock.Now;
            var activities = conditions.SelectMany(cond =>
            {
                if (cond == null)
                    return candidates.Where(a =>
                    {
                        var startDate = lastExecutions.SingleOrDefaultEx(le => le.ActivityEvent.Is(a))?.LastExecution?.CreationDate ?? a.Activity.StartDate;
                        return a.Event.Timer!.Duration != null && a.Event.Timer!.Duration!.Add(startDate) < now;
                    }).Select(a => a.Activity.ToLite()).ToList();

                return candidates.Where(a => !a.Event.Timer!.AvoidExecuteConditionByTimer && a.Event.Timer!.Condition.Is(cond) && cond.Evaluate(a.Activity, now)).Select(a => a.Activity.ToLite()).ToList();
            }).Distinct().ToList();

            if (!activities.Any())
                return null;

            var pkg = new PackageEntity().CreateLines(activities);

            return ProcessLogic.Create(CaseActivityProcessAlgorithm.Timeout, pkg).Execute(ProcessOperation.Execute).ToLite();
        });
        ProcessLogic.Register(CaseActivityProcessAlgorithm.Timeout, new PackageExecuteAlgorithm<CaseActivityEntity>(CaseActivityOperation.Timer));

        QueryLogic.Expressions.Register((CaseEntity c) => c.DecompositionSurrogateActivity());
        QueryLogic.Expressions.Register((CaseActivityEntity ca) => ca.CurrentUserHasNotification(), CaseActivityMessage.CurrentUserHasNotification);
        QueryLogic.Expressions.Register((ICaseMainEntity a) => a.CaseActivities(), () => typeof(CaseActivityEntity).NicePluralName());
        QueryLogic.Expressions.Register((ICaseMainEntity a) => a.Cases(), () => typeof(CaseEntity).NicePluralName());
        QueryLogic.Expressions.Register((ICaseMainEntity a) => a.LastCaseActivity(), CaseActivityMessage.LastCaseActivity);
        QueryLogic.Expressions.Register((ICaseMainEntity a) => a.CurrentUserHasNotification(), CaseActivityMessage.CurrentUserHasNotification);

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

        new Graph<CaseNotificationEntity>.Delete(CaseNotificationOperation.Delete)
        {
            Delete = (e, args) => e.Delete(),
        }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();

        new Graph<CaseNotificationEntity>.ConstructFrom<CaseActivityEntity>(CaseNotificationOperation.CreateCaseNotificationFromCaseActivity)
        {
            Construct = (e, args) => new CaseNotificationEntity
            {
                CaseActivity = e.ToLite(),
                Actor = args.GetArg<Lite<UserEntity>>(),
                User = args.GetArg<Lite<UserEntity>>(),
                State = CaseNotificationState.New,
            }.Save(),
        }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();


        QueryLogic.Queries.Register(CaseActivityQuery.Inbox, () => DynamicQueryCore.Auto(
                from cn in Database.Query<CaseNotificationEntity>()
                where IsForMe.Evaluate(cn)
                let ca = cn.CaseActivity.Entity
                let previous = ca.Previous!.Entity
                select new
                {
                    Entity = cn.CaseActivity,
                    ca.StartDate,
                    Workflow = ca.Case.Workflow.ToLite(),
                    Activity = new ActivityWithRemarks
                    {
                        WorkflowActivity = ((WorkflowActivityEntity)ca.WorkflowActivity).ToLite(),
                        Case = ca.Case.ToLite(),
                        CaseActivity = ca.ToLite(),
                        Notification = cn.ToLite(),
                        Remarks = cn.Remarks,
                        Alerts = ca.MyActiveAlerts().Count(),
                        Tags = ca.Case.Tags().Select(a => a.TagType).ToList(),
                    },
                    MainEntity = ca.Case.MainEntity.ToLite(ca.Case.ToString()),
                    Sender = previous.DoneBy,
                    SenderNote = previous.Note,
                    cn.State,
                    cn.Actor,
                    cn.User,
                })
                .ColumnDisplayName(a => a.Activity, () => InboxMessage.Activity.NiceToString())
                .ColumnDisplayName(a => a.Sender, () => InboxMessage.Sender.NiceToString())
                .ColumnDisplayName(a => a.SenderNote, () => InboxMessage.SenderNote.NiceToString())
                );


        CaseActivityGraph.Register();
        OverrideCaseActivityMixin(sb);

    }


    public static IQueryable<CaseActivityEntity> CancelledCases(CaseEntity c)
    {
        return c.CaseActivities().Where(a => a.DoneBy != null && a.DoneType == DoneType.Jump &&
                (a.DoneDecision == CaseActivityMessage.CanceledCase.ToString() || a.DoneDecision == CaseActivityMessage.CanceledCase.NiceToString())
         );
    }

    class ActivityEvent
    {
        public ActivityEvent(CaseActivityEntity activity, WorkflowEventEntity @event)
        {
            Activity = activity;
            Event = @event;
        }

        public bool Is(ActivityEvent other)
        {
            return this.Activity.Is(other.Activity) && this.Event.Is(other.Event);
        }

        public CaseActivityEntity Activity { get; set; }
        public WorkflowEventEntity Event { get; set; }
    }

    public static CaseActivityEntity CreateCaseActivity(this WorkflowEntity workflow, ICaseMainEntity mainEntity)
    {
        var caseActivity = workflow.ConstructFrom(CaseActivityOperation.CreateCaseActivityFromWorkflow, mainEntity);
        return caseActivity.Execute(CaseActivityOperation.Register);
    }


    public static Dictionary<Type, WorkflowOptions> Options = new Dictionary<Type, WorkflowOptions>();

    public class WorkflowOptions
    {
        public Func<ICaseMainEntity>? Constructor;
        public Action<ICaseMainEntity> SaveEntity;
        public Action<ICaseMainEntity, CaseEntity>? Cancel;
        public Action<ICaseMainEntity, CaseEntity>? Reactivate;

        public WorkflowOptions(Func<ICaseMainEntity>? constructor, Action<ICaseMainEntity> saveEntity, Action<ICaseMainEntity, CaseEntity>? cancel, Action<ICaseMainEntity, CaseEntity>? reactivate)
        {
            Constructor = constructor;
            SaveEntity = saveEntity;
            Cancel = cancel;
            Reactivate = reactivate;
        }
    }

    public static FluentInclude<T> WithWorkflow<T>(this FluentInclude<T> fi, Func<T>? constructor, Action<T> save, Action<T, CaseEntity>? cancel = null, Action<T, CaseEntity>? reactivate = null)
        where T : Entity, ICaseMainEntity
    {
        fi.SchemaBuilder.Schema.EntityEvents<T>().Saved += (e, args) =>
        {
            if (AvoidNotifyInProgressVariable.Value == true)
                return;

            e.NotifyInProgress();
        };

        Options[typeof(T)] = new WorkflowOptions(constructor,
            saveEntity: e => save((T)e),
            cancel: cancel == null ? null : ((e, c) => cancel((T)e, c)),
            reactivate: reactivate == null ? null : ((e, c) => reactivate((T)e, c)));

        return fi;
    }

    static IDisposable AvoidNotifyInProgress()
    {
        var old = AvoidNotifyInProgressVariable.Value;
        AvoidNotifyInProgressVariable.Value = true;
        return new Disposable(() => AvoidNotifyInProgressVariable.Value = old);
    }

    static AsyncThreadVariable<bool> AvoidNotifyInProgressVariable = Statics.ThreadVariable<bool>("avoidNotifyInProgress");

    public static int NotifyInProgress(this ICaseMainEntity mainEntity)
    {
        using (AuthLogic.Disable())
            return Database.Query<CaseNotificationEntity>()
                .Where(n => n.CaseActivity.Entity.Case.MainEntity == mainEntity && n.CaseActivity.Entity.DoneDate == null)
                .Where(n => IsForMe.Evaluate(n) && (n.State == CaseNotificationState.New || n.State == CaseNotificationState.Opened))
                .UnsafeUpdate()
                .Set(n => n.State, n => CaseNotificationState.InProgress)
                .Execute();
    }

    public class WorkflowExecuteStepContext
    {
        public CaseEntity Case;
        public CaseActivityEntity? PreviousCaseActivity;
        public List<WorkflowActivityEntity> ToActivities = new List<WorkflowActivityEntity>();
        public List<WorkflowEventEntity> ToIntermediateEvents = new List<WorkflowEventEntity>();
        public bool IsFinished { get; set; }
        public List<WorkflowConnectionEntity> Connections = new List<WorkflowConnectionEntity>();

        public List<WorkflowTransitionContext> TransitionContextToNotify = new List<WorkflowTransitionContext>();

        public WorkflowExecuteStepContext(CaseEntity @case, CaseActivityEntity? previous)
        {
            Case = @case;
            PreviousCaseActivity = previous;
        }

        public void ExecuteConnection(WorkflowConnectionEntity connection)
        {
            var wctx = this.NewTransitionContext(connection);

            WorkflowLogic.OnTransition?.Invoke(Case.MainEntity, wctx);

            if (connection.Action != null)
            {
                connection.Action.Execute(Case.MainEntity, wctx);
            };

            this.Connections.Add(connection);
        }

        public WorkflowTransitionContext NewTransitionContext(WorkflowConnectionEntity connection)
        {
            var wtc = new WorkflowTransitionContext(Case, PreviousCaseActivity, connection);
            TransitionContextToNotify.Add(wtc);
            return wtc;
        }

        internal void NotifyTransitionContext(CaseActivityEntity newCaseActivity)
        {
            var graph = WorkflowLogic.GetWorkflowNodeGraph(Case.Workflow.ToLite());
            foreach (var tctx in TransitionContextToNotify.Where(a => a.OnNextCaseActivityCreated != null && a.PreviousCaseActivity != null && a.Connection != null))
            {
                var from = tctx.PreviousCaseActivity!.WorkflowActivity;
                var to = newCaseActivity.WorkflowActivity;

                if (!from.Lane.Pool.Workflow.Is(Case.Workflow)) //Is SubWorkflow
                    from = graph.Events.Values.SingleEx(a => a.Type == WorkflowEventType.Start);

                if (graph.GetAllConnections(from, to, path => true).Contains(tctx.Connection!) ||
                    from is WorkflowActivityEntity fromWA && fromWA.BoundaryTimers.Any(e => graph.GetAllConnections(e, to, path => true).Contains(tctx.Connection!)))
                    tctx.OnNextCaseActivityCreated!(newCaseActivity);
            }
        }
    }

    static bool Applicable(this WorkflowConnectionEntity wc, WorkflowExecuteStepContext ctx)
    {
        var doneDecission = wc.DoneDecision();

        if (doneDecission != null && doneDecission != ctx.PreviousCaseActivity?.DoneDecision)
            return false;

        if (wc.Condition != null)
        {
            var result = wc.Condition.Evaluate(ctx.Case.MainEntity, ctx.NewTransitionContext(wc));

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

    static void CancelEntity(ICaseMainEntity mainEntity, CaseEntity caseEntity)
    {
        var options = CaseActivityLogic.Options.GetOrThrow(mainEntity.GetType());
        using (AvoidNotifyInProgress())
            options.Cancel?.Invoke(mainEntity, caseEntity);
    }

    static void ReactivateEntity(ICaseMainEntity mainEntity, CaseEntity caseEntity)
    {
        var options = CaseActivityLogic.Options.GetOrThrow(mainEntity.GetType());
        using (AvoidNotifyInProgress())
            options.Reactivate?.Invoke(mainEntity, caseEntity);
    }

    public static CaseActivityEntity RetrieveForViewing(Lite<CaseActivityEntity> lite)
    {
        var ca = lite.RetrieveAndRemember();

        if (ca.DoneBy == null)
            ca.Notifications()
              .Where(n => IsForMe.Evaluate(n) && n.State == CaseNotificationState.New)
              .UnsafeUpdate()
              .Set(a => a.State, a => CaseNotificationState.Opened)
              .Execute();

        return ca;
    }

    public static List<Lite<Entity>> GetActors(this WorkflowLaneEntity lane, CaseActivityEntity? caseActivity)
    {
        if (caseActivity != null)
        {
            if (lane.ActorsEval == null)
                return lane.Actors.ToList();

            var newActors = lane.ActorsEval.Algorithm.GetActors(caseActivity.Case.MainEntity, new WorkflowTransitionContext(caseActivity.Case, caseActivity, null)).EmptyIfNull().ToList();

            if (lane.CombineActorAndActorEvalWhenContinuing)
                newActors.AddRange(lane.Actors);
            

            return newActors.Distinct().ToList();
        }
        else
        {
            if (lane.UseActorEvalForStart)
            {
                return lane.ActorsEval!.Algorithm.GetActors(null!, new WorkflowTransitionContext(null, null, null)).Distinct().ToList();
            }

            return lane.Actors.ToList();
        }
    }


    public static List<UserEntity> GetActorUsers(this WorkflowLaneEntity lane, CaseActivityEntity? caseActivity)
    {
        var actors = lane.GetActors(caseActivity);

        var users = actors.OfType<Lite<UserEntity>>().Chunk(100)
            .SelectMany(gr => Database.Query<UserEntity>().Where(u => gr.Contains(u.ToLite())))
            .ToList();

        var nonUsers = actors.Where(a => a is not Lite<UserEntity>)
            .SelectMany(a => Database.Query<UserEntity>().Where(u => WorkflowLogic.IsUserActorForNotifications.Evaluate(u, a)).ToList())
            .ToList();

        return users.Concat(nonUsers).Distinct().ToList();
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

                var actors = lane.GetActors(caseActivity);

                var notifications = (from a in actors
                                     from u in (a is Lite<UserEntity> u ? [u] : Database.Query<UserEntity>().Where(u => WorkflowLogic.IsUserActorForNotifications.Evaluate(u, a)).Select(u => u.ToLite()).ToList())
                                     select (a, u))
                                     .DistinctBy(p => p.u)
                                     .Select(p => new CaseNotificationEntity
                                     {
                                         CaseActivity = caseActivity.ToLite(),
                                         Actor = p.a,
                                         State = CaseNotificationState.New,
                                         User = p.u
                                     }).ToList();

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
                ToStates = { CaseActivityState.New },
                Construct = (w, args) =>
                {
                    if (w.HasExpired())
                        throw new InvalidOperationException(WorkflowMessage.Workflow0HasExpiredOn1.NiceToString(w, w.ExpirationDate!.Value.ToString()));

                    var parentCase = args.TryGetArgC<Lite<CaseEntity>>();

                    var wfGraph = WorkflowLogic.GetWorkflowNodeGraph(w.ToLite());
                    if (parentCase == null && !wfGraph.IsStartCurrentUser())
                        throw new InvalidOperationException(WorkflowMessage.YouAreNotMemberOfAnyLaneContainingAnStartEventInWorkflow0.NiceToString(wfGraph.Workflow));

                    var mainEntity = args.TryGetArgC<ICaseMainEntity>() ?? CaseActivityLogic.CreateMainEntity(w.MainEntityType.ToType());

                    var @case = new CaseEntity
                    {
                        ParentCase = parentCase,
                        Workflow = w,
                        Description = w.Name,
                        MainEntity = mainEntity,
                    };

                    var start = wfGraph.Events.Values.Single(a => a.Type == WorkflowEventType.Start);
                    var connection = start.NextConnectionsFromCache(ConnectionType.Normal).SingleEx();
                    var next = (WorkflowActivityEntity)connection.To;
                    var ca = new CaseActivityEntity
                    {
                        WorkflowActivity = next,
                        OriginalWorkflowActivityName = next.Name,
                        Case = @case,
                       ScriptExecution=  GetScriptExecution(next)
                    };

                    //new WorkflowExecuteStepContext(@case, ca).ExecuteConnection(connection);

                    return ca;
                }
            }.Register();

            new Graph<CaseEntity>.ConstructFrom<WorkflowEventTaskEntity>(CaseActivityOperation.CreateCaseFromWorkflowEventTask)
            {
                Construct = (wet, args) =>
                {
                    var workflow = wet.GetWorkflow();

                    if (workflow.HasExpired())
                        throw new InvalidOperationException(WorkflowMessage.Workflow0HasExpiredOn1.NiceToString(workflow, workflow.ExpirationDate!.Value.ToString()));

                    var mainEntity = args.GetArg<ICaseMainEntity>();
                    var @case = new CaseEntity
                    {
                        Workflow = workflow,
                        Description = workflow.Name,
                        MainEntity = mainEntity,
                    };

                    var start = wet.Event.RetrieveAndRemember();
                    ExecuteInitialStep(@case, start, start.NextConnectionsFromCache(ConnectionType.Normal).SingleEx());

                    return @case;
                }
            }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();

            new Execute(CaseActivityOperation.Register)
            {
                CanExecute = ca => !(ca.WorkflowActivity is WorkflowActivityEntity) ? CaseActivityMessage.NoWorkflowActivity.NiceToString() : null,
                FromStates = { CaseActivityState.New },
                ToStates = { CaseActivityState.Pending },
                CanBeNew = true,
                CanBeModified = true,
                Execute = (ca, _) =>
                {
                    var wfGraph = WorkflowLogic.GetWorkflowNodeGraph(ca.WorkflowActivity.Lane.Pool.Workflow.ToLite());
                    if (ca.Case.ParentCase == null && !wfGraph.IsStartCurrentUser())
                        throw new InvalidOperationException(WorkflowMessage.YouAreNotMemberOfAnyLaneContainingAnStartEventInWorkflow0.NiceToString(wfGraph.Workflow));

                    SaveEntity(ca.Case.MainEntity);
                    var now = Clock.Now;
                    var c = ca.Case;
                    c.StartDate = now;
                    c.Description = ca.Case.MainEntity.ToString()!.Trim().Etc(100);
                    c.Save();



                    var prevConn = ca.WorkflowActivity.PreviousConnectionsFromCache().SingleEx(a => a.From is WorkflowEventEntity ev && ev.Type == WorkflowEventType.Start);

                    var wec = new WorkflowExecuteStepContext(ca.Case, ca.Previous?.Retrieve());

                    wec.ExecuteConnection(prevConn);

                    ca.StartDate = now;
                    ca.Save();

                    InsertCaseActivityNotifications(ca);

                    wec.NotifyTransitionContext(ca);
                }
            }.Register();


            new Delete(CaseActivityOperation.Delete)
            {
                FromStates = { CaseActivityState.Pending },
                CanDelete = ca => ca.Case.ParentCase != null ? CaseActivityMessage.CaseIsADecompositionOf0.NiceToString(ca.Case.ParentCase) :
                ca.Case.CaseActivities().Any(a => !a.Is(ca)) ? CaseActivityMessage.CaseContainsOtherActivities.NiceToString() :
                !ca.CurrentUserHasNotification() ? CaseActivityMessage.NoNewOrOpenedOrInProgressNotificationsFound.NiceToString() : null,
                Delete = (ca, args) =>
                {
                    var c = ca.Case;
                    ca.Notifications().UnsafeDelete();
                    ca.Delete();
                    c.Delete();
                    if (args.GetArg<bool>())
                        c.MainEntity.Delete();
                },
            }.Register();


            new Execute(CaseActivityOperation.Next)
            {
                CanExecute = ca => !(ca.WorkflowActivity is WorkflowActivityEntity) ? CaseActivityMessage.NoWorkflowActivity.NiceToString() :
                !ca.CurrentUserHasNotification() ? CaseActivityMessage.NoNewOrOpenedOrInProgressNotificationsFound.NiceToString() :
                null,
                FromStates = { CaseActivityState.Pending },
                ToStates = { CaseActivityState.Done },
                CanBeModified = true,
                Execute = (ca, args) =>
                {
                    CheckRequiresOpen(ca);
                    ExecuteStep(ca, DoneType.Next, args.TryGetArgC<string>(), null);
                },
            }.Register();



            new Execute(CaseActivityOperation.Jump)
            {
                CanExecute = ca => !(ca.WorkflowActivity is WorkflowActivityEntity) ? CaseActivityMessage.NoWorkflowActivity.NiceToString() :
                ca.WorkflowActivity.NextConnectionsFromCache(ConnectionType.Jump).IsEmpty() ? CaseActivityMessage.Activity0HasNoJumps.NiceToString(ca.WorkflowActivity) :
                !ca.CurrentUserHasNotification() ? CaseActivityMessage.NoNewOrOpenedOrInProgressNotificationsFound.NiceToString() : null,
                FromStates = { CaseActivityState.Pending },
                ToStates = { CaseActivityState.Done },
                CanBeModified = true,
                Execute = (ca, args) =>
                {
                    CheckRequiresOpen(ca);
                    var to = args.GetArg<Lite<IWorkflowNodeEntity>>();
                    var jump = ca.WorkflowActivity.NextConnectionsFromCache(ConnectionType.Jump).SingleEx(c => to.Is(c.To));
                    ExecuteStep(ca, DoneType.Jump, null, jump);
                },
            }.Register();


            new Execute(CaseActivityOperation.FreeJump)
            {
                FromStates = { CaseActivityState.Pending },
                ToStates = { CaseActivityState.Done },
                CanBeModified = true,
                Execute = (ca, args) =>
                {
                    var to = args.GetArg<Lite<WorkflowActivityEntity>>().Retrieve();
                    if (!to.Lane.Pool.Workflow.Is(ca.Case.Workflow))
                        throw new InvalidOperationException($"Activity {to} does not belong to workflow {ca.Case.Workflow}");

                    using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = ca, Connection = null }))
                    {
                        SaveEntity(ca.Case.MainEntity);
                    }

                    ca.MakeDone(DoneType.Jump, null);

                    var ctx = new WorkflowExecuteStepContext(ca.Case, ca);
                    ctx.ToActivities.Add(to);
                    CreateNextActivities(ca.Case, ctx, ca);
                }
            }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();

            new Execute(CaseActivityOperation.Timer)
            {
                FromStates = { CaseActivityState.Pending },
                ToStates = { CaseActivityState.Done, CaseActivityState.Pending },
                CanExecute = ca => (ca.WorkflowActivity is WorkflowEventEntity we && we.Type.IsTimer() ||
                ca.WorkflowActivity is WorkflowActivityEntity wa && wa.BoundaryTimers.Any()) ? null : CaseActivityMessage.Activity0HasNoTimers.NiceToString(ca.WorkflowActivity),
                Execute = (ca, args) =>
                {
                    var now = Clock.Now;

                    var candidateEvents = ca.WorkflowActivity is WorkflowEventEntity @event ? new WorkflowEventEntity[] { @event } :
                        ((WorkflowActivityEntity)ca.WorkflowActivity).BoundaryTimers.ToArray();

                    var evaluateSpecificEvents = args.TryGetArgC<List<Lite<WorkflowEventEntity>>>();
                    if (evaluateSpecificEvents != null)
                        candidateEvents = candidateEvents.Where(ce => evaluateSpecificEvents.Any(e => e.Is(ce))).ToArray();

                    var lastExecutions = (
                        from et in ca.ExecutedTimers()
                        group et by et.BoundaryEvent into g
                        select new { Event = g.Key, LastExecution = g.OrderByDescending(a => a.CreationDate).FirstOrDefault() }
                        ).ToDictionary(a => a.Event, a => a.LastExecution);

                    var timer = candidateEvents.Where(e => e.Type == WorkflowEventType.BoundaryInterruptingTimer || e.RunRepeatedly || !lastExecutions.ContainsKey(e.ToLite())).FirstOrDefault(t =>
                    {
                        if (t.Timer!.Duration != null)
                        {
                            var startDate = lastExecutions.GetValueOrDefault(t.ToLite())?.CreationDate ?? ca.StartDate;
                            return t.Timer!.Duration!.Add(startDate) < now;
                        }

                        return t.Timer!.Condition!.Evaluate(ca, now);
                    });

                    if (timer == null)
                    {
                        if (evaluateSpecificEvents == null)
                            throw new InvalidOperationException(WorkflowActivityMessage.NoActiveTimerFound.NiceToString());
                        else
                            return; //If evaluating a specific timer manually it is not an error not to find a suitable timer to execute
                    }

                    switch (timer.Type)
                    {
                        case WorkflowEventType.BoundaryForkTimer:
                        case WorkflowEventType.BoundaryInterruptingTimer:
                            ExecuteBoundaryTimer(ca, timer);
                            break;
                        case WorkflowEventType.IntermediateTimer:
                            ExecuteStep(ca, DoneType.Timeout, null, timer.NextConnectionsFromCache(ConnectionType.Normal).SingleEx());
                            break;
                        default:
                            throw new InvalidOperationException("Unexpected Timer Type " + timer.Type);
                    }
                },
            }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();

            new Execute(CaseActivityOperation.MarkAsUnread)
            {
                FromStates = { CaseActivityState.Pending },
                ToStates = { CaseActivityState.Pending },
                CanExecute = c => c.Notifications().Any(cn => IsForMe.Evaluate(cn) && (cn.State == CaseNotificationState.InProgress || cn.State == CaseNotificationState.Opened)) ? null :
                    CaseActivityMessage.NoOpenedOrInProgressNotificationsFound.NiceToString(),
                Execute = (ca, args) =>
                {
                    ca.Notifications()
                    .Where(cn => IsForMe.Evaluate(cn) && (cn.State == CaseNotificationState.InProgress || cn.State == CaseNotificationState.Opened))
                    .UnsafeUpdate()
                    .Set(cn => cn.State, cn => CaseNotificationState.New)
                    .Execute();
                },
            }.Register();

            new Execute(CaseActivityOperation.Undo)
            {
                FromStates = { CaseActivityState.Done },
                ToStates = { CaseActivityState.Pending },
                CanExecute = ca =>
                {
                    if (!ca.DoneBy.Is(UserEntity.Current))
                        return CaseActivityMessage.Only0CanUndoThisOperation.NiceToString(ca.DoneBy);

                    if (!ca.NextActivities().All(na => na.IsFreshNew()))
                        return CaseActivityMessage.NextActivityAlreadyInProgress.NiceToString();

                    if (ca.Case.ParentCase != null && !ca.Case.InDB().SelectMany(c => c.DecompositionSurrogateActivity().NextActivities()).All(na => na.IsFreshNew()))
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
                    if (ca.Case.ParentCase != null && ca.Case.FinishDate.HasValue)
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
                FromStates = { CaseActivityState.Pending },
                ToStates = { CaseActivityState.Done },
                Execute = (ca, args) =>
                {
                    using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = ca }))
                    {
                        var script = ((WorkflowActivityEntity)ca.WorkflowActivity).Script!.Script!.RetrieveFromCache();

                        if (ca.ScriptExecution == null)
                            ca.ScriptExecution = GetScriptExecution(ca.WorkflowActivity);

                        script.Eval.Algorithm.ExecuteUntyped(ca.Case.MainEntity, new WorkflowScriptContext
                        {
                            CaseActivity = ca,
                            RetryCount = ca.ScriptExecution!.RetryCount,
                        });
                    }

                    ExecuteStep(ca, DoneType.ScriptSuccess, null, null);
                },
            }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();

            new Execute(CaseActivityOperation.ScriptScheduleRetry)
            {
                CanExecute = s => s.WorkflowActivity is WorkflowActivityEntity wa && wa.Type == WorkflowActivityType.Script ? null : CaseActivityMessage.OnlyForScriptWorkflowActivities.NiceToString(),
                FromStates = { CaseActivityState.Pending },
                ToStates = { CaseActivityState.Pending },
                Execute = (ca, args) =>
                {
                    var se = ca.ScriptExecution!;
                    se.RetryCount++;
                    se.NextExecution = args.GetArg<DateTime>();
                    se.ProcessIdentifier = null;
                    ca.Save();
                },
            }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();

            new Execute(CaseActivityOperation.ScriptFailureJump)
            {
                CanExecute = s => s.WorkflowActivity is WorkflowActivityEntity wa && wa.Type == WorkflowActivityType.Script ? null : CaseActivityMessage.OnlyForScriptWorkflowActivities.NiceToString(),
                FromStates = { CaseActivityState.Pending },
                ToStates = { CaseActivityState.Done },
                Execute = (ca, args) =>
                {
                    ExecuteStep(ca, DoneType.ScriptFailure, null, ca.WorkflowActivity.NextConnectionsFromCache(ConnectionType.ScriptException).SingleEx());
                },
            }.SetMaxAutomaticUpgrade(OperationAllowed.None).Register();
        }

        private static void CheckRequiresOpen(CaseActivityEntity ca)
        {
            if (((WorkflowActivityEntity)ca.WorkflowActivity).RequiresOpen)
            {
                if (!ca.Notifications().Any(cn => IsForMe.Evaluate(cn) && cn.State != CaseNotificationState.New))
                    throw new ApplicationException(CaseActivityMessage.TheActivity0RequiresToBeOpened.NiceToString(ca.WorkflowActivity));
            }
        }


        public static void ExecuteStep(CaseActivityEntity ca, DoneType doneType, string? decision, WorkflowConnectionEntity? firstConnection)
        {
            using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = ca, Connection = firstConnection, Decision = decision }))
            {
                SaveEntity(ca.Case.MainEntity);
            }

            ca.MakeDone(doneType, decision);

            var ctx = new WorkflowExecuteStepContext(ca.Case, ca);

            if (firstConnection != null)
            {
                if (firstConnection.Condition != null)
                {
                    var jumpCtx = ctx.NewTransitionContext(firstConnection);
                    var result = firstConnection.Condition.Evaluate(ca.Case.MainEntity, jumpCtx);
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

        private static void FinishStep(CaseEntity @case, WorkflowExecuteStepContext ctx, CaseActivityEntity? ca)
        {
            @case.Description = @case.MainEntity.ToString()!.Trim().Etc(100);

            if (ctx.IsFinished)
            {
                if (ctx.ToActivities.Any() || ctx.ToIntermediateEvents.Any())
                    throw new InvalidOperationException("ToActivities and ToIntermediateEvents should be empty when finishing");

                if (@case.CaseActivities().Any(a => a.State == CaseActivityState.Pending))
                    return;

                using (WorkflowActivityInfo.Scope(new WorkflowActivityInfo { CaseActivity = ca, Connection = null }))
                {
                    SaveEntity(ca!.Case.MainEntity);
                }

                @case.FinishDate = ca.DoneDate!.Value;
                @case.Save();

                if (@case.ParentCase != null)
                    TryToRecompose(@case);
            }
            else
            {
                CreateNextActivities(@case, ctx, ca);
            }
        }

        public static void CreateNextActivities(CaseEntity @case, WorkflowExecuteStepContext ctx, CaseActivityEntity? previous)
        {
            @case.Save();

            foreach (var twa in ctx.ToActivities)
            {
                if (twa.Type == WorkflowActivityType.DecompositionWorkflow || twa.Type == WorkflowActivityType.CallWorkflow)
                {
                    var lastConn = ctx.Connections.OfType<WorkflowConnectionEntity>().Single(a => a.To.Is(twa));

                    Decompose(@case, previous, twa, lastConn, ctx);
                }
                else
                {
                    var nca = InsertNewCaseActivity(@case, twa, previous);
                    InsertCaseActivityNotifications(nca);
                    ctx.NotifyTransitionContext(nca);
                }
            }

            foreach (var twe in ctx.ToIntermediateEvents)
            {
                InsertNewCaseActivity(@case, twe, previous);
            }
        }

        private static void ExecuteBoundaryTimer(CaseActivityEntity ca, WorkflowEventEntity boundaryEvent)
        {
            switch (boundaryEvent.Type)
            {
                case WorkflowEventType.BoundaryForkTimer:
                    new CaseActivityExecutedTimerEntity
                    {
                        BoundaryEvent = boundaryEvent.ToLite(),
                        CaseActivity = ca.ToLite(),
                    }.Save();
                    break;
                case WorkflowEventType.BoundaryInterruptingTimer:
                    ca.MakeDone(DoneType.Timeout, boundaryEvent.DecisionOptionName);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected Boundary Timer Type " + boundaryEvent.Type);
            }

            var connection = boundaryEvent.NextConnectionsFromCache(ConnectionType.Normal).SingleEx();

            var @case = ca.Case;
            var ctx = new WorkflowExecuteStepContext(@case, ca)
            {
                Case = @case,
                PreviousCaseActivity = ca,
            };

            ctx.ExecuteConnection(connection);

            if (!FindNext(connection.To, ctx))
                return;

            FinishStep(ca.Case, ctx, ca);
        }

        private static void ExecuteInitialStep(CaseEntity @case, WorkflowEventEntity @event, WorkflowConnectionEntity transition)
        {
            SaveEntity(@case.MainEntity);

            @case.Description = @case.MainEntity.ToString()!.Trim().Etc(100);
            @case.Save();

            var ctx = new WorkflowExecuteStepContext(@case, null);

            if (transition.Condition != null)
            {
                var jumpCtx = new WorkflowTransitionContext(@case, null, transition);
                var result = transition.Condition.Evaluate(@case.MainEntity, jumpCtx);
                if (!result)
                    throw new ApplicationException(WorkflowMessage.JumpTo0FailedBecause1.NiceToString(transition, transition.Condition));
            }

            ctx.ExecuteConnection(transition);
            if (!FindNext(transition, ctx))
                return;

            FinishStep(@case, ctx, null);
        }

        static CaseActivityEntity InsertNewCaseActivity(CaseEntity @case, IWorkflowNodeEntity workflowActivity, CaseActivityEntity? previous)
        {
            return new CaseActivityEntity
            {
                StartDate = previous?.DoneDate ?? Clock.Now,
                Previous = previous?.ToLite(),
                WorkflowActivity = workflowActivity,
                OriginalWorkflowActivityName = workflowActivity.GetName()!,
                Case = @case,
                ScriptExecution = GetScriptExecution(workflowActivity)
            }.Save();
        }


        private static ScriptExecutionEmbedded? GetScriptExecution(IWorkflowNodeEntity workflowActivity)
        {
            return workflowActivity is WorkflowActivityEntity w && w.Type == WorkflowActivityType.Script ? new ScriptExecutionEmbedded
            {
                NextExecution = Clock.Now,
                RetryCount = 0,
            } : null;
        }

        internal static void TryToRecompose(CaseEntity childCase)
        {
            using (AuthLogic.Disable()) //Type Conditions may not give access to parent case
            {
                if (Database.Query<CaseEntity>().Where(cc => cc.ParentCase.Is(childCase.ParentCase) && cc.Workflow.Is(childCase.Workflow)).All(a => a.FinishDate.HasValue))
                {
                    var decompositionCaseActivity = childCase.DecompositionSurrogateActivity();
                    if (decompositionCaseActivity.DoneDate != null)
                        throw new InvalidOperationException("The DecompositionCaseActivity is already finished");

                    var lastActivitiesNotes = Database.Query<CaseEntity>().Where(c => c.ParentCase.Is(childCase.ParentCase)).Select(c => c.CaseActivities().OrderByDescending(ca => ca.DoneDate).FirstOrDefault())
                        .Where(ca => ca != null)
                        .Select(ca => new { ca!.DoneBy, ca!.Note })
                        .ToList()
                        .Where(ca => ca.Note.HasText()).ToString(a => $"{a.DoneBy}: {a.Note}", "\n");

                    decompositionCaseActivity.Note = lastActivitiesNotes;
                    ExecuteStep(decompositionCaseActivity, DoneType.Recompose, null, null);
                }
            }
        } 

        private static void Decompose(CaseEntity @case, CaseActivityEntity? previous, WorkflowActivityEntity decActivity, WorkflowConnectionEntity conn, WorkflowExecuteStepContext ctx)
        {
            using (AuthLogic.Disable()) //Type Conditions may not give access to parent case
            {
                var surrogate = InsertNewCaseActivity(@case, decActivity, previous);
                ctx.NotifyTransitionContext(surrogate);
                var subEntities = decActivity.SubWorkflow!.SubEntitiesEval.Algorithm.GetSubEntities(@case.MainEntity, new WorkflowTransitionContext(@case, previous, conn));
                if (decActivity.Type == WorkflowActivityType.CallWorkflow && subEntities.Count > 1)
                    throw new InvalidOperationException("More than one entity generated using CallWorkflow. Use DecompositionWorkflow instead.");

                if (subEntities.IsEmpty())
                    ExecuteStep(surrogate, DoneType.Recompose, null, null);
                else
                {
                    var subWorkflow = decActivity.SubWorkflow.Workflow;
                    foreach (var se in subEntities)
                    {
                        var caseActivity = subWorkflow.ConstructFrom(CaseActivityOperation.CreateCaseActivityFromWorkflow, se, @case.ToLite());
                        caseActivity.Previous = surrogate.ToLite();
                        caseActivity.Execute(CaseActivityOperation.Register);
                    }
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
                else if (ne.Type == WorkflowEventType.IntermediateTimer)
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
                                .Where(a => a.Type == ConnectionType.Normal || a.Type == ConnectionType.Decision)
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
                                 a.Type == ConnectionType.Decision ||
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
                            if (!AllTrackCompleted(0, gateway, ctx, new HashSet<IWorkflowNodeEntity>()).IsCompleted)
                                return false;

                            var singleConnection = gateway.NextConnectionsFromCache(ConnectionType.Normal).SingleEx();
                            return FindNext(singleConnection, ctx);
                        }
                    default:
                        throw new InvalidOperationException();
                }
            }
        }

        public static IDisposable Visiting(HashSet<IWorkflowNodeEntity> visited, IWorkflowNodeEntity node)
        {
            visited.Add(node);
            return new Disposable(() => visited.Remove(node));
        }




        private static BoolBox AllTrackCompleted(int depth, IWorkflowNodeEntity node, WorkflowExecuteStepContext ctx, HashSet<IWorkflowNodeEntity> visited)
        {
            if (node is WorkflowActivityEntity || node is WorkflowEventEntity we && we.Type == WorkflowEventType.IntermediateTimer)
            {
                var caseActivity = ctx.Case.CaseActivities().Where(a => a.WorkflowActivity.Is(node)).OrderBy(a => a.StartDate).LastOrDefault();

                if (caseActivity != null)
                {
                    if (node.Is(ctx.PreviousCaseActivity!.WorkflowActivity))
                        caseActivity = ctx.PreviousCaseActivity;

                    if (caseActivity.DoneDate.HasValue)
                        return BoolBox.True(caseActivity);
                    else
                        return BoolBox.False;
                }

                if (visited.Contains(node))
                    return BoolBox.False;

                var previous = node.PreviousConnectionsFromCache().ToList();
                using (Visiting(visited, node))
                {
                    if (previous.Any(wc => AllTrackCompleted(depth, wc.From, ctx, visited).IsCompatible(wc)))
                        return BoolBox.True(caseActivity);
                    else
                        return BoolBox.False;
                }
            }
            else if (node is WorkflowEventEntity e && (e.Type == WorkflowEventType.BoundaryForkTimer || e.Type == WorkflowEventType.BoundaryInterruptingTimer))
            {
                var parentActivity = WorkflowLogic.GetWorkflowNodeGraph(e.Lane.Pool.Workflow.ToLite()).Activities.GetOrThrow(e.BoundaryOf!);

                var caseActivity = ctx.Case.CaseActivities().Where(a => a.WorkflowActivity.Is(parentActivity)).OrderBy(a => a.StartDate).LastOrDefault();

                if (caseActivity != null)
                {
                    if (node.Is(ctx.PreviousCaseActivity!.WorkflowActivity))
                    {
                        //caseActivity = ctx.CaseActivity;
                        throw new InvalidOperationException("Unexpected BoundaryTimer with WorkflowEvent in CaseActivity");
                    }

                    if (caseActivity.DoneDate.HasValue)
                        return BoolBox.True(caseActivity);
                    else
                        return BoolBox.False;
                }

                if (visited.Contains(parentActivity))
                    return BoolBox.False;

                using (Visiting(visited, parentActivity))
                {
                    var connections = parentActivity.PreviousConnectionsFromCache().ToList();
                    if (parentActivity.BoundaryTimers.Any(a => a.Type == WorkflowEventType.BoundaryForkTimer))
                    {
                        if (depth <= 1)
                            return BoolBox.True(null);

                        if (connections.Any(wc => AllTrackCompleted(depth - 1, wc.From, ctx, visited).IsCompatible(wc)))
                            return BoolBox.True(caseActivity);
                        else
                            return BoolBox.False;
                    }
                    else
                    {
                        if (connections.Any(wc => AllTrackCompleted(depth, wc.From, ctx, visited).IsCompatible(wc)))
                            return BoolBox.True(caseActivity);
                        else
                            return BoolBox.False;
                    }
                }

            }
            else if (node is WorkflowGatewayEntity g)
            {
                if (g.Direction == WorkflowGatewayDirection.Split)
                {
                    var wc = g.PreviousConnectionsFromCache().SingleEx();
                    switch (g.Type)
                    {
                        case WorkflowGatewayType.Exclusive:
                            {
                                var bb = AllTrackCompleted(depth, wc.From, ctx, visited);
                                if (bb.IsCompatible(wc))
                                    return BoolBox.True(bb.CaseActivity);
                                else
                                    return BoolBox.False;
                            }
                        case WorkflowGatewayType.Inclusive:
                        case WorkflowGatewayType.Parallel:
                            {
                                if (depth <= 1)
                                    return BoolBox.True(null);

                                var bb = AllTrackCompleted(depth - 1, wc.From, ctx, visited);
                                if (bb.IsCompatible(wc))
                                    return BoolBox.True(bb.CaseActivity);
                                else
                                    return BoolBox.False;
                            }
                        default:
                            throw new UnexpectedValueException(g.Type);
                    }
                }
                else if (g.Direction == WorkflowGatewayDirection.Join)
                {
                    var connections = g.PreviousConnectionsFromCache().ToList();

                    switch (g.Type)
                    {
                        case WorkflowGatewayType.Exclusive:
                            var first = (from wc in connections
                                         let tuple = AllTrackCompleted(depth, wc.From, ctx, visited)
                                         where tuple.IsCompatible(wc)
                                         select tuple).FirstOrDefault();

                            return first;

                        case WorkflowGatewayType.Inclusive:
                        case WorkflowGatewayType.Parallel:

                            var graph = WorkflowLogic.GetWorkflowNodeGraph(node.Lane.Pool.Workflow.ToLite());

                            var trackGroups = connections.AgGroupToDictionary(
                                wc => graph.TrackId.GetOrThrow(wc.From),
                                wcs => wcs.ToDictionaryEx(wc => wc, wc => AllTrackCompleted(depth + 1, wc.From, ctx, visited).IsCompatible(wc)));

                            if (trackGroups.All(kvp => kvp.Value.Values.Any(a => a))) // Every Parallel gets implicit Exclusive Join behaviour for each Track ID group. 
                                return BoolBox.True(null);
                            else
                                return BoolBox.False;
                        default:
                            throw new UnexpectedValueException(g.Type);
                    }

                }
                else
                    throw new UnexpectedValueException(g.Direction);
            }
            else
                throw new UnexpectedValueException(node);
        }

        struct BoolBox
        {
            public bool IsCompleted { get; private set; }
            public CaseActivityEntity? CaseActivity { get; private set; }

            public BoolBox(bool isCompleted, CaseActivityEntity? caseActivity)
            {
                if (caseActivity != null && !isCompleted)
                    throw new InvalidOperationException("Not completed should not have caseActivities");

                if (caseActivity != null && caseActivity.DoneDate == null)
                    throw new InvalidOperationException("caseActivity is not completed");

                this.IsCompleted = isCompleted;
                this.CaseActivity = caseActivity;
            }

            public static BoolBox False => new BoolBox(false, null);
            public static BoolBox True(CaseActivityEntity? ca) => new BoolBox(true, ca);

            public bool IsCompatible(WorkflowConnectionEntity wc)
            {
                if (!this.IsCompleted)
                    return false;

                if (CaseActivity == null)
                    return true;


                var doneTypeOk = CaseActivity.DoneType!.Value switch
                {
                    DoneType.Next => wc.Type == ConnectionType.Normal && (wc.DoneDecision() == null || wc.DoneDecision() == CaseActivity.DoneDecision),
                    DoneType.Jump => wc.From.Is(CaseActivity.WorkflowActivity) ? wc.Type == ConnectionType.Jump : wc.Type == ConnectionType.Normal,
                    DoneType.ScriptFailure => wc.From.Is(CaseActivity.WorkflowActivity) ? wc.Type == ConnectionType.ScriptException : wc.Type == ConnectionType.Normal,
                    DoneType.ScriptSuccess => wc.Type == ConnectionType.Normal,
                    DoneType.Timeout =>
                    CaseActivity.WorkflowActivity is WorkflowActivityEntity wa ? !wc.From.Is(wa) /*wa.BoundaryTimers.Contains(wc.From)*/ && wc.Type == ConnectionType.Normal :
                    CaseActivity.WorkflowActivity is WorkflowEventEntity we && we.Is(wc.From) ? wc.Type == ConnectionType.Normal :
                    false,
                    DoneType.Recompose => wc.Type == ConnectionType.Normal,
                    DoneType other => throw new UnexpectedValueException(other),
                };

                if (!doneTypeOk)
                    return false;

                if (wc.Condition != null)
                {
                    var result = wc.Condition.Evaluate(CaseActivity.Case.MainEntity, new WorkflowTransitionContext(CaseActivity.Case, CaseActivity, wc));

                    return result;
                }

                return true;
            }
        }
    }

    private static ICaseMainEntity CreateMainEntity(Type type)
    {
        var options = Options.GetOrThrow(type);
        if (options.Constructor == null)
            throw new InvalidOperationException($"The WorkflowOptions for {type.Name} doesn't have a Constructor. Consider adding one in sb.Include<{type.Name}>().WithWorkflow()");

        return options.Constructor();
    }

    private static void MakeDone(this CaseActivityEntity ca, DoneType doneType, string? decision)
    {
        ca.DoneBy = UserEntity.Current;
        ca.DoneDate = Clock.Now;
        ca.DoneType = doneType;
        ca.DoneDecision = decision;
        ca.Case.Description = ca.Case.MainEntity.ToString()!.Trim().Etc(100);
        ca.Save();

        ca.Notifications()
           .DisableQueryFilter()
           .UnsafeUpdate()
           .Set(a => a.State, cn =>  IsForMe.Evaluate(cn) ? CaseNotificationState.Done : CaseNotificationState.DoneByOther)
           .Execute();
    }

    private static void OverrideCaseActivityMixin(SchemaBuilder sb)
    {
        sb.Schema.WhenIncluded<SMSMessageEntity>(() =>
        {
            if (MixinDeclarations.IsDeclared(typeof(SMSMessageEntity), typeof(CaseActivityMixin)))
                QueryLogic.Queries.Register(typeof(SMSMessageEntity), () =>
                    from m in Database.Query<SMSMessageEntity>()
                    select new
                    {
                        Entity = m,
                        m.Id,
                        m.From,
                        m.DestinationNumber,
                        m.State,
                        m.SendDate,
                        m.Template,
                        m.Referred,
                        m.Mixin<CaseActivityMixin>().CaseActivity,
                        m.Exception,
                    });
        });

        sb.Schema.WhenIncluded<EmailMessageEntity>(() =>
        {
            if (MixinDeclarations.IsDeclared(typeof(EmailMessageEntity), typeof(CaseActivityMixin)))
                QueryLogic.Queries.Register(typeof(EmailMessageEntity), () =>
                    from e in Database.Query<EmailMessageEntity>()
                    select new
                    {
                        Entity = e,
                        e.Id,
                        e.State,
                        e.Subject,
                        e.Template,
                        e.Sent,
                        e.Target,
                        e.Mixin<CaseActivityMixin>().CaseActivity,
                        e.Mixin<EmailMessagePackageMixin>().Package,
                        e.Exception,
                    });
        });
    }
}
